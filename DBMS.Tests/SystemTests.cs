using Dbms.Core;
using Xunit;
using System.Collections.Generic;

namespace Dbms.Tests
{
    public class SystemTests
    {
        [Fact]
        public void Test_CharInvl_Validation()
        {
            var col = new Column { Name = "Letter", Type = ColumnType.CharInvl, IntervalStart = "a", IntervalEnd = "m" };
            Assert.True(col.Validate("c"));
            Assert.True(col.Validate("a"));
            Assert.False(col.Validate("z"));
            Assert.False(col.Validate("1"));
        }

        [Fact]
        public void Test_StringCharInvl_Validation()
        {
            var col = new Column { Name = "BinaryWord", Type = ColumnType.StringCharInvl, IntervalStart = "0", IntervalEnd = "1" };
            Assert.True(col.Validate("101010"));
            Assert.False(col.Validate("102010"));
            Assert.False(col.Validate("abc"));
        }

        [Fact]
        public void Test_Table_Projection()
        {
            var table = new Table { Name = "Employees" };
            table.Columns.Add(new Column { Name = "Id", Type = ColumnType.Integer });
            table.Columns.Add(new Column { Name = "Name", Type = ColumnType.String });
            table.Columns.Add(new Column { Name = "Department", Type = ColumnType.String });

            table.AddRow(new Dictionary<string, string> { { "Id", "1" }, { "Name", "Alice" }, { "Department", "IT" } });
            table.AddRow(new Dictionary<string, string> { { "Id", "2" }, { "Name", "Bob" }, { "Department", "HR" } });

            var projected = table.Project("Employees_Names", new List<string> { "Name" });

            Assert.Single(projected.Columns);
            Assert.Equal("Name", projected.Columns[0].Name);
            Assert.Equal(2, projected.Rows.Count);
            Assert.True(projected.Rows[0].Values.ContainsKey("Name"));
            Assert.False(projected.Rows[0].Values.ContainsKey("Department"));
        }
    }
}
