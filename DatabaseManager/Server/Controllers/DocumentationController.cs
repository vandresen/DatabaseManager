using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DatabaseManager.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DocumentationController : ControllerBase
    {
        private readonly IWebHostEnvironment env;

        public DocumentationController(IWebHostEnvironment env)
        {
            this.env = env;
        }

        [HttpGet("{docname}")]
        public async Task<ActionResult<string>> Get(string docname)
        {
            string documentation = "";
            try
            {
                string contentRootPath = env.ContentRootPath;
                string docFile = contentRootPath + @"\Documentation\" + docname + ".md";
                if (!System.IO.File.Exists(docFile)) return NotFound();
                documentation = System.IO.File.ReadAllText(docFile);
            }
            catch (Exception)
            {
                return NotFound();
            }

            return documentation;
        }
    }
}
