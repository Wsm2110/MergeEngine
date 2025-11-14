using MergeEngine.Core;
using MergEngine.Contracts;
using MergEngine.Core;

namespace MergeEngine.Samples;
public sealed class VehicleState : IMergeObject
{
    public VectorClock Clock { get; set; } = new VectorClock();

    public string PlateNumber { get; set; }

    public double Speed { get; set; }

    public bool EngineOn { get; set; }

    public HashSet<string> ActiveSensors { get; set; } = new();

    [IgnoreMerge]
    public string LocalNotes { get; set; }
}
