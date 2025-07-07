using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace HttpClientFactoryCustom.Repository
{
    public class OracleConnectionFactory : IOraclelConnectionFactory
    {
        private readonly IConfiguration _configuration;
        private readonly Dictionary<DatabaseName, string> _connectionStrings;

        public OracleConnectionFactory(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionStrings = new Dictionary<DatabaseName, string>
        {
            { DatabaseName.ArchiveDb, _configuration.GetConnectionString("OracleFinance") },
            { DatabaseName.ArchiveDb, _configuration.GetConnectionString("OracleHR") }
        };
        }

        public IDbConnection CreateConnection(DatabaseName dbName)
        {
            var connStr = _connectionStrings[dbName];
            return new OracleConnection(connStr);
        }
    }

}
