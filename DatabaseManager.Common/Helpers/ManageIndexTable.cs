using DatabaseManager.Common.Entities;
using DatabaseManager.Shared;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace DatabaseManager.Common.Helpers
{
    public class ManageIndexTable
    {
        private List<DataAccessDef> _accessDefs;
        private SqlConnection sqlCn = null;
        private SqlDataAdapter indexAdapter;
        private QcFlags qcFlags;
        private DataTable indexTable;
        private string _connectionString;
        private List<IndexModel> idxList;
        private IndexAccess idxAccess;

        public ManageIndexTable()
        {
            qcFlags = new QcFlags();
        }

        public ManageIndexTable(string connectionString)
        {
            _connectionString = connectionString;
            idxAccess = new IndexAccess();
            idxList = idxAccess.SelectIndexesByQuery("", connectionString);
            qcFlags = new QcFlags();
            foreach (var idx in idxList)
            {
                qcFlags[idx.IndexId] = idx.QC_String;
            }
        }

        public ManageIndexTable(List<DataAccessDef> accessDefs, string connectionString,
            string dataType = "", string qcRule = "")
        {
            _accessDefs = accessDefs;
            IndexAccess idxAccess = new IndexAccess();
            //DataAccessDef ruleAccessDef = _accessDefs.First(x => x.DataType == "Index");
            string select = idxAccess.GetSelectSQL();
            if (!string.IsNullOrEmpty(dataType))
            {
                select = select + $" where DATATYPE = '{dataType}'";
                if (!string.IsNullOrEmpty(qcRule))
                {
                    select = select + $" and QC_STRING like '%{qcRule}%'";
                }
            }

            sqlCn = new SqlConnection(connectionString);
            indexAdapter = new SqlDataAdapter();
            indexAdapter.SelectCommand = new SqlCommand(select, sqlCn);
            indexTable = new DataTable();
            indexAdapter.Fill(indexTable);
            qcFlags = new QcFlags();
        }

        public void ClearQCFlags(bool clearQcFlags)
        {
            InitQCFlags(clearQcFlags);
            SaveQCFlags();
        }

        public void InitQCFlags(bool clearQcFlags)
        {

            foreach (DataRow row in indexTable.Rows)
            {
                int idx = Convert.ToInt32(row["INDEXID"]);
                if (clearQcFlags)
                {
                    qcFlags[idx] = "";
                }
                else
                {
                    qcFlags[idx] = row["QC_STRING"].ToString();
                }
            }
        }

        public void SaveQCFlagDapper()
        {
            foreach (var idx in idxList)
            {
                idx.QC_String = qcFlags[idx.IndexId];
            }
            idxAccess.UpdateDataQCFlags(_connectionString, idxList);
        }

        public void SaveQCFlags()
        {
            foreach (DataRow row in indexTable.Rows)
            {
                int indexID = Convert.ToInt32(row["INDEXID"]);
                row["QC_STRING"] = qcFlags[indexID];
            }

            string upd = @"update pdo_qc_index set QC_STRING = @qc_string, JSONDATAOBJECT = @jsondataobject where INDEXID = @id";
            SqlCommand cmd = new SqlCommand(upd, sqlCn);
            cmd.Parameters.Add("@qc_string", SqlDbType.NVarChar, 400, "QC_STRING");
            cmd.Parameters.Add("@jsondataobject", SqlDbType.NVarChar, -1, "JSONDATAOBJECT");
            SqlParameter parm = cmd.Parameters.Add("@id", SqlDbType.Int, 4, "INDEXID");
            parm.SourceVersion = DataRowVersion.Original;

            indexAdapter.UpdateCommand = cmd;
            indexAdapter.Update(indexTable);
        }

        public DataTable GetIndexTable()
        {
            return indexTable;
        }

        public void SetQCFlag(int indexId, string qcStr)
        {
            qcFlags[indexId] = qcStr;
        }

        public string GetQCFlag(int indexId)
        {
            return qcFlags[indexId];
        }
    }
}
