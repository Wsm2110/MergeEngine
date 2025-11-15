using MergeEngine.Contracts;
using MergeEngine.Core;

namespace MergeEngine;

/// <summary>
/// Provides a comprehensive library of reusable merge rules used to resolve
/// property-level conflicts in distributed, eventually-consistent objects.
///
/// Vector clocks determine whether a conflict is causal or concurrent:
/// <list type="bullet">
///   <item><description><b>Before</b> — the remote object is strictly newer.</description></item>
///   <item><description><b>After</b> — the local object is strictly newer.</description></item>
///   <item><description><b>Equal</b> — both objects share identical causal history.</description></item>
///   <item><description><b>Concurrent</b> — both objects were updated independently without seeing each other.</description></item>
/// </list>
///
/// Merge rules are executed **only for concurrent updates**.  
/// For Before/After/Equal, causal order resolves the conflict automatically,
/// meaning the rule acts as a **domain-specific conflict resolver**, not a replacement 
/// for vector-clock semantics.
///
/// This model mimics modern CRDT and multi-master system behavior (e.g., Dynamo, Riak, 
/// Cassandra, Akka Distributed Data).
/// </summary>
public static class MergeRules
{
    // ---------------------------------------------------------------------
    //  BASE / STANDARD RULES
    // ---------------------------------------------------------------------

    /// <summary>
    /// Default vector-clock-aware Last-Write-Wins rule.
    ///
    /// <para><b>Behavior:</b></para>
    /// <list type="bullet">
    ///   <item><description>Remote is causally newer → remote wins.</description></item>
    ///   <item><description>Local is causally newer → local wins.</description></item>
    ///   <item><description>Equal history → remote selected for determinism.</description></item>
    ///   <item><description>Concurrent → remote chosen (symmetric LWW).</description></item>
    /// </list>
    ///
    /// Useful for fields representing pure "latest state" values.
    /// </summary>
    public static IMergeRule<T> LastWriteWins<T>() =>
        new VectorClockLwwRule<T>();

    /// <summary>
    /// Boolean OR merge rule — true dominates false.
    ///
    /// Suitable for:
    /// <list type="bullet">
    ///   <item><description>Alarm states</description></item>
    ///   <item><description>Feature flags</description></item>
    ///   <item><description>Readiness indicators</description></item>
    /// </list>
    ///
    /// This rule is **monotonic**, meaning once the value becomes true, it can never revert.
    /// </summary>
    public static IMergeRule<bool> OrBoolean() =>
        new OrBooleanRule();


    /// <summary>
    /// Boolean AND merge rule — false dominates true.
    ///
    /// Useful when:
    /// <list type="bullet">
    ///   <item><description>Every participant must agree</description></item>
    ///   <item><description>Safety is enforced when any source is false</description></item>
    ///   <item><description>You want “all must confirm” semantics</description></item>
    /// </list>
    /// </summary>
    public static IMergeRule<bool> AndBoolean() =>
        new AndBooleanRule();

    /// <summary>
    /// CRDT-style union merge for sets.
    /// Always preserves all unique elements from both replicas.
    ///
    /// This provides **observed-remove** behavior for sets where only additions matter.
    /// </summary>
    public static IMergeRule<HashSet<T>> SetUnion<T>() =>
        new SetUnionRule<T>();

    /// <summary>
    /// A merge rule for distributed counters that sums integer values **only for concurrent merges**.
    ///
    /// This prevents double-counting causal chains.
    /// </summary>
    public static IMergeRule<int> SumInt() =>
        new SumIntRule();

    /// <summary>
    /// Chooses the maximum integer value.
    ///
    /// Useful for:
    /// <list type="bullet">
    ///   <item><description>Confidence scores</description></item>
    ///   <item><description>Threat levels</description></item>
    ///   <item><description>Max-based reconciliation heuristics</description></item>
    /// </list>
    /// </summary>
    public static IMergeRule<int> MaxInt() =>
        new MaxIntRule();

    /// <summary>
    /// Chooses the minimum integer value.
    /// </summary>
    public static IMergeRule<int> MinInt() =>
        new MinIntRule();

    /// <summary>
    /// Selects the maximum of two double values.
    /// </summary>
    public static IMergeRule<double> MaxDouble() =>
        new MaxDoubleRule();

    /// <summary>
    /// Selects the minimum of two double values.
    /// </summary>
    public static IMergeRule<double> MinDouble() =>
        new MinDoubleRule();

    /// <summary>
    /// Averages two double values.
    ///
    /// Useful for merging sensor readings or distributed metrics.
    /// </summary>
    public static IMergeRule<double> AverageDouble() =>
        new AverageDoubleRule();

    /// <summary>
    /// Blends two double values using a linear interpolation factor (weight).
    ///
    /// <para>Example: weight = 0.7 → 70% remote, 30% local.</para>
    /// </summary>
    public static IMergeRule<double> BlendDouble(double weight = 0.5) =>
        new BlendDoubleRule(weight);

    /// <summary>
    /// Chooses the string with the greatest length.
    /// Useful for merging descriptions or messages where longer = more complete.
    /// </summary>
    public static IMergeRule<string> LongestString() =>
        new LongestStringRule();

    /// <summary>
    /// Chooses the shortest string.
    /// </summary>
    public static IMergeRule<string> ShortestString() =>
        new ShortestStringRule();


    /// <summary>
    /// Appends two lists (duplicates allowed).
    ///
    /// Useful for merging event logs or trace histories.
    /// </summary>
    public static IMergeRule<List<T>> AppendList<T>() =>
        new AppendListRule<T>();

    /// <summary>
    /// Appends all unique elements from both lists.
    /// Behaves like a list-based CRDT grow-only set.
    /// </summary>
    public static IMergeRule<List<T>> UniqueAppend<T>() =>
        new UniqueAppendRule<T>();

    /// <summary>
    /// Merges two dictionaries.  
    /// If a key exists on both sides, its value is merged via the specified value-rule.
    /// </summary>
    public static IMergeRule<Dictionary<TKey, TValue>> DictionaryMerge<TKey, TValue>(IMergeRule<TValue> valueRule) =>
        new DictionaryMergeRule<TKey, TValue>(valueRule);

    /// <summary>
    /// Timestamp-based merge rule.
    /// The most recent timestamp wins.
    ///
    /// Suitable when entries are versioned by wall-clock time.
    /// </summary>
    public static IMergeRule<(T Value, DateTime Timestamp)> Timestamped<T>() =>
        new TimestampedRule<T>();

    /// <summary>
    /// Uses priority values to determine the winning side.
    /// Higher priority always wins.
    /// </summary>
    public static IMergeRule<(T Value, int Priority)> Priority<T>() =>
        new PriorityRule<T>();

    /// <summary>
    /// Always selects the local value.
    /// Useful for node-locked configuration or when this node is authoritative.
    /// </summary>
    public static IMergeRule<T> PreferLocal<T>() =>
        new PreferLocalRule<T>();

    /// <summary>
    /// Always selects the remote value.
    /// Useful for ingest pipelines or when a remote source is authoritative.
    /// </summary>
    public static IMergeRule<T> PreferRemote<T>() => new PreferRemoteRule<T>();

    /// <summary>
    /// Conflict resolver where a specific node always wins.
    /// Ideal for:
    /// <list type="bullet">
    ///   <item><description>Primary-backup architectures</description></item>
    ///   <item><description>Leader-based replication</description></item>
    ///   <item><description>Manual operator override</description></item>
    /// </list>
    /// </summary>
    public static IMergeRule<T> NodeAlwaysWins<T>(string nodeId) => new NodeAlwaysWinsRule<T>(nodeId);

    /// <summary>
    /// “Proof-of-work”-style rule:  
    /// The side with the largest total number of vector-clock updates wins.
    ///
    /// Mimics distributed blockchains where the longest chain wins.
    /// </summary>
    public static IMergeRule<T> MostUpdatesWins<T>() =>
        new MostUpdatesWinsRule<T>();

    /// <summary>
    /// The replica whose single node contributed the most updates wins.
    ///
    /// Useful for:
    /// <list type="bullet">
    ///   <item><description>Strong-leader nodes</description></item>
    ///   <item><description>Resource-weighted consensus</description></item>
    ///   <item><description>Validator-importance logic</description></item>
    /// </list>
    /// </summary>
    public static IMergeRule<T> HighestNodeContributionWins<T>() =>
        new HighestNodeContributionWinsRule<T>();

    /// <summary>
    /// Trust-based rule, where each node has a predefined weight or reputation.
    /// The side with the higher weighted contribution wins.
    ///
    /// Suitable for:
    /// <list type="bullet">
    ///   <item><description>Reputation systems</description></item>
    ///   <item><description>Weighted quorum models</description></item>
    ///   <item><description>IoT networks with unreliable devices</description></item>
    /// </list>
    /// </summary>
    public static IMergeRule<T> TrustWeighted<T>(Dictionary<string, int> trust) =>
        new TrustWeightedRule<T>(trust);

    /// <summary>
    /// Randomly picks local or remote on conflict.
    ///
    /// Used in gossip and epidemic protocols 
    /// where randomness ensures eventual spread.
    /// </summary>
    public static IMergeRule<T> RandomChoice<T>() =>
        new RandomChoiceRule<T>();

    /// <summary>
    /// Majority-vote rule — the side with more vector-clock entries wins.
    ///
    /// Useful when:
    /// <list type="bullet">
    ///   <item><description>Replica groups operate by quorum</description></item>
    ///   <item><description>You need democratic resolution</description></item>
    /// </list>
    /// </summary>
    public static IMergeRule<T> MajorityVote<T>() =>
        new MajorityVoteRule<T>();

    /// <summary>
    /// Lexicographic rule: the side whose smallest node-id is alphabetically earlier wins.
    /// This gives a deterministic conflict resolver independent of timing.
    /// </summary>
    public static IMergeRule<T> LexicographicNodeWins<T>() =>
        new LexicographicNodeWinsRule<T>();

    private sealed class VectorClockLwwRule<T> : IMergeRule<T>
    {
        public T Merge(T local, T remote, VectorClock lc, VectorClock rc)
        {
            var relation = lc.Compare(rc);

            return relation switch
            {
                VectorClockRelation.Before => remote,
                VectorClockRelation.After => local,
                VectorClockRelation.Equal => remote,
                VectorClockRelation.Concurrent => remote,
                _ => remote
            };
        }
    }

    private sealed class OrBooleanRule : IMergeRule<bool>
    {
        public bool Merge(bool local, bool remote, VectorClock lc, VectorClock rc) =>
            local || remote;
    }

    private sealed class AndBooleanRule : IMergeRule<bool>
    {
        public bool Merge(bool local, bool remote, VectorClock lc, VectorClock rc) =>
            local && remote;
    }

    private sealed class SetUnionRule<T> : IMergeRule<HashSet<T>>
    {
        public HashSet<T> Merge(HashSet<T> local, HashSet<T> remote, VectorClock lc, VectorClock rc)
        {
            var result = new HashSet<T>(local ?? new());
            if (remote != null)
                result.UnionWith(remote);
            return result;
        }
    }

    private sealed class SumIntRule : IMergeRule<int>
    {
        public int Merge(int local, int remote, VectorClock lc, VectorClock rc) =>
            local + remote;
    }

    private sealed class MaxIntRule : IMergeRule<int>
    {
        public int Merge(int local, int remote, VectorClock lc, VectorClock rc) =>
            Math.Max(local, remote);
    }

    private sealed class MinIntRule : IMergeRule<int>
    {
        public int Merge(int local, int remote, VectorClock lc, VectorClock rc) =>
            Math.Min(local, remote);
    }

    private sealed class MaxDoubleRule : IMergeRule<double>
    {
        public double Merge(double local, double remote, VectorClock lc, VectorClock rc) =>
            Math.Max(local, remote);
    }

    private sealed class MinDoubleRule : IMergeRule<double>
    {
        public double Merge(double local, double remote, VectorClock lc, VectorClock rc) =>
            Math.Min(local, remote);
    }

    private sealed class AverageDoubleRule : IMergeRule<double>
    {
        public double Merge(double local, double remote, VectorClock lc, VectorClock rc) =>
            (local + remote) / 2.0;
    }

    private sealed class BlendDoubleRule : IMergeRule<double>
    {
        private readonly double _weight;

        public BlendDoubleRule(double weight) =>
            _weight = weight;

        public double Merge(double local, double remote, VectorClock lc, VectorClock rc) =>
            local * (1 - _weight) + remote * _weight;
    }

    private sealed class LongestStringRule : IMergeRule<string>
    {
        public string Merge(string local, string remote, VectorClock lc, VectorClock rc) =>
            (local?.Length ?? 0) >= (remote?.Length ?? 0) ? local : remote;
    }

    private sealed class ShortestStringRule : IMergeRule<string>
    {
        public string Merge(string local, string remote, VectorClock lc, VectorClock rc) =>
            (local?.Length ?? int.MaxValue) <= (remote?.Length ?? int.MaxValue) ? local : remote;
    }

    private sealed class AppendListRule<T> : IMergeRule<List<T>>
    {
        public List<T> Merge(List<T> local, List<T> remote, VectorClock lc, VectorClock rc)
        {
            var result = new List<T>(local ?? new());
            if (remote != null)
                result.AddRange(remote);
            return result;
        }
    }

    private sealed class UniqueAppendRule<T> : IMergeRule<List<T>>
    {
        public List<T> Merge(List<T> local, List<T> remote, VectorClock lc, VectorClock rc)
        {
            var result = new List<T>(local ?? new());
            if (remote != null)
            {
                foreach (var item in remote)
                    if (!result.Contains(item))
                        result.Add(item);
            }
            return result;
        }
    }

    private sealed class DictionaryMergeRule<TKey, TValue> :
        IMergeRule<Dictionary<TKey, TValue>>
    {
        private readonly IMergeRule<TValue> _valueRule;

        public DictionaryMergeRule(IMergeRule<TValue> rule) =>
            _valueRule = rule;

        public Dictionary<TKey, TValue> Merge(Dictionary<TKey, TValue> local, Dictionary<TKey, TValue> remote, VectorClock lc, VectorClock rc)
        {
            var result = new Dictionary<TKey, TValue>(local ?? new());

            if (remote != null)
            {
                foreach (var kv in remote)
                {
                    if (!result.TryGetValue(kv.Key, out var lv))
                    {
                        result[kv.Key] = kv.Value;
                        continue;
                    }

                    result[kv.Key] = _valueRule.Merge(lv, kv.Value, lc, rc);
                }
            }

            return result;
        }
    }

    private sealed class TimestampedRule<T> :
        IMergeRule<(T Value, DateTime Timestamp)>
    {
        public (T Value, DateTime Timestamp) Merge(
            (T Value, DateTime Timestamp) local,
            (T Value, DateTime Timestamp) remote,
            VectorClock lc, VectorClock rc)
        {
            return local.Timestamp >= remote.Timestamp ? local : remote;
        }
    }

    private sealed class PriorityRule<T> : IMergeRule<(T Value, int Priority)>
    {
        public (T Value, int Priority) Merge(
            (T Value, int Priority) local,
            (T Value, int Priority) remote,
            VectorClock lc, VectorClock rc)
        {
            return local.Priority >= remote.Priority ? local : remote;
        }
    }

    private sealed class PreferLocalRule<T> : IMergeRule<T>
    {
        public T Merge(T local, T remote, VectorClock lc, VectorClock rc) => local;
    }

    private sealed class PreferRemoteRule<T> : IMergeRule<T>
    {
        public T Merge(T local, T remote, VectorClock lc, VectorClock rc) => remote;
    }

    private sealed class NodeAlwaysWinsRule<T> : IMergeRule<T>
    {
        private readonly string _superNode;

        public NodeAlwaysWinsRule(string nodeId) => _superNode = nodeId;

        public T Merge(T local, T remote, VectorClock lc, VectorClock rc)
        {
            lc.Versions.TryGetValue(_superNode, out var lv);
            rc.Versions.TryGetValue(_superNode, out var rv);

            if (lv > rv) return local;
            if (rv > lv) return remote;

            return local;
        }
    }

    private sealed class MostUpdatesWinsRule<T> : IMergeRule<T>
    {
        public T Merge(T local, T remote, VectorClock lc, VectorClock rc)
        {
            long totalLocal = lc.Versions.Values.Sum();
            long totalRemote = rc.Versions.Values.Sum();

            return totalLocal >= totalRemote ? local : remote;
        }
    }

    private sealed class HighestNodeContributionWinsRule<T> : IMergeRule<T>
    {
        public T Merge(T local, T remote, VectorClock lc, VectorClock rc)
        {
            long maxLocal = lc.Versions.Values.DefaultIfEmpty(0).Max();
            long maxRemote = rc.Versions.Values.DefaultIfEmpty(0).Max();

            return maxLocal >= maxRemote ? local : remote;
        }
    }

    private sealed class TrustWeightedRule<T> : IMergeRule<T>
    {
        private readonly Dictionary<string, int> _trust;

        public TrustWeightedRule(Dictionary<string, int> trust) => _trust = trust;

        public T Merge(T local, T remote, VectorClock lc, VectorClock rc)
        {
            long localScore = lc.Versions.Sum(kv =>
                kv.Value * (_trust.TryGetValue(kv.Key, out var w) ? w : 1));

            long remoteScore = rc.Versions.Sum(kv =>
                kv.Value * (_trust.TryGetValue(kv.Key, out var w) ? w : 1));

            return localScore >= remoteScore ? local : remote;
        }
    }

    private sealed class RandomChoiceRule<T> : IMergeRule<T>
    {
        private static readonly Random Rng = new();

        public T Merge(T local, T remote, VectorClock lc, VectorClock rc)
            => Rng.NextDouble() < 0.5 ? local : remote;
    }

    private sealed class MajorityVoteRule<T> : IMergeRule<T>
    {
        public T Merge(T local, T remote, VectorClock lc, VectorClock rc)
        {
            int localVotes = lc.Versions.Count;
            int remoteVotes = rc.Versions.Count;

            if (localVotes > remoteVotes) return local;
            if (remoteVotes > localVotes) return remote;

            return local; // deterministic on tie
        }
    }

    private sealed class LexicographicNodeWinsRule<T> : IMergeRule<T>
    {
        public T Merge(T local, T remote, VectorClock lc, VectorClock rc)
        {
            var localMin = lc.Versions.Keys.Min();
            var remoteMin = rc.Versions.Keys.Min();

            return string.CompareOrdinal(localMin, remoteMin) <= 0
                ? local
                : remote;
        }
    }
}
