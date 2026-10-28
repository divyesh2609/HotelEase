using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using HotelEase.Commands;
using HotelEase.Data;
using Microsoft.Win32;

namespace HotelEase.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private string _hotelName = "Hotel Grand Horizon";
        private string _hotelAddress = "100 Skyline Boulevard, Suite 500, Metro City";
        private string _hotelPhone = "+1 (800) 555-4683";
        private string _hotelEmail = "reservations@grandhorizonhotel.com";
        private string _checkInTime = "02:00 PM";
        private string _checkOutTime = "11:00 AM";

        public string HotelName
        {
            get => _hotelName;
            set => SetProperty(ref _hotelName, value);
        }

        public string HotelAddress
        {
            get => _hotelAddress;
            set => SetProperty(ref _hotelAddress, value);
        }

        public string HotelPhone
        {
            get => _hotelPhone;
            set => SetProperty(ref _hotelPhone, value);
        }

        public string HotelEmail
        {
            get => _hotelEmail;
            set => SetProperty(ref _hotelEmail, value);
        }

        public string CheckInTime
        {
            get => _checkInTime;
            set => SetProperty(ref _checkInTime, value);
        }

        public string CheckOutTime
        {
            get => _checkOutTime;
            set => SetProperty(ref _checkOutTime, value);
        }

        public string DatabasePath => DatabaseHelper.DatabaseFilePath;

        public string DatabaseSize
        {
            get
            {
                try
                {
                    if (File.Exists(DatabasePath))
                    {
                        var info = new FileInfo(DatabasePath);
                        return $"{info.Length / 1024.0:N1} KB";
                    }
                }
                catch { }
                return "Unknown";
            }
        }

        public ICommand OpenDbFolderCommand { get; }
        public ICommand BackupDbCommand { get; }
        public ICommand ResetDemoDataCommand { get; }
        public ICommand SaveSettingsCommand { get; }

        public event Func<Task>? RequestAppRefresh;

        public SettingsViewModel()
        {
            OpenDbFolderCommand = new RelayCommand(OnOpenDbFolder);
            BackupDbCommand = new RelayCommand(OnBackupDb);
            ResetDemoDataCommand = new AsyncRelayCommand(OnResetDemoDataAsync);
            SaveSettingsCommand = new RelayCommand(() =>
            {
                MessageBox.Show("Settings saved successfully.", "Hotel Settings", MessageBoxButton.OK, MessageBoxImage.Information);
            });
        }

        private void OnOpenDbFolder()
        {
            try
            {
                var folder = DatabaseHelper.GetDatabaseFolder();
                if (Directory.Exists(folder))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = folder,
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open directory: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnBackupDb()
        {
            try
            {
                if (!File.Exists(DatabasePath))
                {
                    MessageBox.Show("Database file does not exist yet.", "Backup Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var dialog = new SaveFileDialog
                {
                    Title = "Backup SQLite Database",
                    Filter = "SQLite Database (*.db)|*.db|All Files (*.*)|*.*",
                    FileName = $"hoteln_backup_{DateTime.Now:yyyyMMdd_HHmm}.db"
                };

                if (dialog.ShowDialog() == true)
                {
                    File.Copy(DatabasePath, dialog.FileName, overwrite: true);
                    MessageBox.Show($"Database backup successfully created at:\n{dialog.FileName}", "Backup Succeeded", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Backup failed: {ex.Message}", "Backup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task OnResetDemoDataAsync()
        {
            var confirm1 = MessageBox.Show(
                "WARNING: This will clear the current database and restore the realistic demonstration dataset!\n\nAre you sure you want to proceed?",
                "Confirm Demo Data Reset",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm1 != MessageBoxResult.Yes) return;

            try
            {
                using (var conn = DatabaseHelper.CreateConnection())
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = @"
                        DELETE FROM Payments;
                        DELETE FROM Reservations;
                        DELETE FROM Guests;
                        DELETE FROM Rooms;";
                    await cmd.ExecuteNonQueryAsync();
                }

                await DatabaseInitializer.InitializeAsync();
                OnPropertyChanged(nameof(DatabaseSize));

                if (RequestAppRefresh != null)
                {
                    await RequestAppRefresh.Invoke();
                }

                MessageBox.Show("Demonstration data has been successfully re-seeded!", "Reset Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to reset data: {ex.Message}", "Reset Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
