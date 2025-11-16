using MergeEngine.Contracts;

namespace MergeEngine
{
    /// <summary>
    /// Allows injecting custom merge rules for a given mergeable object type.
    /// This acts as a module / plugin for merge configuration.
    /// </summary>
    public interface IMergeResolver<TObject> where TObject : IMergeObject, new()
    {
        /// <summary>
        /// Register merge rules into the merge engine.
        /// Called by the MergeEngine constructor.
        /// </summary>
        void RegisterRules(MergeEngine<TObject> engine);         
    }
}