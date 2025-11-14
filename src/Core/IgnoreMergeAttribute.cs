using System;
using System.Collections.Generic;
using System.Text;

namespace MergeEngine.Core;

/// <summary>
/// Marks a property to be ignored during merge operations.
/// Useful for volatile UI state, transient cache fields,
/// or values synchronized by other mechanisms.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class IgnoreMergeAttribute : Attribute { }
