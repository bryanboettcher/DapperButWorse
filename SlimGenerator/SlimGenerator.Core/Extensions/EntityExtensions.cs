namespace SlimGenerator.Core.Extensions;

public static class EntityExtensions
{
    /// <summary>
    /// Attempts to extract a primary key from an entity type.
    /// </summary>
    /// <remarks>
    /// Uses a custom search order, as follows:
    /// * First property with a [Key] attribute
    /// * First property with an [ExplicitKey] attribute
    /// * First property with a "proper" datatype, named "Id"  (int, long, Guid)
    /// </remarks>

}