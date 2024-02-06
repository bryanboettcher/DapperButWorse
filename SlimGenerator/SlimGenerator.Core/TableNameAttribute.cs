using System;

namespace SlimGenerator.Core;

[AttributeUsage(AttributeTargets.Class)]
public sealed class TableNameAttribute : Attribute
{
    public TableNameAttribute(string tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName) || tableName.Length < 2)
            throw new ArgumentException($"Provided table name is invalid: '{tableName}'");

        TableName = tableName[0] != '[' 
            ? '[' + tableName + ']' 
            : tableName;
    }

    /// <summary>
    /// User-defined table name for this entity.
    /// </summary>
    public string TableName { get; }
}