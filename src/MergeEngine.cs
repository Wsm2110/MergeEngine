using MergeEngine.Core;
using MergEngine.Contracts;
using MergEngine.Core;
using System.Linq.Expressions;
using System.Reflection;

namespace MergEngine;

/// <summary>
/// Responsible for merging two distributed object instances of type <see cref="TObject"/>.
/// The merge occurs *per property*, using configurable merge rules, rather than replacing
/// entire objects. This enables selective conflict resolution and CRDT-like behavior.
///
/// Each object includes a <see cref="VectorClock"/> tracking its causal update history.
/// Comparing clocks produces a <see cref="VectorClockRelation"/> value that determines
/// how a conflict is resolved:
///
/// <list type="bullet">
/// <item><description><b>Before</b> – The remote change causally happened after the local change.<br/>
/// → The remote value safely overwrites the local value.</description></item>
/// <item><description><b>After</b> – The local object is newer than the remote object.<br/>
/// → The local value is kept, ignoring the remote.</description></item>
/// <item><description><b>Equal</b> – Both objects share identical causal history.<br/>
/// → Either value is valid. The engine chooses remote for determinism.</description></item>
/// <item><description><b>Concurrent</b> – Both updates were made independently without awareness of each other.<br/>
/// → No automatic "last write" exists. The configured merge rule decides the result
/// (e.g. OR-boolean, Set-Union, Max, Priority, etc.).</description></item>
/// </list>
///
/// This approach is superior to naive timestamp-based Last-Write-Wins because
/// vector clocks detect true concurrency and prevent accidental overwrites caused
/// by clock skew or network delay. Only causally ordered writes overwrite each other.
/// </summary>
/// <typeparam name="TObject">
/// Any type implementing <see cref="IMergeObject"/>, representing mergeable distributed state objects.
/// </typeparam>
public class MergeEngine<TObject> where TObject : IMergeObject, new()
{
    private readonly List<PropertyAccessorBase<TObject>> _properties;
    private readonly Dictionary<string, PropertyAccessorBase<TObject>> _propertiesByName;
    private List<PropertyInfo> _ignoredProperties = new List<PropertyInfo>();

    /// <summary>
    /// Initializes a new instance of the <see cref="MergeEngine{TObject}"/> class.
    /// Discovers all merge-eligible public settable properties on <typeparamref name="TObject"/>,
    /// and automatically applies a default <em>Last-Write-Wins with VectorClock ordering</em> rule
    /// to each property. Properties may later override their behavior by registering custom
    /// merge rules (for example: Set-Union, OR-boolean, numeric Max/Sum, or domain rules).
    ///
    /// Internally, the constructor builds strongly-typed property accessors using compiled
    /// expression trees instead of reflection, enabling high-performance merging suitable
    /// for real-time distributed update pipelines.
    /// </summary>
    public MergeEngine()
    {
        _properties = BuildPropertyAccessors();
        _propertiesByName = _properties.ToDictionary(p => p.Name, p => p);
    }

    /// <summary>
    /// Assigns a custom merge rule to a specific property.
    ///
    /// Custom merge rules are only executed when the update is determined to be
    /// <b>Concurrent</b> based on <see cref="VectorClockRelation"/>. In all other
    /// causal cases, the merge decision is made automatically:
    ///
    /// <list type="bullet">
    /// <item><description><b>Before</b> – Remote change is causally newer → remote value wins automatically.</description></item>
    /// <item><description><b>After</b> – Local change is newer → local value is kept.</description></item>
    /// <item><description><b>Equal</b> – Identical causal history → deterministic overwrite using the remote value.</description></item>
    /// <item><description><b>Concurrent</b> – No ordering exists → the custom merge rule resolves the conflict.</description></item>
    /// </list>
    ///
    /// Example property-specific merge rules include numeric maximum, set-union,
    /// logical OR, priority selection, or domain-specific strategies.
    /// </summary>
    /// <example>
    /// <code>
    /// engine.SetRule(x =&gt; x.Speed, MergeRules.MaxDouble());
    /// engine.SetRule(x =&gt; x.BlueForces, MergeRules.SetUnion&lt;string&gt;());
    /// engine.SetRule(x =&gt; x.IsArmed, MergeRules.OrBoolean());
    /// </code>
    /// </example>
    /// <typeparam name="TProp">The property type.</typeparam>
    public void SetRule<TProp>(Expression<Func<TObject, TProp>> propertyExpression, IMergeRule<TProp> rule)
    {
        if (propertyExpression.Body is not MemberExpression member ||
            member.Member is not PropertyInfo propInfo)
        {
            throw new ArgumentException("Expression must be a simple property access.", nameof(propertyExpression));
        }

        if (!_propertiesByName.TryGetValue(propInfo.Name, out var accessorBase))
            throw new InvalidOperationException($"Property '{propInfo.Name}' is not mergeable or not found.");

        if (accessorBase is not PropertyAccessor<TObject, TProp> typedAccessor)
            throw new InvalidOperationException($"Property '{propInfo.Name}' type mismatch when setting rule.");

        typedAccessor.SetRule(rule);
    }

    /// <summary>
    /// Merges two distributed object instances and returns a **new merged instance**.
    /// This method does **not** modify <paramref name="local"/> or <paramref name="remote"/>.
    ///
    /// A fresh <typeparamref name="TObject"/> instance is allocated and populated
    /// by resolving each property independently using the configured merge rules.
    ///
    /// The merged object receives a merged <see cref="VectorClock"/> created by taking
    /// the element-wise maximum of both clocks, ensuring full causal history retention.
    /// </summary>
    /// <param name="local">The local instance.</param>
    /// <param name="remote">The remote instance.</param>
    /// <returns>
    /// A new instance representing the merged state of <paramref name="local"/> and <paramref name="remote"/>.
    /// </returns>
    public TObject Merge(TObject local, TObject remote)
    {
        if (local == null) return remote;
        if (remote == null) return local;

        var result = new TObject();

        // Merge allowed properties
        foreach (var accessor in _properties)
            accessor.MergeInto(result, local, remote, local.Clock, remote.Clock);

        // Copy ignored properties directly from local
        foreach (var prop in _ignoredProperties)
        {
            var value = prop.GetValue(local);
            prop.SetValue(result, value);
        }

        result.Clock = MergeClocks(local.Clock, remote.Clock);
        return result;
    }

    /// <summary>
    /// Merges <paramref name="remote"/> into <paramref name="local"/> **in-place**, updating only the changed
    /// properties on the <paramref name="local"/> object. This avoids allocating a new object and reduces
    /// property change events and UI churn.
    /// 
    /// Use this for high-frequency real-time scenarios where minimizing allocations is critical.
    /// </summary>
    /// <param name="local">The instance that will receive updates.</param>
    /// <param name="remote">The incoming remote instance to merge from.</param>
    /// <returns>The modified <paramref name="local"/> instance.</returns>
    public TObject MergeInto(TObject local, TObject remote)
    {
        if (remote == null) return local;
        if (local == null) return remote;

        foreach (var accessor in _properties)
        {
            accessor.MergeInto(local, local, remote, local.Clock, remote.Clock);
        }

        local.Clock = MergeClocks(local.Clock, remote.Clock);
        return local;
    }

    private static VectorClock MergeClocks(VectorClock a, VectorClock b)
    {
        var merged = new VectorClock();

        foreach (var key in a.Versions.Keys.Union(b.Versions.Keys))
        {
            a.Versions.TryGetValue(key, out var av);
            b.Versions.TryGetValue(key, out var bv);
            merged.Versions[key] = Math.Max(av, bv);
        }

        return merged;
    }

    private List<PropertyAccessorBase<TObject>> BuildPropertyAccessors()
    {
        var properties = typeof(TObject)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance);

        _ignoredProperties = properties
            .Where(p => p.IsDefined(typeof(IgnoreMergeAttribute), true))
            .ToList();

        var mergeable = properties
            .Where(p =>
                p.CanRead &&
                p.CanWrite &&
                p.Name != nameof(IMergeObject.Clock) &&
                !_ignoredProperties.Contains(p))
            .ToArray();

        var list = new List<PropertyAccessorBase<TObject>>(mergeable.Length);

        foreach (var prop in mergeable)
        {
            var accessorType = typeof(PropertyAccessor<,>)
                .MakeGenericType(typeof(TObject), prop.PropertyType);

            list.Add((PropertyAccessorBase<TObject>)Activator.CreateInstance(accessorType, prop)!);
        }

        return list;
    }

}