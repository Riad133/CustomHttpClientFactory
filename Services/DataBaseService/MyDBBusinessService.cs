using Dapper;
using HttpClientFactoryCustom.Repository;

namespace HttpClientFactoryCustom.Services.DataBaseService
{
    public class MyDBBusinessService
    {
        private readonly Func<DatabaseName, IUnitOfWork> _unitOfWorkFactory;

        public MyDBBusinessService(Func<DatabaseName, IUnitOfWork> unitOfWorkFactory)
        {
            _unitOfWorkFactory = unitOfWorkFactory;
        }

        public async Task PerformComplexOperationAsync()
        {
            using var mainUow = _unitOfWorkFactory(DatabaseName.MainDb);
            using var reportUow = _unitOfWorkFactory(DatabaseName.ReportingDb);

            try
            {
                mainUow.BeginTransaction();
                reportUow.BeginTransaction();

                // Example: Add real parameters as needed
                var mainParams = new DynamicParameters();
                var reportParams = new DynamicParameters();

                await mainUow.ExecuteAsync("sp_DoMainDbWork", mainParams);
                await reportUow.ExecuteAsync("sp_DoReportingDbWork", reportParams);

                mainUow.Commit();
                reportUow.Commit();
            }
            catch (Exception ex)
            {
                // Optionally log the exception here

                try { mainUow.Rollback(); } catch { /* Optionally log rollback failure */ }
                try { reportUow.Rollback(); } catch { /* Optionally log rollback failure */ }
                throw;
            }
        }

    }
}
