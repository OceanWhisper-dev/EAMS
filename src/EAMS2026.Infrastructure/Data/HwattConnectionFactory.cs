using Microsoft.Data.SqlClient;
using System.Data;

namespace EAMS2026.Infrastructure.Data;

public class HwattConnectionFactory
{
    private readonly string _connectionString;

    public HwattConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public IDbConnection CreateConnection()
    {
        var connStr = ErpConnectionFactory.AppendSqlServerCompat(_connectionString);
        return new SqlConnection(connStr);
    }
}