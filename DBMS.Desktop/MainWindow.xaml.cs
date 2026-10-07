using Dbms.Core;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;

namespace Dbms.Desktop
{
    public partial class MainWindow : Window
    {
        private readonly HttpClient _httpClient;
        private Database _currentDb;
        private List<Column> _pendingColumns = new List<Column>();

        public MainWindow()
        {
            InitializeComponent();
            _httpClient = new HttpClient { BaseAddress = new System.Uri("http://localhost:5088/") };
            ComboColType.ItemsSource = System.Enum.GetValues(typeof(ColumnType));
            ComboColType.SelectedIndex = 0;
            Refresh_Click(null, null);
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            TxtStatus.Text = "Отримання даних...";
            try
            {
                // 1. Запам'ятовуємо назву поточної обраної таблиці
                string selectedTableName = (ListTables.SelectedItem as Table)?.Name;

                var response = await _httpClient.GetAsync("api/database");
                if (response.IsSuccessStatusCode)
                {
                    _currentDb = await response.Content.ReadFromJsonAsync<Database>();
                    ListTables.ItemsSource = _currentDb?.Tables;
                    ListTables.DisplayMemberPath = "Name";

                    if (_currentDb?.Tables != null && _currentDb.Tables.Count > 0)
                    {
                        // 2. Шукаємо ту саму таблицю серед оновлених даних
                        var matchedTable = _currentDb.Tables.FirstOrDefault(t => t.Name == selectedTableName);

                        if (matchedTable != null)
                        {
                            ListTables.SelectedItem = matchedTable;
                        }
                        else
                        {
                            ListTables.SelectedIndex = 0;
                        }
                    }

                    TxtStatus.Text = "Підключено до бази: " + _currentDb?.Name;
                }
            }
            catch (System.Exception)
            {
                TxtStatus.Text = "Помилка з'єднання з сервером";
            }
        }

        private async void LoadDb_Click(object sender, RoutedEventArgs e)
        {
            await _httpClient.PostAsync("api/database/load", null);
            Refresh_Click(null, null);
        }

        private async void SaveDb_Click(object sender, RoutedEventArgs e)
        {
            var response = await _httpClient.PostAsync("api/database/save", null);
            TxtStatus.Text = response.IsSuccessStatusCode ? "Базу успішно збережено на диск" : "Помилка збереження";
        }

        private void ListTables_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListTables.SelectedItem is Table selectedTable)
            {
                DataGridMain.Columns.Clear();
                PanelRowInputs.Children.Clear();
                ListProjectColumns.Items.Clear();

                foreach (var col in selectedTable.Columns)
                {
                    DataGridMain.Columns.Add(new DataGridTextColumn
                    {
                        Header = col.Name,
                        Binding = new System.Windows.Data.Binding($"Values[{col.Name}]")
                    });

                    PanelRowInputs.Children.Add(new TextBlock
                    {
                        Text = $"{col.Name} ({col.Type})",
                        Margin = new Thickness(0, 10, 0, 2),
                        Foreground = new SolidColorBrush(Colors.Gray)
                    });

                    var inputField = new TextBox { Tag = col.Name, Height = 25 };
                    if (!string.IsNullOrEmpty(col.IntervalStart))
                        inputField.ToolTip = $"Діапазон: [{col.IntervalStart} - {col.IntervalEnd}]";
                    PanelRowInputs.Children.Add(inputField);

                    ListProjectColumns.Items.Add(col.Name);
                }
                DataGridMain.ItemsSource = selectedTable.Rows;
            }
        }

        private async void AddRow_Click(object sender, RoutedEventArgs e)
        {
            if (ListTables.SelectedItem is Table selectedTable)
            {
                var values = new Dictionary<string, string>();
                foreach (var child in PanelRowInputs.Children)
                {
                    if (child is TextBox tb && tb.Tag is string colName)
                        values[colName] = tb.Text;
                }

                var response = await _httpClient.PostAsJsonAsync($"api/database/tables/{selectedTable.Name}/rows", values);
                if (response.IsSuccessStatusCode)
                {
                    // Очищення текстових полів після додавання
                    foreach (var child in PanelRowInputs.Children)
                    {
                        if (child is TextBox tb) tb.Clear();
                    }

                    TxtStatus.Text = "Запис успішно додано";
                    Refresh_Click(null, null);
                }
                else
                {
                    MessageBox.Show("Помилка валідації. Перевірте типи введених даних.");
                }
            }
        }

        private async void Project_Click(object sender, RoutedEventArgs e)
        {
            if (ListTables.SelectedItem is Table selectedTable && ListProjectColumns.SelectedItems.Count > 0)
            {
                var selectedCols = ListProjectColumns.SelectedItems.Cast<string>().ToList();
                var newName = TxtProjectName.Text;

                var response = await _httpClient.PostAsJsonAsync($"api/database/tables/{selectedTable.Name}/project?newName={newName}", selectedCols);
                if (response.IsSuccessStatusCode)
                {
                    TxtStatus.Text = $"Проєкцію {newName} створено";
                    Refresh_Click(null, null);
                }
            }
        }

        private void AddColumn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtColName.Text)) return;
            var newCol = new Column
            {
                Name = TxtColName.Text,
                Type = (ColumnType)ComboColType.SelectedItem,
                IntervalStart = TxtInvStart.Text,
                IntervalEnd = TxtInvEnd.Text
            };
            _pendingColumns.Add(newCol);

            var colDescriptions = _pendingColumns.Select(c =>
                c.Type == ColumnType.CharInvl || c.Type == ColumnType.StringCharInvl
                ? $"{c.Name} ({c.Type} [{c.IntervalStart}-{c.IntervalEnd}])"
                : $"{c.Name} ({c.Type})");

            TxtPendingCols.Text = string.Join("\n", colDescriptions);
            TxtColName.Clear();
            TxtInvStart.Clear();
            TxtInvEnd.Clear();
        }

        private async void CreateTable_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtNewTableName.Text) || _pendingColumns.Count == 0) return;

            var newTable = new Table { Name = TxtNewTableName.Text, Columns = new List<Column>(_pendingColumns) };
            var response = await _httpClient.PostAsJsonAsync("api/database/tables", newTable);

            if (response.IsSuccessStatusCode)
            {
                TxtStatus.Text = $"Таблицю {TxtNewTableName.Text} створено";
                _pendingColumns.Clear();
                TxtPendingCols.Text = "Колонки відсутні";
                TxtNewTableName.Clear();
                Refresh_Click(null, null);
            }
        }

        private void DataGridMain_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataGridMain.SelectedItem is Row selectedRow)
            {
                foreach (var child in PanelRowInputs.Children)
                {
                    if (child is TextBox tb && tb.Tag is string colName)
                    {
                        tb.Text = selectedRow.Values.TryGetValue(colName, out string val) ? val : string.Empty;
                    }
                }
            }
        }

        private async void RenameTable_Click(object sender, RoutedEventArgs e)
        {
            if (ListTables.SelectedItem is Table selectedTable && !string.IsNullOrWhiteSpace(TxtRenameTable.Text))
            {
                var response = await _httpClient.PutAsync($"api/database/tables/{selectedTable.Name}/rename?newName={TxtRenameTable.Text}", null);
                if (response.IsSuccessStatusCode)
                {
                    TxtStatus.Text = $"Таблицю перейменовано на {TxtRenameTable.Text}";
                    TxtRenameTable.Clear();
                    Refresh_Click(null, null);
                }
            }
        }

        private async void DeleteTable_Click(object sender, RoutedEventArgs e)
        {
            if (ListTables.SelectedItem is Table selectedTable)
            {
                var response = await _httpClient.DeleteAsync($"api/database/tables/{selectedTable.Name}");
                if (response.IsSuccessStatusCode)
                {
                    TxtStatus.Text = "Таблицю видалено";
                    Refresh_Click(null, null);
                }
            }
        }

        private async void EditRow_Click(object sender, RoutedEventArgs e)
        {
            if (ListTables.SelectedItem is Table selectedTable && DataGridMain.SelectedItem is Row selectedRow)
            {
                var values = new Dictionary<string, string>();
                foreach (var child in PanelRowInputs.Children)
                {
                    if (child is TextBox tb && tb.Tag is string colName) values[colName] = tb.Text;
                }

                var response = await _httpClient.PutAsJsonAsync($"api/database/tables/{selectedTable.Name}/rows/{selectedRow.Id}", values);
                if (response.IsSuccessStatusCode)
                {
                    TxtStatus.Text = "Рядок успішно оновлено";
                    Refresh_Click(null, null);
                }
                else MessageBox.Show("Помилка валідації. Перевірте правильність типів даних.");
            }
        }

        private async void DeleteRow_Click(object sender, RoutedEventArgs e)
        {
            if (ListTables.SelectedItem is Table selectedTable && DataGridMain.SelectedItem is Row selectedRow)
            {
                var response = await _httpClient.DeleteAsync($"api/database/tables/{selectedTable.Name}/rows/{selectedRow.Id}");
                if (response.IsSuccessStatusCode)
                {
                    TxtStatus.Text = "Рядок видалено";
                    foreach (var child in PanelRowInputs.Children)
                    {
                        if (child is TextBox tb) tb.Clear();
                    }
                    Refresh_Click(null, null);
                }
            }
        }

        private void TxtProjectName_GotFocus(object sender, RoutedEventArgs e) => TxtProjectName.Clear();
    }
}