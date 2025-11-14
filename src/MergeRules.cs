using MergeEngine.Contracts;
using MergeEngine.Core;
using System;
using System.Collections.Generic;

namespace MergeEngine;

/// <summary>
/// Provides reusable, composable merge rules for resolving conflicting property values
/// in distributed, eventually-consistent systems. Each rule is designed to run only when
/// vector clock comparison determines that updates are <see cref="VectorClockRelation.Concurrent"/>,
/// meaning no causal ordering exists between the conflicting updates.
///
/// If the update relationship is <b>Before</b>, <b>After</b>, or <b>Equal</b>, merge rules
/// are bypassed and causal ordering determines the result automatically.
/// </summary>
public static class MergeRules
{
    /// <summary>
    /// Creates a default last-write-wins rule based on vector clock causal order.
    /// This rule selects the value belonging to the newer update, based on
    /// <see cref="VectorClockRelation"/> comparison.
    ///
    /// Only if the updates are <b>Concurrent</b> does it resolve via a deterministic fallback
    /// (local wins by default, but this can be substituted for domain-specific logic).
    /// </summary>
    public static IMergeRule<T> LastWriteWins<T>()
    {
        return new VectorClockLwwRule<T>();
    }

    /// <summary>
    /// Creates a CRDT-style union rule that merges two <see cref="HashSet{T}"/> values.
    /// This rule is useful for collections that represent memberships, tags, or force compositions,
    /// such as BlueForce tracking identifiers where every unique element should be preserved.
    ///
    /// Union merge rules intentionally do not overwrite values but instead preserve information
    /// from both sides of a concurrent update.
    /// </summary>
    public static IMergeRule<HashSet<TItem>> SetUnion<TItem>()
    {
        return new SetUnionRule<TItem>();
    }

    /// <summary>
    /// Provides a boolean merge rule implementing logical OR semantics.
    /// This ensures that <b>true dominates false</b>, which is useful when
    /// the boolean indicates a state like alarm, readiness, or alert activation,
    /// where any positive signal must propagate.
    ///
    /// This rule behaves the same for all vector clock relationships since OR
    /// is monotonic (merging can only move toward true).
    /// </summary>
    public static IMergeRule<bool> OrBoolean()
    {
        return new OrBooleanRule();
    }

    /// <summary>
    /// Chooses the larger of the two double values, regardless of causal relation.
    /// Often used for measurements where the highest confidence or strongest signal
    /// is desired (e.g., threat level, signal strength, speed aggregation).
    /// </summary>
    public static IMergeRule<double> MaxDouble()
    {
        return new MaxDoubleRule();
    }

    /// <summary>
    /// Sums integer values. Useful for distributed counters or additive metrics
    /// such as total quantities, resource usage, or accumulated effects.
    ///
    /// Not idempotent — runs only when concurrent to avoid double-increment problems.
    /// </summary>
    public static IMergeRule<int> SumInt()
    {
        return new SumIntRule();
    }

    #region Rule Implementations

    /// <summary>
    /// Vector clock-based LWW (Last Write Wins) merge rule.
    /// Uses causal comparison to choose either local or remote values.
    /// If updates are concurrent, local wins by default (subject to override).
    /// </summary>
    private sealed class VectorClockLwwRule<T> : IMergeRule<T>
    {
        public T Merge(T localValue, T remoteValue, VectorClock localClock, VectorClock remoteClock)
        {
            var relation = localClock.Compare(remoteClock);

            switch (relation)
            {
                case VectorClockRelation.Before:
                    // Remote happened causally after local — remote is guaranteed newer.
                    return remoteValue;

                case VectorClockRelation.After:
                    // Local happened causally after remote — safe to keep local.
                    return localValue;

                case VectorClockRelation.Equal:
                    // Identical histories — changes originated from same update lineage.
                    // Remote is chosen for determinism.
                    return remoteValue;

                case VectorClockRelation.Concurrent:
                default:
                    // True conflict: both sides modified without seeing each other.
                    // Default LWW fallback returns localValue (can be customized).
                    return localValue;
            }
        }
    }

    /// <summary>
    /// CRDT-style set union. Ensures no information is lost when merging
    /// concurrently updated sets. Represents monotonic growth.
    /// </summary>
    private sealed class SetUnionRule<TItem> : IMergeRule<HashSet<TItem>>
    {
        public HashSet<TItem> Merge(
            HashSet<TItem> localValue,
            HashSet<TItem> remoteValue,
            VectorClock localClock,
            VectorClock remoteClock)
        {
            var result = new HashSet<TItem>(localValue ?? new HashSet<TItem>());

            if (remoteValue != null)
                result.UnionWith(remoteValue); // merges in place

            return result;
        }
    }

    /// <summary>
    /// Logical OR rule for boolean values.
    /// Useful for situations where any "True" should propagate globally.
    /// Examples: alarm triggered, hazard detected, tracking enabled, etc.
    /// </summary>
    private sealed class OrBooleanRule : IMergeRule<bool>
    {
        public bool Merge(bool localValue, bool remoteValue, VectorClock localClock, VectorClock remoteClock)
        {
            return localValue || remoteValue;
        }
    }

    /// <summary>
    /// Numerical aggregation merge rule that chooses the maximum value.
    /// Useful for confidence ratings, speed, range, severity or threat levels.
    /// </summary>
    private sealed class MaxDoubleRule : IMergeRule<double>
    {
        public double Merge(double localValue, double remoteValue, VectorClock localClock, VectorClock remoteClock)
        {
            return Math.Max(localValue, remoteValue);
        }
    }

    /// <summary>
    /// A merge rule that sums integer values.
    /// Useful for distributed counting, additive telemetry, load or capacity calculations.
    /// </summary>
    private sealed class SumIntRule : IMergeRule<int>
    {
        public int Merge(int localValue, int remoteValue, VectorClock localClock, VectorClock remoteClock)
        {
            return localValue + remoteValue;
        }
    }

    #endregion
}
