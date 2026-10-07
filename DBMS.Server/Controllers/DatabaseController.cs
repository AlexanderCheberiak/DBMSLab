using Dbms.Core;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;

namespace Dbms.Server.Controllers
{
    [ApiController]
    [Route("api/databases")]
    public class DatabaseController : ControllerBase
    {
        private static Dictionary<string, Database> _databases = new Dictionary<string, Database>
        {
            { "DefaultDB", new Database {
                Name = "DefaultDB",
                Tables = new List<Table> { new Table { Name = "Employees", Columns = new List<Column> { new Column { Name = "Id", Type = ColumnType.Integer }, new Column { Name = "Name", Type = ColumnType.String } } } }
            } }
        };

        [HttpGet]
        public ActionResult<IEnumerable<string>> GetDatabases() => Ok(_databases.Keys);

        [HttpGet("{dbName}")]
        public ActionResult<Database> GetDatabase(string dbName)
        {
            if (_databases.TryGetValue(dbName, out var db)) return Ok(db);
            return NotFound();
        }

        [HttpPost("{dbName}")]
        public ActionResult CreateDatabase(string dbName)
        {
            if (_databases.ContainsKey(dbName)) return Conflict("База даних вже існує.");
            _databases[dbName] = new Database { Name = dbName, Tables = new List<Table>() };
            return Ok();
        }

        [HttpPut("{dbName}/rename")]
        public ActionResult RenameDatabase(string dbName, [FromQuery] string newName)
        {
            if (!_databases.TryGetValue(dbName, out var db)) return NotFound();
            if (_databases.ContainsKey(newName)) return Conflict("Назва вже використовується.");

            db.Name = newName;
            _databases.Remove(dbName);
            _databases[newName] = db;

            if (System.IO.File.Exists($"{dbName}.json"))
                System.IO.File.Move($"{dbName}.json", $"{newName}.json");

            return Ok();
        }

        [HttpPost("{dbName}/save")]
        public ActionResult SaveToDisk(string dbName)
        {
            if (!_databases.TryGetValue(dbName, out var db)) return NotFound();
            System.IO.File.WriteAllText($"{dbName}.json", JsonSerializer.Serialize(db));
            return Ok();
        }

        [HttpPost("{dbName}/load")]
        public ActionResult LoadFromDisk(string dbName)
        {
            if (System.IO.File.Exists($"{dbName}.json"))
            {
                var json = System.IO.File.ReadAllText($"{dbName}.json");
                _databases[dbName] = JsonSerializer.Deserialize<Database>(json);
                return Ok();
            }
            return NotFound();
        }

        [HttpPost("{dbName}/tables")]
        public ActionResult CreateTable(string dbName, [FromBody] Table table)
        {
            if (!_databases.TryGetValue(dbName, out var db)) return NotFound();
            db.Tables.Add(table);
            return Ok();
        }

        [HttpPut("{dbName}/tables/{tableName}/rename")]
        public ActionResult RenameTable(string dbName, string tableName, [FromQuery] string newName)
        {
            if (!_databases.TryGetValue(dbName, out var db)) return NotFound();
            var table = db.Tables.FirstOrDefault(t => t.Name == tableName);
            if (table == null) return NotFound();
            table.Name = newName;
            return Ok();
        }

        [HttpDelete("{dbName}/tables/{tableName}")]
        public ActionResult DeleteTable(string dbName, string tableName)
        {
            if (!_databases.TryGetValue(dbName, out var db)) return NotFound();
            var table = db.Tables.FirstOrDefault(t => t.Name == tableName);
            if (table == null) return NotFound();
            db.Tables.Remove(table);
            return Ok();
        }

        [HttpPost("{dbName}/tables/{tableName}/rows")]
        public ActionResult AddRow(string dbName, string tableName, [FromBody] Dictionary<string, string> rowValues)
        {
            if (!_databases.TryGetValue(dbName, out var db)) return NotFound();
            var table = db.Tables.FirstOrDefault(t => t.Name == tableName);
            if (table == null || !table.AddRow(rowValues)) return BadRequest();
            return Ok();
        }

        [HttpPut("{dbName}/tables/{tableName}/rows/{rowId}")]
        public ActionResult UpdateRow(string dbName, string tableName, int rowId, [FromBody] Dictionary<string, string> rowValues)
        {
            if (!_databases.TryGetValue(dbName, out var db)) return NotFound();
            var table = db.Tables.FirstOrDefault(t => t.Name == tableName);
            if (table == null || !table.UpdateRow(rowId, rowValues)) return BadRequest();
            return Ok();
        }

        [HttpDelete("{dbName}/tables/{tableName}/rows/{rowId}")]
        public ActionResult DeleteRow(string dbName, string tableName, int rowId)
        {
            if (!_databases.TryGetValue(dbName, out var db)) return NotFound();
            var table = db.Tables.FirstOrDefault(t => t.Name == tableName);
            if (table == null || !table.DeleteRow(rowId)) return NotFound();
            return Ok();
        }

        [HttpPost("{dbName}/tables/{tableName}/project")]
        public ActionResult ProjectTable(string dbName, string tableName, [FromQuery] string newName, [FromBody] List<string> columnNames)
        {
            if (!_databases.TryGetValue(dbName, out var db)) return NotFound();
            var table = db.Tables.FirstOrDefault(t => t.Name == tableName);
            if (table == null) return NotFound();
            db.Tables.Add(table.Project(newName, columnNames));
            return Ok();
        }

        [HttpPost("import")]
        public ActionResult ImportDatabase([FromBody] Database db)
        {
            if (db == null || string.IsNullOrWhiteSpace(db.Name)) return BadRequest("Некоректний формат бази");

            // Додає нову базу або перезаписує існуючу з такою ж назвою
            _databases[db.Name] = db;
            return Ok();
        }
    }
}