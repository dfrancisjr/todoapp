using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Threading;
using System.ComponentModel;
using System.Windows.Data;
using System.Diagnostics;

namespace TodoApp
{
    public partial class MainWindow : Window
    {
        string dbPath = "tasks.db";
        List<TaskItem> tasks = new List<TaskItem>();
        ICollectionView taskView;
        DispatcherTimer reminderTimer;

        public MainWindow()
        {
            InitializeComponent();
            InitDatabase();
            LoadData();
            StartTimer();
        }

        private void InitDatabase()
        {
            if (!File.Exists(dbPath)) SQLiteConnection.CreateFile(dbPath);
            using (var conn = new SQLiteConnection($"Data Source={dbPath};"))
            {
                conn.Open();
                // Inside InitDatabase()
                string sql = @"CREATE TABLE IF NOT EXISTS Tasks (Id INTEGER PRIMARY KEY AUTOINCREMENT, 
                        TaskName TEXT, Category TEXT, TicketNumber TEXT, Description TEXT, Notes TEXT, 
                        PercentComplete INTEGER, Priority TEXT, IsReminder INTEGER, ReminderDate TEXT, 
                        StartDate TEXT, EndDate TEXT, LastModified TEXT, IsImportant INTEGER)";
                new SQLiteCommand(sql, conn).ExecuteNonQuery();
            }
        }

        private void LoadData()
        {
            tasks.Clear();
            using (var conn = new SQLiteConnection($"Data Source={dbPath};"))
            {
                conn.Open();
                var reader = new SQLiteCommand("SELECT * FROM Tasks", conn).ExecuteReader();
                while (reader.Read())
                {
                    tasks.Add(new TaskItem
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        TaskName = reader["TaskName"].ToString(),
                        Category = reader["Category"].ToString(),
                        TicketNumber = reader["TicketNumber"].ToString(),
                        PercentComplete = Convert.ToInt32(reader["PercentComplete"]),
                        Priority = reader["Priority"].ToString(),
                        Description = reader["Description"].ToString(),
                        Notes = reader["Notes"].ToString(),
                        EndDate = reader["EndDate"].ToString(),
                        LastModified = reader["LastModified"].ToString(),
                        IsImportant = Convert.ToInt32(reader["IsImportant"]) == 1,
                        IsReminder = Convert.ToInt32(reader["IsReminder"]) == 1,
                        ReminderDate = reader["ReminderDate"] == DBNull.Value || string.IsNullOrEmpty(reader["ReminderDate"].ToString()) ? null : (DateTime?)DateTime.Parse(reader["ReminderDate"].ToString())
                    });
                }
            }
            taskView = CollectionViewSource.GetDefaultView(tasks);
            dgTasks.ItemsSource = taskView;
            UpdateStatusBar();
        }

        private void AddNewTask_Click(object sender, RoutedEventArgs e) { ExecuteQuery(null); LoadData(); ClearInputs_Click(null, null); }

        private void UpdateTask_Click(object sender, RoutedEventArgs e)
        {
            if (dgTasks.SelectedItem is TaskItem s) { ExecuteQuery(s.Id); LoadData(); }
            else { MessageBox.Show("Select a task first!"); }
        }

        private void ExecuteQuery(int? id)
        {
            using (var conn = new SQLiteConnection($"Data Source={dbPath};"))
            {
                conn.Open();
                string sql = id == null
                    ? "INSERT INTO Tasks (TaskName, Category, TicketNumber, Description, Notes, PercentComplete, Priority, IsImportant, IsReminder, ReminderDate, StartDate, EndDate, LastModified) VALUES (@n, @c, @t, @d, @no, @p, @pr, @i, @ir, @rd, @sd, @ed, @lm)"
                    : "UPDATE Tasks SET TaskName=@n, Category=@c, TicketNumber=@t, Description=@d, Notes=@no, PercentComplete=@p, Priority=@pr, IsImportant=@i, IsReminder=@ir, ReminderDate=@rd, StartDate=@sd, EndDate=@ed, LastModified=@lm WHERE Id=@id";

                var cmd = new SQLiteCommand(sql, conn);

                // Date Pickers to SQLite Strings
                cmd.Parameters.AddWithValue("@sd", dpStart.SelectedDate?.ToString("yyyy-MM-dd") ?? "");
                cmd.Parameters.AddWithValue("@ed", dpEnd.SelectedDate?.ToString("yyyy-MM-dd") ?? "");

                // ... rest of your parameters (TaskName, Category, etc.) ...
                cmd.Parameters.AddWithValue("@n", txtName.Text);
                cmd.Parameters.AddWithValue("@lm", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                if (id.HasValue) cmd.Parameters.AddWithValue("@id", id.Value);

                cmd.ExecuteNonQuery();
            }
            LoadData(); // Refresh grid instantly

        }
        

        private void DeleteTask_Click(object sender, RoutedEventArgs e)
        {
            if (dgTasks.SelectedItem is TaskItem s && MessageBox.Show("Delete?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                using (var conn = new SQLiteConnection($"Data Source={dbPath};")) { conn.Open(); new SQLiteCommand($"DELETE FROM Tasks WHERE Id={s.Id}", conn).ExecuteNonQuery(); }
                LoadData();
                ClearInputs_Click(null, null);
            }
        }

        private void CleanOldTasks_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Delete old completed tasks?", "Confirm", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                using (var conn = new SQLiteConnection($"Data Source={dbPath};")) { conn.Open(); new SQLiteCommand("DELETE FROM Tasks WHERE PercentComplete = 100 AND Date(EndDate) <= date('now', '-30 days')", conn).ExecuteNonQuery(); }
                LoadData();
            }
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e) { taskView.Filter = (o) => (o as TaskItem).TaskName.ToLower().Contains(txtSearch.Text.ToLower()) || (o as TaskItem).TicketNumber.ToLower().Contains(txtSearch.Text.ToLower()); }

        private void DgTasks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgTasks.SelectedItem is TaskItem s)
            {
                // ... existing field filling ...
                dpStart.SelectedDate = string.IsNullOrEmpty(s.StartDate) ? null : (DateTime?)DateTime.Parse(s.StartDate);
                dpEnd.SelectedDate = string.IsNullOrEmpty(s.EndDate) ? null : (DateTime?)DateTime.Parse(s.EndDate);
            }
        }

        private void dgTasks_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) { txtName.Focus(); txtName.SelectAll(); }

        private void StartTimer()
        {
            reminderTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(60) };
            reminderTimer.Tick += (s, e) => {
                var due = tasks.FirstOrDefault(t => t.IsReminder && t.ReminderDate <= DateTime.Now && !t.ReminderDismissed);
                if (due != null) { MessageBox.Show("Reminder: " + due.TaskName); due.ReminderDismissed = true; }
            };
            reminderTimer.Start();
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            var t = (sender as Hyperlink).DataContext as TaskItem;
            string url = t.Category == "Jira" ? $"https://jira.com/{t.TicketNumber}" : $"https://service-now.com/{t.TicketNumber}";
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }

        private void SortByDate_Click(object sender, RoutedEventArgs e)
        {
            // 1. Get the view of our data
            var view = CollectionViewSource.GetDefaultView(dgTasks.ItemsSource);

            // 2. Clear any existing sorting
            view.SortDescriptions.Clear();

            // 3. Sort by EndDate (Ascending = soonest dates at the top)
            view.SortDescriptions.Add(new SortDescription("EndDate", ListSortDirection.Ascending));

            // 4. Secondary sort: Put "Important" tasks at the very top within those dates
            view.SortDescriptions.Add(new SortDescription("IsImportant", ListSortDirection.Descending));

            MessageBox.Show("Tasks sorted by urgency (Dates + Importance).");
        }

        private void ExportCSV_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder("Task,Category,Priority,Percent\n");
            foreach (var t in tasks) sb.AppendLine($"{t.TaskName},{t.Category},{t.Priority},{t.PercentComplete}");
            File.WriteAllText("tasks.csv", sb.ToString()); MessageBox.Show("Saved to tasks.csv");
        }

        private void CmbCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool isSys = cmbCategory.Text == "Jira" || cmbCategory.Text == "ServiceNow";
            if (lblTicket != null) { lblTicket.Visibility = isSys ? Visibility.Visible : Visibility.Collapsed; txtTicket.Visibility = isSys ? Visibility.Visible : Visibility.Collapsed; }
        }

        private void ClearInputs_Click(object sender, RoutedEventArgs e) { dgTasks.SelectedItem = null; txtName.Clear(); txtTicket.Clear(); txtDesc.Clear(); txtNotes.Clear(); sldPercent.Value = 0; }
        private void SetLightTheme(object sender, RoutedEventArgs e) => Application.Current.Resources.MergedDictionaries[0].Source = new Uri("LightTheme.xaml", UriKind.Relative);
        private void SetDarkTheme(object sender, RoutedEventArgs e) => Application.Current.Resources.MergedDictionaries[0].Source = new Uri("DarkTheme.xaml", UriKind.Relative);
        private void UpdateStatusBar() { lblTotalCount.Text = tasks.Count.ToString(); lblDoneCount.Text = tasks.Count(t => t.PercentComplete == 100).ToString(); }
        private void Exit_Click(object sender, RoutedEventArgs e) => Close();
    }
}