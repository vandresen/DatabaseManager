using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DatabaseManager.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DatabaseManager.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QCMethodsController : ControllerBase
    {
        public QCMethodsController()
        {

        }

        [HttpPost("Completeness")]
        public async Task<ActionResult<string>> CompletenessRule(QcRuleSetup setup)
        {
            if (setup == null) return BadRequest();
            //ConnectParameters connector = Common.GetConnectParameters(connectionString, container, source);
            //if (connector == null) return BadRequest();
            //DbUtilities dbConn = new DbUtilities();
            //try
            //{
            //    dbConn.OpenConnection(connector);
            //    RuleUtilities.SaveRule(dbConn, rule, _ruleAccessDef);
            //}
            //catch (Exception ex)
            //{
            //    return BadRequest();
            //}
            //dbConn.CloseConnection();
            string returnStatus = "Passed";
            return Ok(returnStatus);
        }
    }
}
