using MergeEngine.Core;

namespace MergeEngine.Contracts;

/// <summary>
/// Defines a merge rule used to reconcile values of a specific object property
/// during distributed conflict resolution. Merge rules determine how to resolve
/// conflicts when updates occur concurrently across multiple nodes.
///
/// Merge rules are invoked only when the update relationship determined by
/// <see cref="VectorClock.Compare(VectorClock)"/> results in
/// <see cref="VectorClockRelation.Concurrent"/>. In this case, no causal ordering
/// exists between the two updates, meaning neither update can safely overwrite the other
/// without applying custom logic.
///
/// For all other causal relationships:
/// <list type="bullet">
/// <item><description><b>Before</b> – Remote value causally happened after local → remote wins automatically.</description></item>
/// <item><description><b>After</b> – Local value is newer → local wins automatically.</description></item>
/// <item><description><b>Equal</b> – Identical causal history → deterministic overwrite using the remote value.</description></item>
/// <item><description><b>Concurrent</b> – No ordering exists → <b>custom merge rule is executed here</b>.</description></item>
/// </list>
///
/// Examples of custom merge strategies include:
/// numeric maximum, set-union, boolean logical OR, priority selection, domain-specific
/// business rules, or multi-value registers.
/// </summary>
/// <typeparam name="TValue">The type of value being merged.</typeparam>
public interface IMergeRule<TValue>
{
    /// <summary>
    /// Merges two property values using custom logic and contextual vector clock information.
    /// The implementation should examine the relationship between clocks and apply the correct
    /// conflict resolution behavior when necessary.
    /// </summary>
    /// <param name="localValue">The current local property value.</param>
    /// <param name="remoteValue">The remote property value from another node.</param>
    /// <param name="localClock">The vector clock associated with the local state.</param>
    /// <param name="remoteClock">The vector clock associated with the remote state.</param>
    /// <returns>The resolved merged value.</returns>
    TValue Merge(TValue localValue, TValue remoteValue, VectorClock localClock, VectorClock remoteClock);
}
