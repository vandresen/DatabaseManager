using Dapper;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.Rules.Services
{
    public class DapperDataAccess : IDatabaseAccess
    {
        public Task DeleteData<T>(string storedProcedure, T parameters, string connectionString)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<T>> LoadData<T, U>(string storedProcedure, U parameters, string connectionString)
        {
            using IDbConnection cnn = new SqlConnection(connectionString);
            return await cnn.QueryAsync<T>(storedProcedure, parameters, commandType: CommandType.StoredProcedure); throw new NotImplementedException();
        }

        public Task SaveData<T>(string storedProcedure, T parameters, string connectionString)
        {
            throw new NotImplementedException();
        }
    }
}
