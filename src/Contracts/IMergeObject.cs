using MergeEngine.Core;

namespace MergeEngine.Contracts;

/// <summary>
/// Represents an object that participates in distributed merge
/// and maintains a vector clock describing its version.
/// </summary>
public interface IMergeObject
{
    /// <summary>
    /// Gets or sets the vector clock associated with this object instance.
    /// </summary>
    VectorClock Clock { get; set; }

    /// <summary>
    /// Called whenever any local modification occurs so vector clock versioning is guaranteed.
    /// </summary>
    void Touch(string nodeId)
    {
        Clock.Increment(nodeId);
    }
}
