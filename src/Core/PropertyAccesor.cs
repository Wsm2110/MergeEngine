using MergeEngine.Contracts;
using System.Linq.Expressions;
using System.Reflection;

namespace MergeEngine.Core;

/// <summary>
/// Abstract base type for strongly-typed property access and merge behavior.
/// </summary>
public abstract class PropertyAccessorBase<TObject>
{
    /// <summary>
    /// The name of the property on <typeparamref name="TObject"/>.
    /// </summary>
    public string Name { get; }

    protected PropertyAccessorBase(string name) => Name = name;

    /// <summary>
    /// Executes the merge logic for this property and writes the resolved result into the output object.
    /// </summary>
    public abstract void MergeInto(
        TObject result,
        TObject local,
        TObject remote,
        VectorClock localClock,
        VectorClock remoteClock);
}

/// <summary>
/// Strongly typed property accessor for get/set reflection-free access and per-property merge logic.
/// </summary>
public sealed class PropertyAccessor<TObject, TProp> : PropertyAccessorBase<TObject>
{
    private readonly Func<TObject, TProp> _getter;
    private readonly Action<TObject, TProp> _setter;
    private IMergeRule<TProp> _rule;

    public PropertyAccessor(PropertyInfo propertyInfo)
        : base(propertyInfo.Name)
    {
        _getter = BuildGetter(propertyInfo);
        _setter = BuildSetter(propertyInfo);
        _rule = MergeRules.LastWriteWins<TProp>(); // default rule
    }

    public void SetRule(IMergeRule<TProp> rule) =>
        _rule = rule ?? throw new ArgumentNullException(nameof(rule));

    /// <summary>
    /// Merges the property value and applies the update only if the result differs.
    /// This avoids unnecessary writes, minimizing GC pressure and UI refresh costs.
    /// </summary>
    public override void MergeInto(
        TObject result,
        TObject local,
        TObject remote,
        VectorClock localClock,
        VectorClock remoteClock)
    {
        var relation = localClock.Compare(remoteClock);

        var localValue = _getter(local);
        var remoteValue = _getter(remote);

        TProp resolved;

        switch (relation)
        {
            case VectorClockRelation.Before:
                resolved = remoteValue;
                break;

            case VectorClockRelation.After:
                resolved = localValue;
                break;

            case VectorClockRelation.Equal:
                // Note. means both replicas have seen exactly the same history of updates.
                // Therefore the objects are guaranteed to be in sync, so resolving is a deterministic no-op.
                resolved = remoteValue; 
                break;

            case VectorClockRelation.Concurrent:
            default:
                // Only here do we call custom merge rule
                resolved = _rule.Merge(localValue, remoteValue, localClock, remoteClock);
                break;
        }

        _setter(result, resolved);
    }

    private static Func<TObject, TProp> BuildGetter(PropertyInfo prop)
    {
        var instance = Expression.Parameter(typeof(TObject), "instance");
        var access = Expression.Property(instance, prop);
        return Expression.Lambda<Func<TObject, TProp>>(access, instance).Compile();
    }

    private static Action<TObject, TProp> BuildSetter(PropertyInfo prop)
    {
        var instance = Expression.Parameter(typeof(TObject), "instance");
        var valueParam = Expression.Parameter(typeof(TProp), "value");
        var assign = Expression.Assign(Expression.Property(instance, prop), valueParam);
        return Expression.Lambda<Action<TObject, TProp>>(assign, instance, valueParam).Compile();
    }
}
