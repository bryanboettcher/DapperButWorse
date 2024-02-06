using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;
using SlimGenerator.Core.EntityGeneration;
using DateTime = System.DateTime;

namespace SlimGenerator.Core.Extensions;

public static class SqlDataReaderExtensions
{
    private static Dictionary<(Type, Type), Func<object, object>> Converters = new();
    static SqlDataReaderExtensions()
    {
        Converters.Add((typeof(int), typeof(bool)), i => (int)i != 0);
        Converters.Add((typeof(int), typeof(byte)), i => (byte)(int)i);
        Converters.Add((typeof(char), typeof(string)), c => c);
    }

    public static async IAsyncEnumerable<TEntity> MapReaderToEntities<TEntity>(this SqlDataReader reader)
    {
        var mapper = GetMapper<TEntity>();

        while (await reader.ReadAsync())
        {
            var value = (TEntity) mapper(reader);
            yield return value;
        }
    }

    private static Func<SqlDataReader, object> GetMapper<TEntity>()
    {
        if (typeof(TEntity) == typeof(int)) return reader => reader.GetInt32(0);
        if (typeof(TEntity) == typeof(byte)) return reader => reader.GetByte(0);
        if (typeof(TEntity) == typeof(short)) return reader => reader.GetInt16(0);
        if (typeof(TEntity) == typeof(long)) return reader => reader.GetInt64(0);
        if (typeof(TEntity) == typeof(string)) return reader => reader.GetString(0);
        if (typeof(TEntity) == typeof(Guid)) return reader => reader.GetGuid(0);

        var mapper = new EntityMapper<TEntity>();
        return reader => mapper.MapResult(reader)!;
    }

    private class EntityMapper<TEntity>
    {
        static readonly PropertyInfo[] Properties = typeof(TEntity).GetProperties();

        public TEntity MapResult(SqlDataReader reader)
        {
            var entity = EntityProxying.ShouldProxyEntity<TEntity>()
                ? EntityProxying.CreateProxy<TEntity>()
                : Activator.CreateInstance<TEntity>();

            foreach (var prop in Properties)
            {
                var value = reader.GetValue(prop.Name);

                var source = value.GetType();
                var target = prop.PropertyType;

                if ((source != target) && Converters.TryGetValue((source, target), out var converter))
                {
                    value = converter(value);
                }

                if (value is DateTime d)
                    value = DateTime.SpecifyKind(d, DateTimeKind.Utc);

                if (value == DBNull.Value)
                    value = null;

                prop.SetValue(entity, value);

            }

            return entity;
        }
    }
}