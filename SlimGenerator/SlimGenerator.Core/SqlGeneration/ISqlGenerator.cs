using System.Collections.Generic;
using System.Reflection;

namespace SlimGenerator.Core.SqlGeneration;

/// <summary>
/// Operations to generate CRUD SQL from entities.  Covered by
/// test in TaylorSummit.Core.Persistence.Tests.
/// </summary>
/// <typeparam name="TEntity"></typeparam>
public interface ISqlGenerator<TEntity> where TEntity: class, new()
{
    /// <summary>
    /// Builds a SELECT statement (with SQL Server syntax) that fetches
    /// all columns, and an optional TOP number of rows.
    /// </summary>
    /// <param name="maxRows"></param>
    /// <returns></returns>
    string GetSelectSql(int? maxRows = default);

    /// <summary>
    /// Builds a DELETE statement (with SQL Server syntax) that removes
    /// a single entity with all the primary keys it can find.
    /// </summary>
    string GetDeleteSql();

    /// <summary>
    /// Builds an INSERT statement (with SQL Server syntax)
    /// that includes all non-null and non-key fields, and
    /// attempts to get the generated PK back out (be it
    /// numeric or Guid).
    /// </summary>
    string GetInsertSql(TEntity? entity);

    /// <summary>
    /// Builds an UPDATE statement (with SQL Server syntax)
    /// that includes all non-null or dirty fields (if using
    /// a proxy) 
    /// </summary>
    string GetUpdateSql(TEntity? entity);

    /// <summary>
    /// Gets any changed or non-default non-key fields on the
    /// provided entity.  All non-key fields are returned if
    /// the entity is null.
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="forCreate"></param>
    /// <returns></returns>
    IEnumerable<PropertyInfo> GetDirtyFields(TEntity? entity, bool forCreate);

    /// <summary>
    /// Gets the defined key fields on the provided entity.
    /// </summary>
    /// <param name="forCreate"></param>
    /// <returns></returns>
    IEnumerable<PropertyInfo> GetKeys(bool forCreate);
}