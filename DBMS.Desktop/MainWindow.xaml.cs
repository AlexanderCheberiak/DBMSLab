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
            // ПЕРЕВІРТЕ ПОРТ СЕРВЕРА (замініть 5088 на ваш поточний)
            _httpClient = new HttpClient { BaseAddress = new System.Uri("http://localhost:5088/") };
            ComboColType.ItemsSource = System.Enum.GetValues(typeof(ColumnType));
            ComboColType.SelectedIndex = 0;
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            var response = await _httpClient.GetAsync("api/database");
            if (response.IsSuccessStatusCode)
            {
                _currentDb = await response.Content.ReadFromJsonAsync<Database>();
                TableSelector.ItemsSource = _currentDb?.Tables;
                TableSelector.DisplayMemberPath = "Name";
                if (TableSelector.Items.Count > 0) TableSelector.SelectedIndex = 0;
            }
        }

        private async void LoadDb_Click(object sender, RoutedEventArgs e)
        {
            await _httpClient.PostAsync("api/database/load", null);
            Refresh_Click(null, null);
        }

        private async void SaveDb_Click(object sender, RoutedEventArgs e)
        {
            await _httpClient.PostAsync("api/database/save", null);
            MessageBox.Show("Базу збережено на диск.");
        }

        private void TableSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TableSelector.SelectedItem is Table selectedTable)
            {
                // Оновлення таблиці DataGrid
                DataGridMain.AutoGenerateColumns = false;
                DataGridMain.Columns.Clear();
                DataGridMain.Columns.Add(new DataGridTextColumn { Header = "Id", Binding = new System.Windows.Data.Binding("Id") });

                PanelRowInputs.Children.Clear();
                ListProjectColumns.Items.Clear();

                foreach (var col in selectedTable.Columns)
                {
                    // Генерація колонок для DataGrid
                    DataGridMain.Columns.Add(new DataGridTextColumn { Header = col.Name, Binding = new System.Windows.Data.Binding($"Values[{col.Name}]") });

                    // Генерація полів для додавання рядка
                    PanelRowInputs.Children.Add(new TextBlock { Text = $"{col.Name} ({col.Type})", Margin = new Thickness(0, 5, 0, 0) });
                    var inputField = new TextBox { Tag = col.Name, Height = 25 };
                    if (!string.IsNullOrEmpty(col.IntervalStart))
                        inputField.ToolTip = $"Діапазон: [{col.IntervalStart} - {col.IntervalEnd}]";
                    PanelRowInputs.Children.Add(inputField);

                    // Наповнення списку для проєкції
                    ListProjectColumns.Items.Add(col.Name);
                }
                DataGridMain.ItemsSource = selectedTable.Rows;
            }
        }

        private async void AddRow_Click(object sender, RoutedEventArgs e)
        {
            if (TableSelector.SelectedItem is Table selectedTable)
            {
                var values = new Dictionary<string, string>();

                // Зчитування даних зі згенерованих текстових полів
                foreach (var child in PanelRowInputs.Children)
                {
                    if (child is TextBox tb && tb.Tag is string colName)
                        values[colName] = tb.Text;
                }

                var response = await _httpClient.PostAsJsonAsync($"api/database/tables/{selectedTable.Name}/rows", values);
                if (response.IsSuccessStatusCode) Refresh_Click(null, null);
                else MessageBox.Show("Помилка валідації. Перевірте типи введених даних.");
            }
        }

        private async void Project_Click(object sender, RoutedEventArgs e)
        {
            if (TableSelector.SelectedItem is Table selectedTable && ListProjectColumns.SelectedItems.Count > 0)
            {
                var selectedCols = ListProjectColumns.SelectedItems.Cast<string>().ToList();
                var newName = TxtProjectName.Text;

                var response = await _httpClient.PostAsJsonAsync($"api/database/tables/{selectedTable.Name}/project?newName={newName}", selectedCols);
                if (response.IsSuccessStatusCode) Refresh_Click(null, null);
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
            TxtPendingCols.Text = string.Join(", ", _pendingColumns.Select(c => c.Name));
            TxtColName.Clear();
        }

        private async void CreateTable_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtNewTableName.Text) || _pendingColumns.Count == 0) return;

            var newTable = new Table { Name = TxtNewTableName.Text, Columns = new List<Column>(_pendingColumns) };
            var response = await _httpClient.PostAsJsonAsync("api/database/tables", newTable);

            if (response.IsSuccessStatusCode)
            {
                _pendingColumns.Clear();
                TxtPendingCols.Text = "Немає";
                Refresh_Click(null, null);
            }
        }

        private void TxtProjectName_GotFocus(object sender, RoutedEventArgs e) => TxtProjectName.Clear();
    }
}