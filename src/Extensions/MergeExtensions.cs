using MergeEngine.Contracts;

namespace MergeEngine.Extensions;
public static class MergeObjectExtensions
{
    /// <summary>
    /// Safely applies a local update to the object and increments its vector clock.
    /// This ensures every state change results in a causal version update automatically.
    /// </summary>
    public static void Update<T>(this T mergeObject, Action<T> updateAction, string nodeId)
        where T : IMergeObject
    {
        if(updateAction == null)
            throw new ArgumentNullException(nameof(updateAction));

        updateAction(mergeObject);
        mergeObject.Touch(nodeId);
    }
}
