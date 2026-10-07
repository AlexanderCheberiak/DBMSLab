using Dbms.Core;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Dbms.Desktop
{
    public partial class MainWindow : Window
    {
        private readonly HttpClient _httpClient;
        private Database _currentDb;
        private List<Column> _pendingColumns = new List<Column>();
        private string CurrentDbName => ComboDatabases.SelectedItem as string;

        public MainWindow()
        {
            InitializeComponent();
            _httpClient = new HttpClient { BaseAddress = new System.Uri("http://localhost:5088/") };
            ComboColType.ItemsSource = System.Enum.GetValues(typeof(ColumnType));
            ComboColType.SelectedIndex = 0;
            LoadDatabasesList();
        }

        private async void LoadDatabasesList()
        {
            var response = await _httpClient.GetAsync("api/databases");
            if (response.IsSuccessStatusCode)
            {
                var dbs = await response.Content.ReadFromJsonAsync<List<string>>();
                var prevSelection = CurrentDbName;

                ComboDatabases.SelectionChanged -= ComboDatabases_SelectionChanged;
                ComboDatabases.ItemsSource = dbs;
                ComboDatabases.SelectionChanged += ComboDatabases_SelectionChanged;

                if (dbs != null && dbs.Contains(prevSelection)) ComboDatabases.SelectedItem = prevSelection;
                else if (dbs != null && dbs.Count > 0) ComboDatabases.SelectedIndex = 0;

                LoadCurrentDatabase();
            }
        }

        private async void LoadCurrentDatabase()
        {
            if (string.IsNullOrEmpty(CurrentDbName))
            {
                ListTables.ItemsSource = null;
                return;
            }

            TxtStatus.Text = $"Завантаження: {CurrentDbName}...";
            var response = await _httpClient.GetAsync($"api/databases/{CurrentDbName}");
            if (response.IsSuccessStatusCode)
            {
                _currentDb = await response.Content.ReadFromJsonAsync<Database>();
                string selectedTableName = (ListTables.SelectedItem as Table)?.Name;

                ListTables.ItemsSource = _currentDb?.Tables;
                ListTables.DisplayMemberPath = "Name";

                if (_currentDb?.Tables != null)
                {
                    var matchedTable = _currentDb.Tables.FirstOrDefault(t => t.Name == selectedTableName);
                    ListTables.SelectedItem = matchedTable ?? _currentDb.Tables.FirstOrDefault();
                }
                TxtStatus.Text = $"Підключено до бази: {CurrentDbName}";
            }
        }

        private void ComboDatabases_SelectionChanged(object sender, SelectionChangedEventArgs e) => LoadCurrentDatabase();
        private void Refresh_Click(object sender, RoutedEventArgs e) => LoadDatabasesList();

        private async void CreateDb_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtDbName.Text)) return;
            var response = await _httpClient.PostAsync($"api/databases/{TxtDbName.Text}", null);
            if (response.IsSuccessStatusCode) LoadDatabasesList();
        }

        private async void RenameDb_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(CurrentDbName) || string.IsNullOrWhiteSpace(TxtDbName.Text)) return;
            var response = await _httpClient.PutAsync($"api/databases/{CurrentDbName}/rename?newName={TxtDbName.Text}", null);
            if (response.IsSuccessStatusCode) LoadDatabasesList();
        }

        //private async void LoadDb_Click(object sender, RoutedEventArgs e)
        //{
        //    if (string.IsNullOrWhiteSpace(TxtDbName.Text)) return;
        //    var response = await _httpClient.PostAsync($"api/databases/{TxtDbName.Text}/load", null);
        //    if (response.IsSuccessStatusCode) LoadDatabasesList();
        //    else MessageBox.Show("Файл бази не знайдено на сервері.");
        //}

        //private async void SaveDb_Click(object sender, RoutedEventArgs e)
        //{
        //    if (string.IsNullOrEmpty(CurrentDbName)) return;
        //    var response = await _httpClient.PostAsync($"api/databases/{CurrentDbName}/save", null);
        //    TxtStatus.Text = response.IsSuccessStatusCode ? "Збережено на диск" : "Помилка збереження";
        //}

        private async void LoadFromFile_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "JSON файли (*.json)|*.json|Всі файли (*.*)|*.*",
                Title = "Оберіть файл бази даних"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string json = System.IO.File.ReadAllText(openFileDialog.FileName);

                    // Налаштування для ігнорування регістру символів (camelCase <-> PascalCase)
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var db = JsonSerializer.Deserialize<Database>(json, options);

                    if (db == null || string.IsNullOrWhiteSpace(db.Name))
                    {
                        MessageBox.Show("Файл порожній або містить непідтримувану структуру.");
                        return;
                    }

                    var response = await _httpClient.PostAsJsonAsync("api/databases/import", db);
                    if (response.IsSuccessStatusCode)
                    {
                        TxtStatus.Text = $"БД '{db.Name}' успішно імпортовано.";
                        LoadDatabasesList();
                    }
                    else
                    {
                        MessageBox.Show("Сервер відхилив дані. Перевірте формат файлу.");
                    }
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show("Файл пошкоджено або він має неправильний формат структури бази.\n" + ex.Message);
                }
            }
        }

        private async void SaveToFile_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(CurrentDbName))
            {
                MessageBox.Show("Спочатку оберіть базу даних для збереження.");
                return;
            }

            var saveFileDialog = new SaveFileDialog
            {
                FileName = CurrentDbName + ".json",
                Filter = "JSON файли (*.json)|*.json",
                Title = "Зберегти базу даних як"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                var response = await _httpClient.GetAsync($"api/databases/{CurrentDbName}");
                if (response.IsSuccessStatusCode)
                {
                    // Отримуємо актуальний JSON із сервера і пишемо у вибраний локальний файл
                    string json = await response.Content.ReadAsStringAsync();
                    System.IO.File.WriteAllText(saveFileDialog.FileName, json);
                    TxtStatus.Text = $"Файл збережено: {saveFileDialog.FileName}";
                }
                else
                {
                    TxtStatus.Text = "Помилка завантаження даних із сервера для збереження.";
                }
            }
        }

        private void ListTables_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DataGridMain.Columns.Clear();
            PanelRowInputs.Children.Clear();
            ListProjectColumns.Items.Clear();

            if (ListTables.SelectedItem is Table selectedTable)
            {
                foreach (var col in selectedTable.Columns)
                {
                    DataGridMain.Columns.Add(new DataGridTextColumn { Header = col.Name, Binding = new System.Windows.Data.Binding($"Values[{col.Name}]") });

                    PanelRowInputs.Children.Add(new TextBlock { Text = $"{col.Name} ({col.Type})", Margin = new Thickness(0, 10, 0, 2), Foreground = new SolidColorBrush(Colors.Gray) });
                    var inputField = new TextBox { Tag = col.Name, Height = 25 };
                    if (!string.IsNullOrEmpty(col.IntervalStart)) inputField.ToolTip = $"[{col.IntervalStart}-{col.IntervalEnd}]";
                    PanelRowInputs.Children.Add(inputField);

                    ListProjectColumns.Items.Add(col.Name);
                }
                DataGridMain.ItemsSource = selectedTable.Rows;
            }
            else
            {
                DataGridMain.ItemsSource = null;
            }
        }

        private void DataGridMain_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataGridMain.SelectedItem is Row selectedRow)
            {
                foreach (var child in PanelRowInputs.Children)
                {
                    if (child is TextBox tb && tb.Tag is string colName)
                        tb.Text = selectedRow.Values.TryGetValue(colName, out string val) ? val : string.Empty;
                }
            }
        }

        private async void AddRow_Click(object sender, RoutedEventArgs e)
        {
            if (ListTables.SelectedItem is Table selectedTable && !string.IsNullOrEmpty(CurrentDbName))
            {
                var values = PanelRowInputs.Children.OfType<TextBox>().ToDictionary(tb => (string)tb.Tag, tb => tb.Text);
                var response = await _httpClient.PostAsJsonAsync($"api/databases/{CurrentDbName}/tables/{selectedTable.Name}/rows", values);
                if (response.IsSuccessStatusCode) LoadCurrentDatabase();
            }
        }

        private async void EditRow_Click(object sender, RoutedEventArgs e)
        {
            if (ListTables.SelectedItem is Table selectedTable && DataGridMain.SelectedItem is Row selectedRow && !string.IsNullOrEmpty(CurrentDbName))
            {
                var values = PanelRowInputs.Children.OfType<TextBox>().ToDictionary(tb => (string)tb.Tag, tb => tb.Text);
                var response = await _httpClient.PutAsJsonAsync($"api/databases/{CurrentDbName}/tables/{selectedTable.Name}/rows/{selectedRow.Id}", values);
                if (response.IsSuccessStatusCode) LoadCurrentDatabase();
            }
        }

        private async void DeleteRow_Click(object sender, RoutedEventArgs e)
        {
            if (ListTables.SelectedItem is Table selectedTable && DataGridMain.SelectedItem is Row selectedRow && !string.IsNullOrEmpty(CurrentDbName))
            {
                var response = await _httpClient.DeleteAsync($"api/databases/{CurrentDbName}/tables/{selectedTable.Name}/rows/{selectedRow.Id}");
                if (response.IsSuccessStatusCode) LoadCurrentDatabase();
            }
        }

        private async void CreateTable_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtNewTableName.Text) || _pendingColumns.Count == 0 || string.IsNullOrEmpty(CurrentDbName)) return;
            var newTable = new Table { Name = TxtNewTableName.Text, Columns = new List<Column>(_pendingColumns) };
            var response = await _httpClient.PostAsJsonAsync($"api/databases/{CurrentDbName}/tables", newTable);
            if (response.IsSuccessStatusCode)
            {
                _pendingColumns.Clear();
                TxtPendingCols.Text = "Колонки відсутні";
                TxtNewTableName.Clear();
                LoadCurrentDatabase();
            }
        }

        private async void RenameTable_Click(object sender, RoutedEventArgs e)
        {
            if (ListTables.SelectedItem is Table selectedTable && !string.IsNullOrWhiteSpace(TxtRenameTable.Text) && !string.IsNullOrEmpty(CurrentDbName))
            {
                var response = await _httpClient.PutAsync($"api/databases/{CurrentDbName}/tables/{selectedTable.Name}/rename?newName={TxtRenameTable.Text}", null);
                if (response.IsSuccessStatusCode) LoadCurrentDatabase();
            }
        }

        private async void DeleteTable_Click(object sender, RoutedEventArgs e)
        {
            if (ListTables.SelectedItem is Table selectedTable && !string.IsNullOrEmpty(CurrentDbName))
            {
                var response = await _httpClient.DeleteAsync($"api/databases/{CurrentDbName}/tables/{selectedTable.Name}");
                if (response.IsSuccessStatusCode) LoadCurrentDatabase();
            }
        }

        private async void Project_Click(object sender, RoutedEventArgs e)
        {
            if (ListTables.SelectedItem is Table selectedTable && ListProjectColumns.SelectedItems.Count > 0 && !string.IsNullOrEmpty(CurrentDbName))
            {
                var selectedCols = ListProjectColumns.SelectedItems.Cast<string>().ToList();
                var response = await _httpClient.PostAsJsonAsync($"api/databases/{CurrentDbName}/tables/{selectedTable.Name}/project?newName={TxtProjectName.Text}", selectedCols);
                if (response.IsSuccessStatusCode) LoadCurrentDatabase();
            }
        }

        private void AddColumn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtColName.Text)) return;
            _pendingColumns.Add(new Column { Name = TxtColName.Text, Type = (ColumnType)ComboColType.SelectedItem, IntervalStart = TxtInvStart.Text, IntervalEnd = TxtInvEnd.Text });
            TxtPendingCols.Text = string.Join("\n", _pendingColumns.Select(c => $"{c.Name} ({c.Type})"));
            TxtColName.Clear(); TxtInvStart.Clear(); TxtInvEnd.Clear();
        }

        private void TxtProjectName_GotFocus(object sender, RoutedEventArgs e) => TxtProjectName.Clear();
    }
}