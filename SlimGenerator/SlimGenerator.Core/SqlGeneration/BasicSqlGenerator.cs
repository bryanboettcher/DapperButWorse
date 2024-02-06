using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using SlimGenerator.Core.Extensions;

namespace SlimGenerator.Core.SqlGeneration;

/// <summary>
/// Fairly naively builds readable SQL for trivial CRUD operations.  Expected to
/// be treated as a black box, and not generate more complex SQL operations.
/// </summary>
/// <typeparam name="TEntity"></typeparam>
public class MssqlSqlGenerator<TEntity> : ISqlGenerator<TEntity> 
    where TEntity : class, new()
{
    private readonly Type _entityType;
    private readonly string _entityName;
    private readonly string _tableName;
    private readonly TEntity _sample;
    private readonly List<PropertyInfo> _properties;
    private readonly List<PropertyInfo> _allKeys;
    private readonly List<PropertyInfo> _generatedKeys;
    private readonly List<PropertyInfo> _computedColumns;
    private readonly List<PropertyInfo> _nonKeys;

    public MssqlSqlGenerator()
    {
        _entityType = typeof(TEntity);
        _entityName = _entityType.Name;
        _sample = new TEntity();

        _properties = _entityType.GetProperties().ToList();

        if (_properties.Count == 0)
            throw new InvalidOperationException($"Cannot use {_entityName} with MssqlSqlGenerator: it does not expose any public properties");

        _tableName = GetTableName();
        _allKeys = BuildKeys(true).ToList();
        _generatedKeys = BuildKeys(false).ToList();
        _nonKeys = _properties.Except(_generatedKeys).ToList();
        _computedColumns = _nonKeys.Where(k => k.GetCustomAttribute<ComputedAttribute>() is not null).ToList();
    }

    /// <inheritdoc />
    public virtual string GetInsertSql(TEntity? entity)
    {
        if (_nonKeys.Count == 0)
            throw new InvalidOperationException($"Cannot generate an INSERT statement for {_entityName}: it does not have non-key properties");

        if (_generatedKeys.Count > 1)
            throw new InvalidOperationException($"Cannot generate an INSERT statement for {_entityName}: it has multiple primary keys");

        var key = _generatedKeys.Count == 1 ? _generatedKeys[0] : null;
        var builder = new StringBuilder();

        // GetDirtyFields will exclude all fields that are PKs.
        // If the entity supplied is a proxy, then it will also only
        // include the changed fields -- under the assumption that if
        // they weren't important enough to set, they aren't important
        // enough to insert.
        var names = GetDirtyFields(entity, true)
            .Select(p => p.Name)
            .ToList();

        builder.Append($"INSERT INTO {_tableName} (");
        builder.Append(string.Join(", ", names));
        builder.Append(')');

        if (key is not null && key.PropertyType == typeof(Guid))
        {
            builder.Append($" OUTPUT INSERTED.{key.Name}");
        }

        builder.Append(" VALUES (");
        builder.Append(string.Join(", ", names.Select(p => $"@{p}")));
        builder.Append(')');

        if (key is not null && key.PropertyType != typeof(Guid))
        {
            builder.Append("; SELECT SCOPE_IDENTITY()");
        }

        if (key is null)
        {
            // reader is expecting *something*, so we just generate an integer
            // to discard it, since no PK can be set.
            builder.Append("; SELECT 0");
        }

        builder.Append(';');
        return builder.ToString();
    }

    /// <inheritdoc />
    public virtual string GetSelectSql(int? maxRows = default)
    {
        var entityType = typeof(TEntity);
        var builder = new StringBuilder();

        builder.Append("SELECT ");
        if (maxRows.HasValue)
            builder.Append($"TOP ({maxRows.Value}) ");

        builder.Append(string.Join(", ", entityType.GetProperties().Select(p => $"[{p.Name}]")));
        builder.Append($" FROM {_tableName}");

        return builder.ToString();
    }

    /// <inheritdoc />
    public virtual string GetUpdateSql(TEntity? entity)
    {
        // GetDirtyFields will exclude all fields that are PKs.
        // If the entity supplied is a proxy, then it will also only
        // include the changed fields -- under the assumption that if
        // they weren't important enough to set, they aren't important
        // enough to insert.
        var names = GetDirtyFields(entity, false)
            .Except(_generatedKeys)
            .Select(p => p.Name);
        
        var sb = new StringBuilder();
        sb.Append("UPDATE ");
        sb.Append(_tableName);
        sb.Append(" SET ");
        sb.Append(string.Join(", ", names.Select(n => $"[{n}] = @{n}")));
        sb.Append(" WHERE ");
        sb.Append(GetKeysStatement());
        sb.Append(';');

        return sb.ToString();
    }

    /// <inheritdoc />
    public virtual string GetDeleteSql()
    {
        if (_allKeys.Count == 0)
            throw new InvalidOperationException($"Cannot find an Id property for automatic delete SQL building on {_entityName}");
        
        var sb = new StringBuilder();
        sb.Append("DELETE FROM ");
        sb.Append(_tableName);
        sb.Append(" WHERE ");
        sb.Append(GetKeysStatement());
        sb.Append(';');

        return sb.ToString();
    }
    
    /// <inheritdoc />
    public IEnumerable<PropertyInfo> GetDirtyFields(TEntity? entity, bool forCreate)
    {
        // for the purposes of mutating, we don't want to include computed columns since can't change those anyway
        var keys = (forCreate ? _generatedKeys : _allKeys).Union(_computedColumns).ToList();
        
        if (entity is IProxyDetails proxy)
        {
            var dirtyFields = proxy.GetDirtyFields().ToHashSet(StringComparer.OrdinalIgnoreCase);
            return _nonKeys.Where(p => dirtyFields.Contains(p.Name));
        }

        return entity is null
            ? _nonKeys.Where(p => !keys.Contains(p))
            : _nonKeys.Where(p => !keys.Contains(p) && p.GetValue(entity) is not null);
    }

    /// <param name="forCreate"></param>
    /// <inheritdoc />
    public IEnumerable<PropertyInfo> GetKeys(bool forCreate) => forCreate 
        ? _generatedKeys 
        : _allKeys;

    private string GetTableName()
    {
        var tableAttribute = (_entityType.GetCustomAttribute<TableNameAttribute>());

        return tableAttribute is not null
            ? tableAttribute.TableName
            : $"[{_entityType.Name}s]";
    }
    
    private IEnumerable<PropertyInfo> BuildKeys(bool includeExplicit)
    {
        var returned = false;
        foreach (var p in _properties)
        {
            if (p.GetCustomAttribute<KeyAttribute>() is not null)
            {
                returned = true;
                yield return p;
                continue;
            }

            if (includeExplicit && p.GetCustomAttribute<ExplicitKeyAttribute>() is not null)
            {
                returned = true;
                yield return p;
                continue;
            }

            // we don't want to just ignore all [Key] / [ExplicitKey] fields
            // and then return an "Id" column anyway if that's not actually
            // something defined as the PK!
            if (p.IsPotentialKey() && !returned)
            {
                yield return p;
                continue;
            }
        }
    }

    private string GetKeysStatement()
    {
        var keyParams = _allKeys.Select(k => k.Name).Select(n => $"[{n}] = @{n}");
        return string.Join(" AND ", keyParams);
    }
}
