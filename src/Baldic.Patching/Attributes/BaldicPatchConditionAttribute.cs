using System;
using Baldic.Patching.Conditions;

namespace Baldic.Patching.Attributes
{
    /// <summary>
    /// Apply this attribute to a Harmony patch class to make it conditional.
    /// The condition type must implement <see cref="IBaldicPatchCondition"/>
    /// and have a public no-argument constructor (or a constructor matching
    /// the arguments passed here).
    ///
    /// Multiple instances of this attribute on the same class are ANDed together.
    ///
    /// Usage example:
    /// <code>
    /// [BaldicPatchCondition(typeof(ModLoadedCondition), "some_other_mod")]
    /// [HarmonyPatch(typeof(SomeClass), nameof(SomeClass.SomeMethod))]
    /// public static class MyConditionalPatch
    /// {
    ///     public static void Postfix() { }
    /// }
    /// </code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class BaldicPatchConditionAttribute : Attribute
    {
        /// <summary>The condition type, must implement <see cref="IBaldicPatchCondition"/>.</summary>
        public Type ConditionType { get; }

        /// <summary>Optional constructor arguments forwarded to the condition's constructor.</summary>
        public object?[] Args { get; }

        public BaldicPatchConditionAttribute(Type conditionType, params object?[] args)
        {
            if (!typeof(IBaldicPatchCondition).IsAssignableFrom(conditionType))
                throw new ArgumentException(
                    $"Type '{conditionType.FullName}' must implement IBaldicPatchCondition.",
                    nameof(conditionType));

            ConditionType = conditionType;
            Args = args;
        }

        /// <summary>
        /// Instantiates the condition and evaluates it.
        /// Returns <c>false</c> if instantiation fails (fail-safe: skip patch).
        /// </summary>
        public bool Evaluate(Conditions.PatchConditionContext context)
        {
            try
            {
                var condition = (IBaldicPatchCondition)(Args.Length == 0
                    ? Activator.CreateInstance(ConditionType)!
                    : Activator.CreateInstance(ConditionType, Args)!);
                return condition.ShouldPatch(context);
            }
            catch (Exception ex)
            {
                // If condition instantiation or evaluation fails, skip the patch.
                System.Console.Error.WriteLine(
                    $"[Baldic.Patching] Condition {ConditionType.Name} threw: {ex.Message}");
                return false;
            }
        }
    }
}
