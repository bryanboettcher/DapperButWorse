using System.Collections.Generic;
using Microsoft.SqlServer.Server;

namespace SlimGenerator.Core;

public class TableValueParameter
{
    public required string Name { get; init; }
    public required IEnumerable<SqlDataRecord> DataRecords { get; init; }
}
