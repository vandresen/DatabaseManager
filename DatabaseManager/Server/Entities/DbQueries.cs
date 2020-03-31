using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseManager.Server.Entities
{
    public class DbQueries
    {
        private readonly Dictionary<string, string> _dictionary;

        public DbQueries()
        {
            _dictionary = new Dictionary<string, string>();
            _dictionary = new Dictionary<string, string>
                {
                    { "Rules", "Select Id, DataType, RuleType, RuleParameters, RuleKey, RuleName, " +
                    "RuleFunction, DataAttribute, RuleFilter, FailRule, PredictionOrder, KeyNumber, " +
                    "Active, RuleDescription, CreatedBy, ModifiedBy, CreatedDate, ModifiedDate " +
                    " from pdo_qc_rules" },
                    { "Index", "Select INDEXID, IndexNode.ToString() AS Text_IndexNode, INDEXLEVEL," +
                    "DATANAME, DATATYPE, DATAKEY, QC_STRING, JSONDATAOBJECT " +
                    " from pdo_qc_index" },
                    { "WellBore", "Select UWI, FINAL_TD, WELL_NAME, SURFACE_LATITUDE, SURFACE_LONGITUDE," +
                    "LEASE_NAME, DEPTH_DATUM_ELEV, DEPTH_DATUM, OPERATOR, ASSIGNED_FIELD, CURRENT_STATUS," +
                    "GROUND_ELEV, REMARK, ROW_CHANGED_DATE, ROW_CHANGED_BY" +
                    " from WELL" },
                    { "MarkerPick", "Select STRAT_NAME_SET_ID, STRAT_UNIT_ID, UWI, INTERP_ID, DOMINANT_LITHOLOGY, PICK_DEPTH," +
                    "REMARK, ROW_CHANGED_DATE, ROW_CHANGED_BY " +
                    " from STRAT_WELL_SECTION" },
                    { "MarkerWell", "Select STRAT_NAME_SET_ID, STRAT_UNIT_ID, UWI, INTERP_ID, DOMINANT_LITHOLOGY, PICK_DEPTH," +
                    "REMARK, ROW_CHANGED_DATE, ROW_CHANGED_BY " +
                    " from STRAT_WELL_SECTION" },
                    { "WellTop", "Select STRAT_NAME_SET_ID, STRAT_UNIT_ID, LONG_NAME, ORDINAL_AGE_CODE," +
                    "REMARK, ROW_CHANGED_DATE, ROW_CHANGED_BY " +
                    " from STRAT_UNIT" },
                    { "Log", "Select UWI, CURVE_ID, NULL_REPRESENTATION, VALUE_COUNT, MAX_INDEX, MIN_INDEX, ROW_CHANGED_DATE, ROW_CHANGED_BY from well_log_curve"}
                };
        }

        public string this[string key]
        {
            get { return _dictionary[key]; }
        }
    }
}
