using Microsoft.Data.SqlClient;
using System.Data;

namespace HttpClientFactoryCustom.Repository
{
    public class ConnectionStringProvider
    {
        private readonly IConfiguration _config;

        public ConnectionStringProvider(IConfiguration config)
        {
            _config = config;
        }

        public string GetConnectionString(DatabaseName db)
        {
            return db switch
            {
                DatabaseName.MainDb => _config.GetConnectionString("MainDb"),
                DatabaseName.ReportingDb => _config.GetConnectionString("ReportingDb"),
                DatabaseName.ArchiveDb => _config.GetConnectionString("ArchiveDb"),
                _ => throw new ArgumentException("Unknown database")
            };
        }
    }

    

  

}
