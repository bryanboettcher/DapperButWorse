using System;
using System.Collections.Generic;
using System.Data;

namespace SlimGenerator.Core.Extensions;

public static class SqlExtensions
{
    public static readonly IReadOnlyDictionary<Type, SqlDbType> DbTypeMap;

    static SqlExtensions()
    {
        DbTypeMap = new Dictionary<Type, SqlDbType>()
        {
            { typeof (bool), SqlDbType.Bit },
            { typeof (bool?), SqlDbType.Bit },
            { typeof (byte), SqlDbType.TinyInt },
            { typeof (byte?), SqlDbType.TinyInt },
            { typeof (string), SqlDbType.NVarChar },
            { typeof (DateTime), SqlDbType.DateTime },
            { typeof (DateTime?), SqlDbType.DateTime },
            { typeof (short), SqlDbType.SmallInt },
            { typeof (short?), SqlDbType.SmallInt },
            { typeof (int), SqlDbType.Int },
            { typeof (int?), SqlDbType.Int },
            { typeof (long), SqlDbType.BigInt },
            { typeof (long?), SqlDbType.BigInt },
            { typeof (decimal), SqlDbType.Decimal },
            { typeof (decimal?), SqlDbType.Decimal },
            { typeof (double), SqlDbType.Float },
            { typeof (double?), SqlDbType.Float },
            { typeof (float), SqlDbType.Real },
            { typeof (float?), SqlDbType.Real },
            { typeof (TimeSpan), SqlDbType.Time },
            { typeof (Guid), SqlDbType.UniqueIdentifier },
            { typeof (Guid?), SqlDbType.UniqueIdentifier },
            { typeof (byte[]), SqlDbType.Binary },
            { typeof (byte?[]), SqlDbType.Binary },
            { typeof (char[]), SqlDbType.Char },
            { typeof (char?[]), SqlDbType.Char },
        };
    }

    public static TableValueParameter AsTableParameter<T>(this IEnumerable<T> input, string tvpName, string columnName, int? maxLength = default) =>
        input.AsTableParameter(tvpName)
            .AddParameter(x => x, columnName, maxLength)
            .Build();

    public static TvpBuilder<TInput> AsTableParameter<TInput>(this IEnumerable<TInput> input, string tvpName)
    {
        return new TvpBuilder<TInput>(tvpName, input);
    }
}