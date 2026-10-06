using Dbms.Core;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Collections.Generic;
using System.Net.Http.Json;

namespace Dbms.Desktop
{
    public partial class MainWindow : Window
    {
        private readonly HttpClient _httpClient;
        private Database _currentDb;

        public MainWindow()
        {
            InitializeComponent();
            _httpClient = new HttpClient { BaseAddress = new System.Uri("http://localhost:5088/") };
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var response = await _httpClient.GetAsync("api/database");
                if (response.IsSuccessStatusCode)
                {
                    _currentDb = await response.Content.ReadFromJsonAsync<Database>();
                    TableSelector.ItemsSource = _currentDb?.Tables;
                    TableSelector.DisplayMemberPath = "Name";
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
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
            MessageBox.Show("Saved to disk");
        }

        private void TableSelector_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (TableSelector.SelectedItem is Table selectedTable)
            {
                DataGridMain.ItemsSource = selectedTable.Rows;
            }
        }

        private async void AddRow_Click(object sender, RoutedEventArgs e)
        {
            if (TableSelector.SelectedItem is Table selectedTable)
            {
                try
                {
                    var values = JsonSerializer.Deserialize<Dictionary<string, string>>(TxtInput.Text);
                    var response = await _httpClient.PostAsJsonAsync($"api/database/tables/{selectedTable.Name}/rows", values);
                    
                    if (response.IsSuccessStatusCode) Refresh_Click(null, null);
                    else MessageBox.Show("Validation failed!");
                }
                catch { MessageBox.Show("Invalid input format."); }
            }
        }
    }
}
