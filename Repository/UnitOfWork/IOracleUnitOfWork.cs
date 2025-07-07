using Dapper;
using System.Data;

namespace HttpClientFactoryCustom.Repository.UnitOfWork
{
    public interface IOracleUnitOfWork
    {
        void BeginTransaction();
        void Commit();
        void Rollback();
        Task SaveChangesAsync();
        Task<int> ExecuteAsync(string procName, DynamicParameters parameters);
        Task<T> QuerySingleAsync<T>(string procName, DynamicParameters parameters);
        Task<IEnumerable<T>> QueryAsync<T>(string procName, DynamicParameters parameters);
        Task<List<IEnumerable<object>>> QueryMultipleAsync(string sql, object parameters = null, CommandType commandType = CommandType.StoredProcedure, params Type[] types);
        Task<List<DataTable>> QueryMultipleToDataTablesAsync(string procName, DynamicParameters parameters);
        Task<DataTable> GetDataTableAsync(string procName, DynamicParameters parameters);
    }
}
