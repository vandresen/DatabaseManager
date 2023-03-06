using Microsoft.Data.SqlClient.Server;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.Rules.Models
{
    public class RuleCollection : List<RuleModel>, IEnumerable<SqlDataRecord>
    {
        IEnumerator<SqlDataRecord> IEnumerable<SqlDataRecord>.GetEnumerator()
        {
            var sqlRow = new SqlDataRecord(
                new SqlMetaData("Id", SqlDbType.Int),
                new SqlMetaData("DataType", SqlDbType.NVarChar, 40),
                new SqlMetaData("RuleType", SqlDbType.NVarChar, 40),
                new SqlMetaData("RuleParameters", SqlDbType.NVarChar, 500),
                new SqlMetaData("RuleKey", SqlDbType.NVarChar, 16),
                new SqlMetaData("RuleName", SqlDbType.NVarChar, 40),
                new SqlMetaData("RuleFunction", SqlDbType.NVarChar, 255),
                new SqlMetaData("DataAttribute", SqlDbType.NVarChar, 255),
                new SqlMetaData("RuleFilter", SqlDbType.NVarChar, 255),
                new SqlMetaData("FailRule", SqlDbType.NVarChar, 255),
                new SqlMetaData("PredictionOrder", SqlDbType.Int),
                new SqlMetaData("KeyNumber", SqlDbType.Int),
                new SqlMetaData("Active", SqlDbType.NVarChar, 1),
                new SqlMetaData("RuleDescription", SqlDbType.NVarChar, 255),
                new SqlMetaData("CreatedBy", SqlDbType.NVarChar, 255),
                new SqlMetaData("ModifiedBy", SqlDbType.NVarChar, 255),
                new SqlMetaData("CreatedDate", SqlDbType.DateTime),
                new SqlMetaData("ModifiedDate", SqlDbType.DateTime)
                );

            foreach (RuleModel rule in this)
            {
                sqlRow.SetInt32(0, rule.Id);
                sqlRow.SetString(1, rule.DataType);
                sqlRow.SetString(2, rule.RuleType);
                if (rule.RuleParameters == null) sqlRow.SetDBNull(3);
                else sqlRow.SetString(3, rule.RuleParameters);
                if (rule.RuleKey == null) sqlRow.SetDBNull(4);
                else sqlRow.SetString(4, rule.RuleKey);
                sqlRow.SetString(5, rule.RuleName);
                if (rule.RuleFunction == null) sqlRow.SetDBNull(6);
                else sqlRow.SetString(6, rule.RuleFunction);
                if (rule.DataAttribute == null) sqlRow.SetDBNull(7);
                else sqlRow.SetString(7, rule.DataAttribute);
                if (rule.RuleFilter == null) sqlRow.SetDBNull(8);
                else sqlRow.SetString(8, rule.RuleFilter);
                if (rule.FailRule == null) sqlRow.SetDBNull(9);
                else sqlRow.SetString(9, rule.FailRule);
                sqlRow.SetInt32(10, rule.PredictionOrder);
                sqlRow.SetInt32(11, rule.KeyNumber);
                if (rule.Active == null) sqlRow.SetDBNull(12);
                else sqlRow.SetString(12, rule.Active);
                if (rule.RuleDescription == null) sqlRow.SetDBNull(13);
                else sqlRow.SetString(13, rule.RuleDescription);
                if (rule.CreatedBy == null) sqlRow.SetDBNull(14);
                else sqlRow.SetString(14, rule.CreatedBy);
                if (rule.ModifiedBy == null) sqlRow.SetDBNull(15);
                else sqlRow.SetString(15, rule.ModifiedBy);
                if (rule.CreatedDate == null) sqlRow.SetDBNull(16);
                else sqlRow.SetDateTime(16, (DateTime)rule.CreatedDate);
                if (rule.ModifiedDate == null) sqlRow.SetDBNull(17);
                else sqlRow.SetDateTime(17, (DateTime)rule.ModifiedDate);

                yield return sqlRow;
            }
        }
    }
}
