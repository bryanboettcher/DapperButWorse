using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace SlimGenerator.Core.Extensions;

public static class PropertyInfoExtensions
{
    private static readonly HashSet<Type> PrimaryKeyTypes =
    [
        typeof(int),
        typeof(long),
        typeof(Guid),
    ];

    public static bool IsPotentialKey(this PropertyInfo property) 
        => PrimaryKeyTypes.Contains(property.PropertyType) && property.Name == "Id";

    public static SqlDbType GetSqlDbType(this PropertyInfo property)
    {
        var propertyType = property.PropertyType;
        if (propertyType.IsEnum)
            propertyType = propertyType.GetEnumUnderlyingType();

        if (SqlExtensions.DbTypeMap.TryGetValue(propertyType, out var type))
            return type;

        throw new InvalidOperationException($"Cannot convert a {property.PropertyType} to a SQL data type");
    }
}