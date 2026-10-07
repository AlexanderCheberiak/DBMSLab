using Dbms.Core;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;

namespace Dbms.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DatabaseController : ControllerBase
    {
        // Додано таблицю за замовчуванням для тестування
        private static Database _database = new Database
        {
            Name = "DefaultDB",
            Tables = new List<Table>
            {
                new Table
                {
                    Name = "Employees",
                    Columns = new List<Column>
                    {
                        new Column { Name = "Id", Type = ColumnType.Integer },
                        new Column { Name = "Name", Type = ColumnType.String }
                    }
                }
            }
        };
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

        [HttpPost("tables/{tableName}/project")]
        public ActionResult ProjectTable(string tableName, [FromQuery] string newName, [FromBody] List<string> columnNames)
        {
            var table = _database.Tables.FirstOrDefault(t => t.Name == tableName);
            if (table == null) return NotFound("Table not found");

            var newTable = table.Project(newName, columnNames);
            _database.Tables.Add(newTable);

            return Ok();
        }
    }
}