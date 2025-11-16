/// <summary>
/// Declares a merge rule to apply to a property during concurrent conflict resolution.
/// Used by <see cref="MergeEngine{TObject}"/> to automatically assign rules.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class MergeRuleAttribute : Attribute
{
    /// <summary>
    /// The merge rule type assigned to this property.
    /// Must implement IMergeRule&lt;T&gt;.
    /// </summary>
    public Type RuleType { get; }

    public MergeRuleAttribute(Type ruleType)
    {
        RuleType = ruleType ?? throw new ArgumentNullException(nameof(ruleType));
    }
}
