namespace HttpClientFactoryCustom.Repository
{
    public class ConnectionStringProvider
    {


         private readonly IConfiguration _configuration;
    private readonly Dictionary<DatabaseName, string> _dbMap = new()
    {
        { DatabaseName.MainDb, "Default" },
        { DatabaseName.ReportingDb, "Reporting" },
        { DatabaseName.ArchiveDb, "Archive" }
    };

    public ConnectionStringProvider(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GetConnectionString(DatabaseName dbName)
    {
        if (!_dbMap.TryGetValue(dbName, out var configKey))
            throw new ArgumentException($"No connection mapping for {dbName}");

        return _configuration.GetConnectionString(configKey) 
               ?? throw new ArgumentException($"Connection string '{configKey}' not found.");
    }
    }
}
