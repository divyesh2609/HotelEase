using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace HotelEase.Data
{
    /// <summary>
    /// Manages SQLite database connection and file path in a persistent application data folder.
    /// Demonstrates ADO.NET and SQLite best practices in WPF desktop applications.
    /// </summary>
    public static class DatabaseHelper
    {
        private static readonly string AppDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HotelEase");

        public static readonly string DatabaseFilePath = Path.Combine(AppDataFolder, "hoteln.db");

        public static readonly string ConnectionString = $"Data Source={DatabaseFilePath};";

        /// <summary>
        /// Creates and opens a new SQLite connection with foreign keys enabled.
        /// </summary>
        public static SqliteConnection CreateConnection()
        {
            EnsureDirectoryExists();
            var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            // Explicitly enable foreign key constraints in SQLite
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "PRAGMA foreign_keys = ON;";
            cmd.ExecuteNonQuery();

            return connection;
        }

        public static void EnsureDirectoryExists()
        {
            if (!Directory.Exists(AppDataFolder))
            {
                Directory.CreateDirectory(AppDataFolder);
            }
        }

        public static string GetDatabaseFolder() => AppDataFolder;
    }
}
