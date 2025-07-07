using System.Data;

namespace HttpClientFactoryCustom.Repository
{
    public interface IOraclelConnectionFactory
    {
        IDbConnection CreateConnection(DatabaseName dbName);
    }
}
