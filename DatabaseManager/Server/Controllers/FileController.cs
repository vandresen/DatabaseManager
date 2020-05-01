using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DatabaseManager.Server.Helpers;
using DatabaseManager.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.File;
using Microsoft.Extensions.Configuration;

namespace DatabaseManager.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileController : ControllerBase
    {
        private readonly string connectionString;
        private readonly string container = "sources";

        public FileController(IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString("AzureStorageConnection");
        }

        [HttpGet("{datatype}")]
        public async Task<ActionResult<List<string>>> Get(string datatype)
        {
            List<string> files = new List<string>();
            try
            {
                CloudStorageAccount account = CloudStorageAccount.Parse(connectionString);
                CloudFileShare share = account.CreateCloudFileClient().GetShareReference(datatype);
                IEnumerable<IListFileItem> fileList = share.GetRootDirectoryReference().ListFilesAndDirectories();
                foreach (IListFileItem listItem in fileList)
                {
                    if (listItem.GetType() == typeof(CloudFile))
                    {
                        files.Add(listItem.Uri.Segments.Last());
                    }
                }
            }
            catch (Exception)
            {
                return NotFound();
            }

            return files;
        }

        [HttpPost]
        public async Task<ActionResult<string>> SaveData(FileParameters fileParams)
        {
            if (fileParams == null) return BadRequest();
            try
            {
                ConnectParameters connector = Common.GetConnectParameters(connectionString, container, fileParams.DataConnector);
                CloudStorageAccount account = CloudStorageAccount.Parse(connectionString);
                CloudFileClient fileClient = account.CreateCloudFileClient();
                CloudFileShare share = fileClient.GetShareReference(fileParams.FileShare);
                if (share.Exists())
                {
                    CloudFileDirectory rootDir = share.GetRootDirectoryReference();
                    CloudFile file = rootDir.GetFileReference(fileParams.FileName);
                    if (file.Exists())
                    {
                        string fileText = file.DownloadTextAsync().Result;
                        LASLoader ls = new LASLoader();
                        ls.LoadLASFile(connector, fileText);
                        //DbUtilities dbConn = new DbUtilities();
                        //dbConn.OpenConnection(connector);
                        //dbConn.SQLExecute(sql);
                        //dbConn.CloseConnection();
                    }
                    else
                    {
                        return BadRequest();
                    }
                }
                else
                {
                    return BadRequest();
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());
            }

            return Ok($"OK");
        }
    }
}