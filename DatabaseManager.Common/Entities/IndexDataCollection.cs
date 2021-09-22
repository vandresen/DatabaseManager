using Microsoft.Data.SqlClient.Server;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace DatabaseManager.Common.Entities
{
    public class IndexDataCollection : List<IndexData>, IEnumerable<SqlDataRecord>
    {
        IEnumerator<SqlDataRecord> IEnumerable<SqlDataRecord>.GetEnumerator()
        {
            SqlDataRecord ret = new SqlDataRecord(
                new SqlMetaData("DataName", SqlDbType.NVarChar, 40),
                new SqlMetaData("IndexNode", SqlDbType.NVarChar, 255),
                new SqlMetaData("QcLocation", SqlDbType.NVarChar, 255),
                new SqlMetaData("DataType", SqlDbType.NVarChar, 40),
                new SqlMetaData("DataKey", SqlDbType.NVarChar, 400),
                new SqlMetaData("Latitude", SqlDbType.Float),
                new SqlMetaData("Longitude", SqlDbType.Float),
                new SqlMetaData("JsonDataObject", SqlDbType.NVarChar, SqlMetaData.Max)
                );

            foreach (IndexData data in this)
            {
                ret.SetString(0, data.DataName);
                ret.SetString(1, data.IndexNode);
                if (data.QcLocation == null)
                {
                    ret.SetDBNull(2);
                }
                else
                {
                    ret.SetString(2, data.QcLocation);
                }
                ret.SetString(3, data.DataType);
                if (data.DataKey == null)
                {
                    ret.SetDBNull(4);
                }
                else
                {
                    ret.SetString(4, data.DataKey);
                }
                if (data.Latitude == null)
                {
                    ret.SetDBNull(5);
                }
                else
                {
                    ret.SetDouble(5, (double)data.Latitude);
                }
                if (data.Longitude == null)
                {
                    ret.SetDBNull(6);
                }
                else
                {
                    ret.SetDouble(6, (double)data.Longitude);
                }
                if (data.JsonDataObject == null)
                {
                    ret.SetDBNull(7);
                }
                else
                {
                    ret.SetString(7, data.JsonDataObject);
                }

                yield return ret;
            }
        }
    }
}
