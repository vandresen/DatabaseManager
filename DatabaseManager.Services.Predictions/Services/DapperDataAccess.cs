using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.Predictions.Services
{
    public class DapperDataAccess : IDatabaseAccess
    {
        public Task<IEnumerable<T>> LoadData<T, U>(string storedProcedure, U parameters, string connectionString)
        {
            throw new NotImplementedException();
        }
    }
}
