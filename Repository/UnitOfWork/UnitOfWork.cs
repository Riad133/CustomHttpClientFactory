using System.Data;
using System.Data.Common;
using Dapper;
using HttpClientFactoryCustom.Repository;



public class UnitOfWork : IUnitOfWork
{
    private readonly IDbConnection _connection;
    private IDbTransaction _transaction;
    private bool _transactionStarted;

    public UnitOfWork(ISqlConnectionFactory connectionFactory, DatabaseName dbName)
    {
        _connection = connectionFactory.CreateConnection(dbName);
        _connection.Open();
    }

    public void BeginTransaction()
    {
        if (_transaction == null)
        {
            _transaction = _connection.BeginTransaction();
            _transactionStarted = true;
        }
    }

    public void Commit()
    {
        _transaction?.Commit();
        _transaction?.Dispose();
        _transaction = null;
        _transactionStarted = false;
    }

    public void Rollback()
    {
        _transaction?.Rollback();
        _transaction?.Dispose();
        _transaction = null;
        _transactionStarted = false;
    }

    public async Task<int> ExecuteAsync(string procName, DynamicParameters parameters)
    {
        return await _connection.ExecuteAsync(procName, parameters, _transaction, commandType: CommandType.StoredProcedure);
    }

    public async Task<T> QuerySingleAsync<T>(string procName, DynamicParameters parameters)
    {
        return await _connection.QueryFirstOrDefaultAsync<T>(procName, parameters, _transaction, commandType: CommandType.StoredProcedure);
    }

    public async Task<IEnumerable<T>> QueryAsync<T>(string procName, DynamicParameters parameters)
    {
        return await _connection.QueryAsync<T>(procName, parameters, _transaction, commandType: CommandType.StoredProcedure);
    }

    public async Task<List<IEnumerable<object>>> QueryMultipleAsync(string sql, object parameters = null, CommandType commandType = CommandType.StoredProcedure, params Type[] types)
    {
        var resultSets = new List<IEnumerable<object>>();
        using var multi = await _connection.QueryMultipleAsync(sql, parameters, _transaction,360, commandType);

        foreach (var type in types)
        {
            var method = typeof(SqlMapper.GridReader)
                .GetMethod("ReadAsync", new[] { typeof(bool) });

            if (method == null)
                throw new InvalidOperationException("Could not find ReadAsync<T>(bool) method.");

            var generic = method.MakeGenericMethod(type);
            var task = (Task)generic.Invoke(multi, new object[] { true });

            await task.ConfigureAwait(false);

            var resultProp = task.GetType().GetProperty("Result");
            var result = (IEnumerable<object>)resultProp.GetValue(task);
            resultSets.Add(result);
        }

        return resultSets;
    }

    public async Task<List<DataTable>> QueryMultipleToDataTablesAsync(string procName, DynamicParameters parameters)
    {
        using var reader = await _connection.ExecuteReaderAsync(procName, parameters, _transaction, commandType: CommandType.StoredProcedure);
        var dbReader = (DbDataReader)reader;
        var tables = new List<DataTable>();

        do
        {
            var dt = new DataTable();
            dt.Load(dbReader);
            tables.Add(dt);
        } while (await dbReader.NextResultAsync());

        return tables;
    }

    public async Task<DataTable> GetDataTableAsync(string procName, DynamicParameters parameters)
    {
        using var reader = await _connection.ExecuteReaderAsync(procName, parameters, _transaction, commandType: CommandType.StoredProcedure);
        var dt = new DataTable();
        dt.Load(reader);
        return dt;
    }

    public void Dispose()
    {
        if (_transactionStarted)
        {
            Commit();
        }

        _transaction?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
    }
}
