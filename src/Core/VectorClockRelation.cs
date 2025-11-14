using System;
using System.Collections.Generic;
using System.Text;

namespace MergeEngine.Core;

/// <summary>
/// Describes the causal relationship between two vector clocks.
/// </summary>
public enum VectorClockRelation
{
    /// <summary>
    /// Both clocks represent the same version.
    /// </summary>
    Equal = 0,

    /// <summary>
    /// This clock happens before the other clock.
    /// </summary>
    Before = 1,

    /// <summary>
    /// This clock happens after the other clock.
    /// </summary>
    After = 2,

    /// <summary>
    /// The clocks are concurrent and cannot be ordered causally.
    /// </summary>
    Concurrent = 3
}