using System.Data;

namespace HttpClientFactoryCustom.Repository
{
    public interface ISqlConnectionFactory
    {
        IDbConnection CreateConnection(DatabaseName dbName);
    }
}
