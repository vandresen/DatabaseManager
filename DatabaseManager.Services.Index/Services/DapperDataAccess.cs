using Dapper;
using Microsoft.Data.SqlClient;
using Polly;
using System.Data;

namespace DatabaseManager.Services.Index.Services
{
    public class DapperDataAccess : IDapperDataAccess
    {
        private const int SqlCommandTimeout = 1000;
        private const int RetryCount = 3;

        private async Task<T> ExecuteWithRetry<T>(Func<Task<T>> operation)
        {
            var retryPolicy = Policy
                .Handle<SqlException>()
                .WaitAndRetryAsync(
                    RetryCount,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

            return await retryPolicy.ExecuteAsync(operation);
        }

        private async Task ExecuteWithRetry(Func<Task> operation)
        {
            var retryPolicy = Policy
                .Handle<SqlException>()
                .WaitAndRetryAsync(
                    RetryCount,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

            await retryPolicy.ExecuteAsync(operation);
        }

        public async Task<T> Count<T, U>(
            string sql,
            U parameters,
            string connectionString)
        {
            return await ExecuteWithRetry(async () =>
            {
                using IDbConnection cnn = new SqlConnection(connectionString);

                return await cnn.ExecuteScalarAsync<T>(
                    sql,
                    parameters);
            });
        }

        public async Task<IEnumerable<T>> LoadData<T, U>(
            string storedProcedure,
            U parameters,
            string connectionString)
        {
            return await ExecuteWithRetry(async () =>
            {
                using IDbConnection cnn = new SqlConnection(connectionString);

                return await cnn.QueryAsync<T>(
                    storedProcedure,
                    parameters,
                    commandType: CommandType.StoredProcedure);
            });
        }

        public async Task<IEnumerable<T>> ReadData<T>(
            string sql,
            string connectionString)
        {
            return await ExecuteWithRetry(async () =>
            {
                using IDbConnection cnn = new SqlConnection(connectionString);

                return await cnn.QueryAsync<T>(sql);
            });
        }

        public async Task SaveData<T>(
            string storedProcedure,
            T parameters,
            string connectionString)
        {
            await ExecuteWithRetry(async () =>
            {
                using IDbConnection cnn = new SqlConnection(connectionString);

                await cnn.ExecuteAsync(
                    storedProcedure,
                    parameters,
                    commandType: CommandType.StoredProcedure);
            });
        }

        public async Task<T> SaveDataScalar<T, U>(
            string storedProcedure,
            U parameters,
            string connectionString)
        {
            return await ExecuteWithRetry(async () =>
            {
                using IDbConnection cnn = new SqlConnection(connectionString);

                return await cnn.ExecuteScalarAsync<T>(
                    storedProcedure,
                    parameters,
                    commandType: CommandType.StoredProcedure);
            });
        }

        public async Task SaveDataSQL<T>(
            string sql,
            T parameters,
            string connectionString)
        {
            await ExecuteWithRetry(async () =>
            {
                using IDbConnection cnn = new SqlConnection(connectionString);

                await cnn.ExecuteAsync(
                    sql,
                    parameters);
            });
        }
    }
}