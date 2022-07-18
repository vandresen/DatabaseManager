using Dapper;
using DatabaseManager.Common.Entities;
using DatabaseManager.Shared;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Common.Helpers
{
    public class IndexAccess
    {
        private string getSql = "Select IndexId, IndexNode.ToString() AS TextIndexNode, " +
            "IndexLevel, DataName, DataType, DataKey, QC_String, UniqKey, JsonDataObject, " +
            "Latitude, Longitude " +
            "from pdo_qc_index";

        public IndexAccess()
        {

        }

        public string GetSelectSQL()
        {
            return getSql;
        }

        public DataAccessDef GetDataAccessDefinition()
        {
            DataAccessDef dataAccessDef = new DataAccessDef();
            dataAccessDef.DataType = "Index";
            dataAccessDef.Select = getSql;
            dataAccessDef.Keys = "INDEXID";
            return dataAccessDef;
        }

        public List<IndexModel> SelectIndexesByQuery(string query, string connectionString)
        {
            List<IndexModel> indexResults = new List<IndexModel>();
            using (IDbConnection cnn = new SqlConnection(connectionString))
            {
                string sql = getSql + query;
                indexResults = cnn.Query<IndexModel>(sql).ToList();
            }
            return indexResults;
        }

        public int IndexCountByQuery(string query, string connectionString)
        {
            int count = 0;
            using (IDbConnection cnn = new SqlConnection(connectionString))
            {
                string sql = "select count(*) from pdo_qc_index " + query;
                count = cnn.ExecuteScalar<int>(sql);
            }
            return count;
        }

        public void ClearAllQCFlags(string connectionString)
        {
            int count = 0;
            using (IDbConnection cnn = new SqlConnection(connectionString))
            {
                string sql = "EXEC spClearQCFlags ";
                count = cnn.Execute(sql);
            }
        }

        public void UpdateDataQCFlags(string connectionString, List<IndexModel> idxList)
        {
            int count = 0;
            string upd = @"update pdo_qc_index set QC_STRING = @QC_String where INDEXID = @IndexId";
            using (IDbConnection cnn = new SqlConnection(connectionString))
            {
                count = cnn.Execute(upd, idxList);
            }
        }
    }
}
