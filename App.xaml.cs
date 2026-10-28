using System;
using System.Windows;
using HotelEase.Data;

namespace HotelEase
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// Demonstrates WPF Application Lifecycle, Global Exception Handling, and Database Bootstrap.
    /// </summary>
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Setup global exception handling
            DispatcherUnhandledException += (s, args) =>
            {
                MessageBox.Show(
                    $"An unexpected error occurred:\n\n{args.Exception.Message}\n\nTechnical details:\n{args.Exception}",
                    "HotelEase — Application Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                args.Handled = true;
            };

            try
            {
                // Initialize SQLite database and seed demo data if empty
                await DatabaseInitializer.InitializeAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to initialize the database:\n{ex.Message}\n\nApplication may not function properly.",
                    "Database Initialization Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
    }
}
