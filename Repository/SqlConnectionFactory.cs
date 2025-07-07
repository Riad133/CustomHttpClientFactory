using Microsoft.Data.SqlClient;
using System.Data;

namespace HttpClientFactoryCustom.Repository
{
    public class SqlConnectionFactory : ISqlConnectionFactory
    {
        private readonly ConnectionStringProvider _provider;

        public SqlConnectionFactory(ConnectionStringProvider provider)
        {
            _provider = provider;
        }

        public IDbConnection CreateConnection(DatabaseName db)
        {
            return new SqlConnection(_provider.GetConnectionString(db));
        }
    }

}
