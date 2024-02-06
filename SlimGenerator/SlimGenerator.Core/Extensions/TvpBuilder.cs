using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.SqlServer.Server;

namespace SlimGenerator.Core.Extensions;

public class TvpBuilder<TInput>
{
    private class ValueProvider
    {
        public Func<TInput, object?> Selector { get; set; }
        public int Order { get; set; }
        public string ColumnName { get; set; }
        public SqlDbType DataType { get; set; }
        public int? MaxLength { get; set; }
    }

    private readonly List<ValueProvider> _valueProviders = new();
    private readonly string _tvpName;
    private readonly IEnumerable<TInput> _input;

    public TvpBuilder(string tvpName, IEnumerable<TInput> input)
    {
        _tvpName = tvpName;
        _input = input;
    }

    /// <summary>
    /// Maps a property in a POCO to a column in a TVP.
    /// </summary>
    public TvpBuilder<TInput> AddParameter<TProp>(Func<TInput, TProp> selector, string columnName, int? maxLength = default)
    {
        var targetType = typeof(TProp);
        
        if (!SqlExtensions.DbTypeMap.TryGetValue(targetType, out var sqlType))
            throw new InvalidOperationException($"No built-in conversion from {targetType.Name} to an underlying SqlDbType");

        if ((sqlType == SqlDbType.VarChar || sqlType == SqlDbType.NVarChar) && maxLength is null || maxLength == 0)
            maxLength = 4000;

        var order = _valueProviders.Count;

        var provider = new ValueProvider
        {
            Selector = o => selector(o), // box the return type for generics reasons
            ColumnName = columnName,
            Order = order,
            DataType = sqlType,
            MaxLength = maxLength
        };

        _valueProviders.Add(provider);
        return this;
    }

    /// <summary>
    /// Converts the builder into a usable TableValueParameter
    /// </summary>
    /// <returns></returns>
    public TableValueParameter Build()
    {
        return new TableValueParameter
        {
            DataRecords = BuildRecords(),
            Name = _tvpName
        };
    }
    private IEnumerable<SqlDataRecord> BuildRecords()
    {
        static SqlMetaData CreateMetaData(ValueProvider v) =>
            v.MaxLength.HasValue
                ? new SqlMetaData(v.ColumnName, v.DataType, v.MaxLength.Value)
                : new SqlMetaData(v.ColumnName, v.DataType);

        var dataRecord = new SqlDataRecord(
            _valueProviders.Select(CreateMetaData).ToArray()
        );

        foreach (var item in _input)
        {
            foreach (var provider in _valueProviders)
            {
                dataRecord.SetValue(provider.Order, provider.Selector(item));
            }

            yield return dataRecord;
        }
    }

}