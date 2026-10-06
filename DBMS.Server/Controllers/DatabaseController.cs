using Dbms.Core;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Text.Json;

namespace Dbms.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DatabaseController : ControllerBase
    {
        private static Database _database = new Database { Name = "DefaultDB" };
        private readonly string _filePath = "database.json";

        [HttpGet]
        public ActionResult<Database> GetDatabase() => Ok(_database);

        [HttpPost("tables")]
        public ActionResult CreateTable([FromBody] Table table)
        {
            _database.Tables.Add(table);
            return Ok();
        }

        [HttpPost("tables/{tableName}/rows")]
        public ActionResult AddRow(string tableName, [FromBody] Dictionary<string, string> rowValues)
        {
            var table = _database.Tables.FirstOrDefault(t => t.Name == tableName);
            if (table == null) return NotFound("Table not found");

            bool isValid = table.AddRow(rowValues);
            if (!isValid) return BadRequest("Data validation failed for one or more columns.");

            return Ok();
        }

        [HttpPost("save")]
        public ActionResult SaveToDisk()
        {
            var json = JsonSerializer.Serialize(_database);
            System.IO.File.WriteAllText(_filePath, json);
            return Ok();
        }

        [HttpPost("load")]
        public ActionResult LoadFromDisk()
        {
            if (System.IO.File.Exists(_filePath))
            {
                var json = System.IO.File.ReadAllText(_filePath);
                _database = JsonSerializer.Deserialize<Database>(json);
                return Ok(_database);
            }
            return NotFound("File not found");
        }
    }
}
