namespace MergeEngine.Core;

/// <summary>
/// A vector clock used to track and compare distributed versions of an object.
/// Each entry tracks how many updates have occurred on peer nodes participating
/// in a distributed system.
/// </summary>
public class VectorClock
{
    /// <summary>
    /// Gets the version counters per node identifier.
    /// </summary>
    public Dictionary<string, long> Versions { get; } = new Dictionary<string, long>();

    /// <summary>
    /// Increments the version entry for the specified node.
    /// Call this once when the object is updated on that node.
    /// </summary>
    /// <param name="nodeId">The identifier of the node performing the update.</param>
    public void Increment(string nodeId)
    {
        Versions[nodeId] = Versions.TryGetValue(nodeId, out var value) ? value + 1 : 1;
    }

    /// <summary>
    /// Compares this vector clock with another and returns their causal relationship.
    /// </summary>
    /// <param name="other">The other vector clock.</param>
    public VectorClockRelation Compare(VectorClock other)
    {
        bool less = false;
        bool greater = false;

        foreach (var key in Versions.Keys.Union(other.Versions.Keys))
        {
            Versions.TryGetValue(key, out var v1);
            other.Versions.TryGetValue(key, out var v2);

            if (v1 < v2) less = true;
            if (v1 > v2) greater = true;

            if (less && greater)
                return VectorClockRelation.Concurrent;
        }

        if (!less && !greater)
            return VectorClockRelation.Equal;

        return less ? VectorClockRelation.Before : VectorClockRelation.After;
    }

    /// <summary>
    /// Deep clones the vector clock so replicas do not share reference state.
    /// </summary>
    public VectorClock Clone()
    {
        var clone = new VectorClock();
        foreach (var kv in Versions)
            clone.Versions[kv.Key] = kv.Value;
        return clone;
    }

    /// <summary>
    /// Creates a new vector clock representing the combined causal history of
    /// this clock and another clock. Each per-node counter is merged using the
    /// element-wise maximum, ensuring the resulting clock reflects all observed
    /// updates from both sides.
    ///
    /// This operation is used during object merge to ensure the merged instance
    /// carries a complete and correct version history across nodes.
    /// </summary>
    /// <param name="other">The clock to merge with this clock.</param>
    /// <returns>A new <see cref="VectorClock"/> instance containing the merged counters.</returns>
    public VectorClock Merge(VectorClock other)
    {
        var merged = new VectorClock();

        foreach (var key in Versions.Keys.Union(other.Versions.Keys))
        {
            Versions.TryGetValue(key, out var v1);
            other.Versions.TryGetValue(key, out var v2);
            merged.Versions[key] = Math.Max(v1, v2);
        }

        return merged;
    }
}