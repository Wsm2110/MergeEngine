using MergeEngine;
using MergeEngine.Samples;

public sealed class VehicleMergeResolver : IMergeResolver<VehicleState>
{
    public void RegisterRules(MergeEngine<VehicleState> engine)
    {
        // Example: override default rule for Speed
        engine.SetRule(v => v.Speed, MergeRules.MaxDouble());

        // Example: custom rule for sensors
        engine.SetRule(v => v.ActiveSensors, MergeRules.SetUnion<string>());
    }
}
