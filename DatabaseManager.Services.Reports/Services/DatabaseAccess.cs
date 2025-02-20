using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DatabaseManager.Services.Reports.Services
{
    public class DatabaseAccess : IDatabaseAccess
    {
        
        public async Task<IEnumerable<T>> LoadData<T, U>(string storedProcedure, U parameters, string connectionString)
        {
            using IDbConnection cnn = new SqlConnection(connectionString);
            return await cnn.QueryAsync<T>(storedProcedure, parameters, commandType: CommandType.StoredProcedure);
        }
    }
}
