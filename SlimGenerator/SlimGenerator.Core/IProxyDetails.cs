using System.Collections.Generic;

namespace SlimGenerator.Core;

/// <summary>
/// Used at runtime to decorate an entity for change tracking.
/// </summary>
public interface IProxyDetails
{
    /// <summary>
    /// Called to forget all pending changes on the object.
    /// </summary>
    void ResetChanges();

    /// <summary>
    /// Get the current dirty fields for the object.
    /// </summary>
    /// <returns></returns>
    IEnumerable<string> GetDirtyFields();
}