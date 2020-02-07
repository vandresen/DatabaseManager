using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DatabaseManager.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DeleteController : ControllerBase
    {
        [HttpPost]
        public string Delete()
        {
            string message = "Good ";
            //if (student == null)
            //{
            //    return NotFound();
            //}

            //_context.Students.Remove(student);
            //await _context.SaveChangesAsync();

            return message;
        }
    }
}