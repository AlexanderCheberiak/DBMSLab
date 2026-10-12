using System.Collections.Generic;
using System.Linq;
using Xunit;
using Dbms.Core;

namespace Dbms.Tests
{
    public class TableTests
    {
        [Fact]
        public void InsertAndValidateData()
        {
            var table = new Table
            {
                Name = "Users",
                Columns = new List<Column>
                {
                    new Column { Name = "Id", Type = ColumnType.Integer },
                    new Column { Name = "Name", Type = ColumnType.String }
                }
            };

            var validRow = new Dictionary<string, string> { { "Id", "1" }, { "Name", "Alice" } };
            var invalidRow = new Dictionary<string, string> { { "Id", "TextInsteadOfNumber" }, { "Name", "Bob" } };

            bool isAddedValid = table.AddRow(validRow);
            bool isAddedInvalid = table.AddRow(invalidRow);

            Assert.True(isAddedValid);
            Assert.False(isAddedInvalid); 
            Assert.Single(table.Rows);    
        }

        [Fact]
        public void CharInvl_Validation()
        {
            // Налаштування колонки типу charInvl з інтервалом 'a' - 'f'
            var col = new Column
            {
                Name = "Grade",
                Type = ColumnType.CharInvl,
                IntervalStart = "a",
                IntervalEnd = "f"
            };


            Assert.True(col.Validate("c"));    //  символ всередині діапазону
            Assert.True(col.Validate("a"));    //  межа діапазону
            Assert.False(col.Validate("z"));   //  символ поза діапазоном
            Assert.False(col.Validate("abc")); //  передано рядок замість одного символу
            Assert.False(col.Validate("1"));   //  цифра замість букви
        }

        [Fact]
        public void ProjectionTest()
        {
            //Створення таблиці з трьома колонками
            var table = new Table
            {
                Name = "Employees",
                Columns = new List<Column>
                {
                    new Column { Name = "Id", Type = ColumnType.Integer },
                    new Column { Name = "Name", Type = ColumnType.String },
                    new Column { Name = "Salary", Type = ColumnType.Real }
                }
            };
            table.AddRow(new Dictionary<string, string> { { "Id", "1" }, { "Name", "John" }, { "Salary", "1000.50" } });
            table.AddRow(new Dictionary<string, string> { { "Id", "2" }, { "Name", "Jane" }, { "Salary", "2000.00" } });

            var columnsToProject = new List<string> { "Id", "Name" };

            //  Виконання операції проєкції
            var projectedTable = table.Project("Employees_PublicInfo", columnsToProject);

            //  Перевірка структури та даних нової таблиці
            Assert.Equal("Employees_PublicInfo", projectedTable.Name);
            Assert.Equal(2, projectedTable.Columns.Count);

            // Колонка Salary має бути відсутня у новій схемі
            Assert.DoesNotContain(projectedTable.Columns, c => c.Name == "Salary");
            Assert.Contains(projectedTable.Columns, c => c.Name == "Id");

            // Перевірка перенесення даних
            Assert.Equal(2, projectedTable.Rows.Count);
            Assert.False(projectedTable.Rows[0].Values.ContainsKey("Salary")); // Даних про зарплату не має бути
            Assert.Equal("John", projectedTable.Rows[0].Values["Name"]);
            Assert.Equal("2", projectedTable.Rows[1].Values["Id"]);
        }
    }
}