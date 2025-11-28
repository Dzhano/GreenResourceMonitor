using GreenResourceMonitor.Models;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;

namespace GreenResourceMonitor.Services
{
	public class SQLiteService : IDisposable
	{
		private readonly string databasePath;
		private readonly SqliteConnection connection;

		public SQLiteService(string dbPath = null)
		{
			// SQLitePCL.Batteries.Init(); // Soon to be added

			string folder = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
				"GreenResourceMonitor", "snaphots.db");
		 	
			databasePath = string.IsNullOrWhiteSpace(dbPath) ? folder : dbPath;

			Directory.CreateDirectory(Path.GetDirectoryName(databasePath) ?? ".");

			connection = new SqliteConnection($"Data Source={databasePath}");
			// connection.Open();

			InitializeDatabase();
		}

		private void InitializeDatabase()
		{
			string createTableQuery = @"
				CREATE TABLE IF NOT EXISTS Snapshots (
					Id INTEGER PRIMARY KEY AUTOINCREMENT,
					Timestamp DATETIME NOT NULL,
					ProcessName TEXT NOT NULL,
					CpuPercent REAL NOT NULL,
					WorkingSetBytes INTEGER NOT NULL,
					EnergyWh REAL NOT NULL,
					CO2Grams REAL NOT NULL,
					CostUSD REAL NOT NULL
				);";
			using (SqliteCommand command = new SqliteCommand(createTableQuery, connection))
			{
				// command.ExecuteNonQuery();
			}
		}

		public void InsertSnapshot(ProcessSnapshot snapshot)
		{
			string insertQuery = @"
				INSERT INTO Snapshots (Timestamp, ProcessName, CpuPercent, WorkingSetBytes, EnergyWh, CO2Grams, CostUSD)
				VALUES (@Timestamp, @ProcessName, @CpuPercent, @WorkingSetBytes, @EnergyWh, @CO2Grams, @CostUSD);";
			using (SqliteCommand command = new SqliteCommand(insertQuery, connection))
			{
				command.Parameters.AddWithValue("@Timestamp", snapshot.UtcTimestamp);
				command.Parameters.AddWithValue("@ProcessName", snapshot.ProcessName);
				command.Parameters.AddWithValue("@CpuPercent", snapshot.CpuPercent);
				command.Parameters.AddWithValue("@WorkingSetBytes", snapshot.WorkingSetBytes);
				command.Parameters.AddWithValue("@EnergyWh", snapshot.EnergyWh);
				command.Parameters.AddWithValue("@CO2Grams", snapshot.CO2Grams);
				command.Parameters.AddWithValue("@CostUSD", snapshot.CostUSD);
				command.ExecuteNonQuery();
			}
		}

		public void InsertSnapshots(IEnumerable<ProcessSnapshot> snapshots)
		{
			using (SqliteTransaction transaction = connection.BeginTransaction())
			{
				foreach (ProcessSnapshot snapshot in snapshots)
				{
					InsertSnapshot(snapshot);
				}
				transaction.Commit();
			}
		}

		// Basic query for export or graphing
		public IEnumerable<(DateTime ts, double energyWh)> GetProcessEnergySeries(string processName)
		{
			using (SqliteCommand command = connection.CreateCommand())
			{
				command.CommandText = @"
                SELECT Timestamp, SUM(energyWh) as totalWh
                FROM Snapshots
                WHERE ProcessName = $name
                GROUP BY Timestamp
                ORDER BY Timestamp;";
				command.Parameters.AddWithValue("$Name", processName); // Prevent SQL injection

				using (var reader = command.ExecuteReader())
				{
					while (reader.Read())
					{
						DateTime ts = DateTime.Parse(reader.GetString(0));
						double ew = reader.IsDBNull(1) ? 0.0 : reader.GetDouble(1);
						yield return (ts, ew);
					}
				}
			}
		}

		public void Dispose() => connection?.Dispose();

		public string DbPath => databasePath;
	}
}
