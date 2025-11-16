using MergeEngine.Contracts;
using MergeEngine.Core;

namespace MergeEngine.Tests.Models
{
    /// <summary>
    /// Represents a generic distributed state object used for testing the full
    /// MergeEngine + MergeRules system. 
    /// 
    /// This class covers:
    /// - Booleans (OR/AND)
    /// - Int counters (SUM/MAX/MIN)
    /// - Doubles (MAX/MIN/AVERAGE)
    /// - Strings (longest/shortest)
    /// - HashSet CRDT (union)
    /// - List merges (append/unique append)
    /// - Dictionary merges (nested per-value merge rules)
    /// - Default fallback rules
    /// 
    /// It is ideal for:
    /// - Stress tests
    /// - Conflict-resolution tests
    /// - Late-joiner tests
    /// - Vector-clock causal semantics tests
    /// - Rule-override validation
    /// </summary>
    public sealed class NodeState : IMergeObject
    {
        /// <summary>
        /// Vector clock describing the causal history of this replica.
        /// Automatically incremented by MergeObjectExtensions.Update().
        /// </summary>
        public VectorClock Clock { get; set; } = new();

        // ---------------------------------------------------------------------
        // BOOLEAN FLAGS
        // ---------------------------------------------------------------------

        /// <summary>
        /// Whether the node is reachable.
        /// OR-merge semantics: true dominates.
        /// </summary>
        [MergeRule(typeof(MergeRules.OrBooleanRule))]
        public bool IsOnline { get; set; }

        /// <summary>
        /// Whether the node is locked for maintenance.
        /// AND-merge semantics: false dominates.
        /// </summary>
        [MergeRule(typeof(MergeRules.AndBooleanRule))]
        public bool Maintenance { get; set; }

        // ---------------------------------------------------------------------
        // NUMERIC VALUES
        // ---------------------------------------------------------------------

        /// <summary>
        /// Current CPU load in percentage.
        /// MAX-merge resolves concurrent updates by taking the highest value.
        /// </summary>
        [MergeRule(typeof(MergeRules.MaxDoubleRule))]
        public double CpuLoad { get; set; }

        /// <summary>
        /// Distributed counter for tasks processed.
        /// SUM is applied *only* on concurrent updates.
        /// </summary>
        [MergeRule(typeof(MergeRules.SumIntRule))]
        public int TasksProcessed { get; set; }

        /// <summary>
        /// A general performance score.
        /// Demonstrates default fallback: LastWriteWins.
        /// </summary>
        public int Score { get; set; }

        // ---------------------------------------------------------------------
        // STRINGS
        // ---------------------------------------------------------------------

        /// <summary>
        /// Human-readable status label.
        /// Longest string wins.
        /// </summary>
        [MergeRule(typeof(MergeRules.LongestStringRule))]
        public string StatusText { get; set; } = string.Empty;

        // ---------------------------------------------------------------------
        // COLLECTION TYPES
        // ---------------------------------------------------------------------

        /// <summary>
        /// CRDT-style set of active capabilities/plugins.
        /// Always merged using union.
        /// </summary>
        [MergeRule(typeof(MergeRules.SetUnionRule<string>))]
        public HashSet<string> Capabilities { get; set; } = new();

        /// <summary>
        /// Operational event log.
        /// UniqueAppend ensures deterministic ordering without duplicates.
        /// </summary>
        [MergeRule(typeof(MergeRules.UniqueAppendRule<string>))]
        public List<string> EventLog { get; set; } = new();

        /// <summary>
        /// Numeric metrics stored in a dictionary, keys merged with MaxInt as value-rule.
        /// Demonstrates nested merge-rules.
        /// </summary>
        [MergeRule(typeof(MergeRules.DictionaryMergeRule<string, int>))]
        public Dictionary<string, int> Metrics { get; set; } = new();
    }
}
