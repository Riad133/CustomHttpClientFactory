using Dapper;
using System.Data.Common;
using System.Data;

namespace HttpClientFactoryCustom.Repository.UnitOfWork
{
    public class OracleUnitOfWork: IOracleUnitOfWork, IDisposable
    {
        private readonly IDbConnection _connection;
        private IDbTransaction _transaction;
        private bool _transactionStarted;
        private bool _committed = false;

        public OracleUnitOfWork(IOraclelConnectionFactory connectionFactory, DatabaseName dbName)
        {
            _connection = connectionFactory.CreateConnection(dbName); // Should return OracleConnection
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
            try
            {
                _transaction?.Commit();
                _committed = true;
            }
            catch (Exception ex)
            {
                Rollback();
                throw new Exception("Commit failed. Transaction has been rolled back.", ex);
            }
        }

        public async Task SaveChangesAsync() => await Task.Run(Commit);

        public void Rollback()
        {
            try
            {
                _transaction?.Rollback();
            }
            catch (Exception ex)
            {
                throw new Exception("Rollback failed.", ex);
            }
        }

        public async Task<int> ExecuteAsync(string procName, DynamicParameters parameters)
        {
            try
            {
                return await _connection.ExecuteAsync(procName, parameters, _transaction, commandType: CommandType.StoredProcedure);
            }
            catch (Exception ex)
            {
                Rollback();
                throw new Exception($"Execution failed for procedure {procName}.", ex);
            }
        }

        public async Task<T> QuerySingleAsync<T>(string procName, DynamicParameters parameters)
        {
            try
            {
                return await _connection.QueryFirstOrDefaultAsync<T>(procName, parameters, _transaction, commandType: CommandType.StoredProcedure);
            }
            catch (Exception ex)
            {
                throw new Exception($"QuerySingle failed for procedure {procName}.", ex);
            }
        }

        public async Task<IEnumerable<T>> QueryAsync<T>(string procName, DynamicParameters parameters)
        {
            try
            {
                return await _connection.QueryAsync<T>(procName, parameters, _transaction, commandType: CommandType.StoredProcedure);
            }
            catch (Exception ex)
            {
                throw new Exception($"QueryAsync failed for procedure {procName}.", ex);
            }
        }

        public async Task<List<IEnumerable<object>>> QueryMultipleAsync(
            string sql,
            object parameters = null,
            CommandType commandType = CommandType.StoredProcedure,
            params Type[] types)
        {
            var resultSets = new List<IEnumerable<object>>();
            try
            {
                using var multi = await _connection.QueryMultipleAsync(sql, parameters, _transaction, 360, commandType);
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
            }
            catch (Exception ex)
            {
                throw new Exception($"QueryMultiple failed for procedure {sql}.", ex);
            }

            return resultSets;
        }

        public async Task<List<DataTable>> QueryMultipleToDataTablesAsync(string procName, DynamicParameters parameters)
        {
            var tables = new List<DataTable>();
            try
            {
                using var reader = await _connection.ExecuteReaderAsync(procName, parameters, _transaction, commandType: CommandType.StoredProcedure);
                var dbReader = (DbDataReader)reader;

                do
                {
                    var dt = new DataTable();
                    dt.Load(dbReader);
                    tables.Add(dt);
                } while (await dbReader.NextResultAsync());
            }
            catch (Exception ex)
            {
                throw new Exception($"QueryMultipleToDataTablesAsync failed for procedure {procName}.", ex);
            }

            return tables;
        }

        public async Task<DataTable> GetDataTableAsync(string procName, DynamicParameters parameters)
        {
            try
            {
                using var reader = await _connection.ExecuteReaderAsync(procName, parameters, _transaction, commandType: CommandType.StoredProcedure);
                var dt = new DataTable();
                dt.Load(reader);
                return dt;
            }
            catch (Exception ex)
            {
                throw new Exception($"GetDataTableAsync failed for procedure {procName}.", ex);
            }
        }

        public void Dispose()
        {
            try
            {
                if (!_committed && _transactionStarted)
                {
                    Rollback();
                }
            }
            catch
            {
                // Suppress dispose-time rollback errors
            }
            finally
            {
                _transaction?.Dispose();
                _connection?.Close();
                _connection?.Dispose();
            }
        }
    }
}
