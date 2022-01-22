﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using DatabaseManager.Common.Entities;
using DatabaseManager.Common.Helpers;
using DatabaseManager.Common.Services;
using DatabaseManager.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace DatabaseManager.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SourceController : ControllerBase
    {
        private string connectionString;
        private readonly string container = "sources";
        private readonly ITableStorageServiceCommon tableStorageService;
        private readonly IFileStorageServiceCommon fileStorageService;
        private readonly IMapper mapper;

        public SourceController(IConfiguration configuration,
            ITableStorageServiceCommon tableStorageService,
            IMapper mapper,
            IFileStorageServiceCommon fileStorageService)
        {
            connectionString = configuration.GetConnectionString("AzureStorageConnection");
            this.tableStorageService = tableStorageService;
            this.fileStorageService = fileStorageService;
            this.mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<List<ConnectParameters>>> Get()
        {
            List<ConnectParameters> connectors = new List<ConnectParameters>();
            try
            {
                string storageAccount = Request.Headers["AzureStorageConnection"];
                Sources ss = new Sources(storageAccount);
                connectors = await ss.GetSources();
            }
            catch (Exception ex)
            {
                return NotFound(ex.ToString());
            }
            return connectors;
        }

        [HttpGet("{name}")]
        public async Task<ActionResult<ConnectParameters>> Get(string name)
        {
            try
            {
                string storageAccount = Request.Headers["AzureStorageConnection"];
                Sources ss = new Sources(storageAccount);
                ConnectParameters connector = await ss.GetSourceParameters(name);
                return connector;
            }
            catch (Exception ex)
            {
                return NotFound(ex.ToString());
            }
        }

        [HttpPut]
        public async Task<ActionResult<string>> UpdateSource(ConnectParameters connectParameters)
        {
            try
            {
                string storageAccount = Request.Headers["AzureStorageConnection"];
                Sources ss = new Sources(storageAccount);
                await ss.UpdateSource(connectParameters);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());
            }

            return Ok($"OK");
        }

        [HttpPost]
        public async Task<ActionResult<string>> SaveSource(ConnectParameters connectParameters)
        {
            try
            {
                string storageAccount = Request.Headers["AzureStorageConnection"];
                Sources ss = new Sources(storageAccount);
                await ss.SaveSource(connectParameters);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());
            }
            
            return Ok($"OK");
        }

        [HttpDelete("{name}")]
        public async Task<ActionResult> Delete(string name)
        {
            try
            {
                string storageAccount = Request.Headers["AzureStorageConnection"];
                Sources ss = new Sources(storageAccount);
                await ss.DeleteSource(name);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());
            }
            
            return NoContent();
        }
    }
}