using MergeEngine.Core;
using MergeEngine.Contracts;
using MergeEngine.Core;

namespace MergeEngine.Tests.Models;

/// <summary>
/// Example mergeable entity for a distributed system (e.g. blue force tracking).
/// </summary>
public sealed class UnitState : IMergeObject
{
    /// <inheritdoc />
    public VectorClock Clock { get; set; } = new VectorClock();

    /// <summary>
    /// Gets or sets the unit's callsign.
    /// </summary>
    public string Callsign { get; set; }

    /// <summary>
    /// Gets or sets the current speed.
    /// </summary>
    public double Speed { get; set; }

    /// <summary>
    /// Gets or sets whether the unit is armed.
    /// </summary>
    public bool IsArmed { get; set; }

    /// <summary>
    /// Gets or sets the set of blue force identifiers associated with this unit.
    /// </summary>
    public HashSet<string> BlueForces { get; set; } = new HashSet<string>();

    [IgnoreMerge]
    public string DebugInfo { get; set; } // ignored
}