using System;

namespace Baldic.Patching.Registry
{
    public enum PatchType
    {
        Prefix,
        Postfix,
        Transpiler,
        Finalizer,
        ReversePatch
    }

    /// <summary>
    /// An immutable record of one Harmony patch application, stored in <see cref="PatchRegistry"/>.
    /// </summary>
    public sealed class PatchRecord
    {
        /// <summary>Harmony instance ID used to apply this patch.</summary>
        public string HarmonyId { get; }

        /// <summary>Fully qualified name of the patched method, e.g. "Assembly.Type::Method".</summary>
        public string TargetMethodFullName { get; }

        /// <summary>The mod that owns this patch.</summary>
        public string OwnerModId { get; }

        /// <summary>Patch class full name.</summary>
        public string PatchClassName { get; }

        public PatchType Type { get; }

        /// <summary>Whether the patch was actually applied (false if condition returned false).</summary>
        public bool Applied { get; }

        /// <summary>Condition type name, or null if unconditional.</summary>
        public string? ConditionType { get; }

        /// <summary>Result of condition evaluation, or true if unconditional.</summary>
        public bool ConditionResult { get; }

        /// <summary>Exception message if patching failed, or null on success.</summary>
        public string? Error { get; }

        public DateTime AppliedAt { get; }

        public PatchRecord(
            string harmonyId,
            string targetMethodFullName,
            string ownerModId,
            string patchClassName,
            PatchType type,
            bool applied,
            string? conditionType = null,
            bool conditionResult = true,
            string? error = null)
        {
            HarmonyId = harmonyId;
            TargetMethodFullName = targetMethodFullName;
            OwnerModId = ownerModId;
            PatchClassName = patchClassName;
            Type = type;
            Applied = applied;
            ConditionType = conditionType;
            ConditionResult = conditionResult;
            Error = error;
            AppliedAt = DateTime.UtcNow;
        }

        public override string ToString() =>
            $"[{(Applied ? "OK" : "SKIP")}] {OwnerModId} → {Type} {TargetMethodFullName}" +
            (ConditionType != null ? $" (cond:{ConditionType}={ConditionResult})" : "") +
            (Error != null ? $" ERROR: {Error}" : "");
    }
}
