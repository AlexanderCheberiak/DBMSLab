using System;
using System.Collections.Generic;
using System.Linq;

namespace Dbms.Core
{
    public enum ColumnType { Integer, Real, Char, String, CharInvl, StringCharInvl }

    public class Column
    {
        public string Name { get; set; }
        public ColumnType Type { get; set; }
        public string IntervalStart { get; set; } 
        public string IntervalEnd { get; set; }

        public bool Validate(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            switch (Type)
            {
                case ColumnType.Integer: return int.TryParse(value, out _);
                case ColumnType.Real: return double.TryParse(value, out _);
                case ColumnType.Char: return char.TryParse(value, out _);
                case ColumnType.String: return true;
                case ColumnType.CharInvl:
                    if (char.TryParse(value, out char c) && char.TryParse(IntervalStart, out char s) && char.TryParse(IntervalEnd, out char e))
                        return c >= s && c <= e;
                    return false;
                case ColumnType.StringCharInvl:
                    if (char.TryParse(IntervalStart, out char ss) && char.TryParse(IntervalEnd, out char ee))
                        return value.All(ch => ch >= ss && ch <= ee);
                    return false;
                default: return false;
            }
        }
    }

    public class Row
    {
        public int Id { get; set; }
        public Dictionary<string, string> Values { get; set; } = new Dictionary<string, string>();
    }

    public class Table
    {
        public string Name { get; set; }
        public List<Column> Columns { get; set; } = new List<Column>();
        public List<Row> Rows { get; set; } = new List<Row>();
        private int _nextRowId = 1;

        public bool AddRow(Dictionary<string, string> values)
        {
            foreach (var col in Columns)
            {
                if (values.TryGetValue(col.Name, out string val))
                {
                    if (!col.Validate(val)) return false;
                }
                else return false;
            }
            Rows.Add(new Row { Id = _nextRowId++, Values = values });
            return true;
        }

        public Table Project(string newName, List<string> columnNames)
        {
            var newTable = new Table { Name = newName };
            foreach (var colName in columnNames)
            {
                var col = Columns.FirstOrDefault(c => c.Name == colName);
                if (col != null)
                {
                    newTable.Columns.Add(new Column 
                    { 
                        Name = col.Name, 
                        Type = col.Type, 
                        IntervalStart = col.IntervalStart, 
                        IntervalEnd = col.IntervalEnd 
                    });
                }
            }

            foreach (var row in Rows)
            {
                var newValues = new Dictionary<string, string>();
                foreach (var colName in columnNames)
                {
                    if (row.Values.ContainsKey(colName))
                        newValues[colName] = row.Values[colName];
                }
                newTable.Rows.Add(new Row { Id = newTable._nextRowId++, Values = newValues });
            }
            return newTable;
        }
    }

    public class Database
    {
        public string Name { get; set; }
        public List<Table> Tables { get; set; } = new List<Table>();
    }
}
