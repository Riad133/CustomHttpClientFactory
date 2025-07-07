using Microsoft.Data.SqlClient;
using System.Data;

namespace HttpClientFactoryCustom.Repository
{
    public class SqlConnectionFactory : ISqlConnectionFactory
    {
        private readonly ConnectionStringProvider _connectionStringProvider;

        public SqlConnectionFactory(ConnectionStringProvider connectionStringProvider)
        {
            _connectionStringProvider = connectionStringProvider;
        }

        public IDbConnection CreateConnection(DatabaseName dbName)
        {
            var connStr = _connectionStringProvider.GetConnectionString(dbName);
            return new SqlConnection(connStr);
        }
    }
}
