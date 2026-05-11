using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Baldic.Loader.Abstractions;
using Baldic.Patching.Attributes;
using Baldic.Patching.Conditions;
using Baldic.Patching.Registry;
using HarmonyLib;

namespace Baldic.Patching
{
    /// <summary>
    /// Orchestrates Harmony patch application for a single mod.
    /// Evaluates <see cref="BaldicPatchConditionAttribute"/> conditions before applying.
    /// Records every patch result in <see cref="PatchRegistry"/>.
    /// </summary>
    public sealed class HarmonyPatcher
    {
        private readonly Harmony _harmony;
        private readonly IModContainer _mod;
        private readonly IBaldicLoader _loader;
        private readonly IBaldicLog _log;
        private readonly string _configRoot;

        /// <summary>
        /// Create a HarmonyPatcher for the given mod.
        /// The Harmony instance ID is "{modId}.baldic".
        /// </summary>
        public HarmonyPatcher(IModContainer mod, IBaldicLoader loader, IBaldicLog log, string configRoot)
        {
            _mod = mod;
            _loader = loader;
            _log = log;
            _configRoot = configRoot;
            _harmony = new Harmony($"{mod.Id}.baldic");
        }

        public string HarmonyId => _harmony.Id;

        /// <summary>
        /// Apply all Harmony patches found in the given assembly.
        /// Classes without a <see cref="BaldicPatchConditionAttribute"/> are patched unconditionally.
        /// Classes with conditions are evaluated; all conditions must be satisfied (AND logic).
        /// </summary>
        public void PatchAll(Assembly assembly)
        {
            var context = new PatchConditionContext(_loader, _mod, _configRoot);
            var types = AccessTools.GetTypesFromAssembly(assembly);

            foreach (var type in types)
            {
                PatchType(type, context);
            }
        }

        /// <summary>
        /// Apply patches from a specific list of type names (from <c>patches.harmony</c> in manifest).
        /// </summary>
        public void PatchTypes(IEnumerable<string> typeFullNames, Assembly[] assemblies)
        {
            var context = new PatchConditionContext(_loader, _mod, _configRoot);
            var assemblyTypes = assemblies.SelectMany(a => AccessTools.GetTypesFromAssembly(a))
                .ToDictionary(t => t.FullName ?? t.Name, StringComparer.Ordinal);

            foreach (var typeName in typeFullNames)
            {
                if (!assemblyTypes.TryGetValue(typeName, out var type))
                {
                    _log.Warn($"[Patching] Patch type '{typeName}' not found in mod '{_mod.Id}' assemblies.");
                    continue;
                }
                PatchType(type, context);
            }
        }

        private void PatchType(Type type, PatchConditionContext context)
        {
            // Only process types that have HarmonyPatch attributes (i.e. are patch classes).
            bool isHarmonyPatch = type.GetCustomAttributes(typeof(HarmonyPatch), inherit: false).Any()
                || type.GetCustomAttributes(typeof(HarmonyPatchAll), inherit: false).Any();
            if (!isHarmonyPatch) return;

            var conditionAttrs = type.GetCustomAttributes(typeof(BaldicPatchConditionAttribute), inherit: false)
                .Cast<BaldicPatchConditionAttribute>()
                .ToList();

            string targetName = GetTargetMethodName(type);
            string? conditionTypeName = conditionAttrs.Count > 0
                ? string.Join("+", conditionAttrs.Select(a => a.ConditionType.Name))
                : null;

            // Evaluate all conditions (AND).
            bool shouldPatch = true;
            foreach (var condAttr in conditionAttrs)
            {
                bool result = condAttr.Evaluate(context);
                _log.Debug($"[Patching] Condition {condAttr.ConditionType.Name} for {type.Name}: {result}");
                if (!result) { shouldPatch = false; break; }
            }

            if (!shouldPatch)
            {
                _log.Info($"[Patching] SKIP {type.Name} (condition false)");
                PatchRegistry.Instance.Record(new PatchRecord(
                    _harmony.Id, targetName, _mod.Id, type.FullName ?? type.Name,
                    Registry.PatchType.Prefix,
                    applied: false,
                    conditionTypeName, conditionResult: false));
                return;
            }

            try
            {
                _harmony.CreateClassProcessor(type).Patch();
                _log.Info($"[Patching] APPLIED {type.Name} → {targetName}");
                PatchRegistry.Instance.Record(new PatchRecord(
                    _harmony.Id, targetName, _mod.Id, type.FullName ?? type.Name,
                    Registry.PatchType.Prefix,
                    applied: true,
                    conditionTypeName, conditionResult: true));
            }
            catch (Exception ex)
            {
                _log.Error($"[Patching] FAILED {type.Name} → {targetName}: {ex.Message}");
                PatchRegistry.Instance.Record(new PatchRecord(
                    _harmony.Id, targetName, _mod.Id, type.FullName ?? type.Name,
                    Registry.PatchType.Prefix,
                    applied: false,
                    conditionTypeName, conditionResult: true, error: ex.Message));
            }
        }

        /// <summary>Unpatch all patches applied by this patcher's Harmony instance.</summary>
        public void UnpatchAll()
        {
            _harmony.UnpatchAll(_harmony.Id);
        }

        private static string GetTargetMethodName(Type patchType)
        {
            // Try to extract target from HarmonyPatch attribute.
            var attr = patchType.GetCustomAttribute<HarmonyPatch>();
            if (attr?.info.declaringType != null && attr.info.methodName != null)
                return $"{attr.info.declaringType.FullName}::{attr.info.methodName}";
            return patchType.FullName ?? patchType.Name;
        }
    }
}
