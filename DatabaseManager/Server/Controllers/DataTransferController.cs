using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DatabaseManager.Common.Helpers;
using DatabaseManager.Components.Services;
using DatabaseManager.Server.Entities;
using DatabaseManager.Server.Helpers;
using DatabaseManager.Server.Services;
using DatabaseManager.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace DatabaseManager.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DataTransferController : ControllerBase
    {
        private readonly ITableStorageService tableStorageService;
        private readonly IFileStorageService fileStorageService;
        private readonly IQueueService queueService;
        private readonly IMapper mapper;
        private readonly string container = "sources";
        private readonly string queueName = "datatransferqueue";
        private readonly string infoName = "datatransferinfo";

        public DataTransferController(ITableStorageService tableStorageService,
            IFileStorageService fileStorageService,
            IQueueService queueService,
            IMapper mapper)
        {
            this.tableStorageService = tableStorageService;
            this.fileStorageService = fileStorageService;
            this.queueService = queueService;
            this.mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<List<MessageQueueInfo>>> GetQueueMessage()
        {
            try
            {
                //List<string> messages = new List<string>();
                //string message = "";
                //bool messageBox = true;
                string tmpConnString = Request.Headers["AzureStorageConnection"];
                DataTransfer dt = new DataTransfer(tmpConnString);
                List<MessageQueueInfo> messages = dt.GetQueueMessage();
                //queueService.SetConnectionString(tmpConnString);
                //while (messageBox)
                //{
                //    message = queueService.GetMessage(infoName);
                //    if (string.IsNullOrEmpty(message))
                //    {
                //        messageBox = false;
                //    }
                //    else
                //    {
                //        messages.Add(message);
                //    }
                //}
                return messages;
            }
            catch (Exception ex)
            {
                return BadRequest("Problems getting info from info queue");
            }
        }

        [HttpGet("{source}")]
        public async Task<ActionResult<List<TransferParameters>>> Get(string source)
        {
            try
            {
                string tmpConnString = Request.Headers["AzureStorageConnection"];
                DataTransfer dt = new DataTransfer(tmpConnString);
                List<string> files = await dt.GetFiles(source);
                List<TransferParameters> transParms = new List<TransferParameters>();
                foreach (var file in files)
                {
                    transParms.Add(new TransferParameters { TargetName = source, Table = file });
                }
                return transParms;
            }
            catch (Exception)
            {
                return NotFound();
            }
        }

        [HttpPost]
        public async Task<ActionResult<string>> CopyData(TransferParameters transferParameters)
        {
            try
            {
                string tmpConnString = Request.Headers["AzureStorageConnection"];
                DataTransfer dt = new DataTransfer(tmpConnString);
                await dt.CopyFiles(transferParameters);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());
            }
            string message = $"{transferParameters.Table} has been copied";
            return Ok(message);
        }

        [HttpPost("remote")]
        public async Task<ActionResult<string>> CopyRemote(TransferParameters transferParameters)
        {
            try
            {
                string tmpConnString = Request.Headers["AzureStorageConnection"];
                DataTransfer dt = new DataTransfer(tmpConnString);
                dt.CopyRemote(transferParameters);
            }
            catch (Exception)
            {
                return BadRequest("Problems with data transfer queue");
            }
            
            string response= $"{transferParameters.Table} has started on remote computer";
            return Ok(response);
        }

        [HttpDelete("{target}/{table}")]
        public async Task<ActionResult> DeleteTable(string target, string table)
        {
            string message = "";
            try
            {
                string tmpConnString = Request.Headers["AzureStorageConnection"];
                DataTransfer dt = new DataTransfer(tmpConnString);
                await dt.DeleteTable(target, table);
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return BadRequest(message);
            }

            return Ok(message);
        }
    }
}
