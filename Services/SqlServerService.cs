using GreenResourceMonitor.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;

namespace GreenResourceMonitor.Services
{
	public class SqlServerService
	{
		private readonly string databasePath;
		private readonly string masterConnectionString;
		private readonly string databaseConnectionString;

		public SqlServerService(string dbPath = null)
		{
			// Build clean folder path
			string folder = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
				"GreenResourceMonitor");
			Directory.CreateDirectory(folder);
			
			// Decide the database file path
			databasePath = string.IsNullOrWhiteSpace(dbPath)
				? Path.Combine(folder, "snapshots.mdf") : dbPath;
			
			// Connection strings
			masterConnectionString = @"Server=(localdb)\mssqllocaldb;Integrated Security=true;Initial Catalog=master;";
			databaseConnectionString = $@"Server=(localdb)\mssqllocaldb;Integrated Security=true;AttachDbFilename={databasePath};";
			
			EnsureDatabaseExists();
			InitializeDatabase();
		}

		private void EnsureDatabaseExists()
		{
			if (!File.Exists(databasePath))
			{
				using (SqlConnection connection = new SqlConnection(masterConnectionString))
				{
					connection.Open();
					SqlCommand createDbCommand = connection.CreateCommand();
					createDbCommand.CommandText =
						$@"CREATE DATABASE [GreenResourceMonitor] ON 
						(NAME = N'GreenResourceMonitor', FILENAME = '{databasePath}')";
					createDbCommand.ExecuteNonQuery();
				}
			}
		}

		private void InitializeDatabase()
		{
			string createTableQuery = @"
				IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Snapshots' AND xtype='U')
				CREATE TABLE Snapshots (
					Id INT IDENTITY(1,1) PRIMARY KEY,
					Timestamp DATETIME NOT NULL,
					ProcessId INT NOT NULL,
					ProcessName NVARCHAR(255) NOT NULL,
					CpuPercent FLOAT NOT NULL,
					WorkingSetBytes BIGINT NOT NULL,
					EnergyWh FLOAT NOT NULL,
					CO2Grams FLOAT NOT NULL,
					CostEUR FLOAT NOT NULL
				);";
			using (SqlConnection connection = new SqlConnection(databaseConnectionString))
			{
				connection.Open();
				using (SqlCommand command = new SqlCommand(createTableQuery, connection))
				{
					command.ExecuteNonQuery();
				}
			}
		}

		public void InsertSnapshot(ProcessSnapshot processSnapshot)
		{
			using (SqlConnection connection = new SqlConnection(databaseConnectionString))
			{
				connection.Open();
				using (SqlCommand command = connection.CreateCommand())
				{
					command.CommandText = @"
						INSERT INTO Snapshots 
						(Timestamp, ProcessId, ProcessName, CpuPercent, WorkingSetBytes, EnergyWh, CO2Grams, CostEUR)
						VALUES (@Timestamp, @ProcessId, @ProcessName, @CpuPercent, @WorkingSetBytes, @EnergyWh, @CO2Grams, @CostEUR);";
					command.Parameters.AddWithValue("@Timestamp", processSnapshot.UtcTimestamp);
					command.Parameters.AddWithValue("@ProcessId", processSnapshot.Pid);
					command.Parameters.AddWithValue("@ProcessName", processSnapshot.ProcessName);
					command.Parameters.AddWithValue("@CpuPercent", processSnapshot.CpuPercent);
					command.Parameters.AddWithValue("@WorkingSetBytes", processSnapshot.WorkingSetBytes);
					command.Parameters.AddWithValue("@EnergyWh", processSnapshot.EnergyWh);
					command.Parameters.AddWithValue("@CO2Grams", processSnapshot.CO2Grams);
					command.Parameters.AddWithValue("@CostEUR", processSnapshot.CostEUR);
					command.ExecuteNonQuery();
				}
			}
		}

		public void InsertSnapshots(IEnumerable<ProcessSnapshot> snapshots)
		{
			using (SqlConnection connection = new SqlConnection(databaseConnectionString))
			{
				// We could have used InsertSnapshot in a loop, but to improve performance we reuse the same connection and transaction.
				connection.Open(); // To not open multiple times connections for each snapshot we have to recreate the code above.
				using (SqlTransaction transaction = connection.BeginTransaction())
				{
					foreach (ProcessSnapshot snapshot in snapshots)
					{
						using (SqlCommand command = connection.CreateCommand())
						{
							command.Transaction = transaction;
							command.CommandText = @"
								INSERT INTO Snapshots 
								(Timestamp, ProcessId, ProcessName, CpuPercent, WorkingSetBytes, EnergyWh, CO2Grams, CostEUR)
								VALUES (@Timestamp, @ProcessId, @ProcessName, @CpuPercent, @WorkingSetBytes, @EnergyWh, @CO2Grams, @CostEUR);";
							command.Parameters.AddWithValue("@Timestamp", snapshot.UtcTimestamp);
							command.Parameters.AddWithValue("@ProcessId", snapshot.Pid);
							command.Parameters.AddWithValue("@ProcessName", snapshot.ProcessName);
							command.Parameters.AddWithValue("@CpuPercent", snapshot.CpuPercent);
							command.Parameters.AddWithValue("@WorkingSetBytes", snapshot.WorkingSetBytes);
							command.Parameters.AddWithValue("@EnergyWh", snapshot.EnergyWh);
							command.Parameters.AddWithValue("@CO2Grams", snapshot.CO2Grams);
							command.Parameters.AddWithValue("@CostEUR", snapshot.CostEUR);
							command.ExecuteNonQuery();
						}
					}
					transaction.Commit();
				}
			}
		}

		public void DeleteSnapshots()
		{
			// Delete all snapshots
			using (SqlConnection connection = new SqlConnection(databaseConnectionString))
			{
				connection.Open();
				using (SqlCommand command = connection.CreateCommand())
				{
					command.CommandText = "DELETE FROM Snapshots;";
					command.ExecuteNonQuery();
				}
			}
		}

		public IEnumerable<ProcessSnapshot> GetSnapshots()
		{
			// Retrieve all snapshots
			List<ProcessSnapshot> snapshots = new List<ProcessSnapshot>();
			using (SqlConnection connection = new SqlConnection(databaseConnectionString))
			{
				connection.Open();
				using (SqlCommand command = connection.CreateCommand())
				{
					command.CommandText = @"
						SELECT Timestamp, ProcessId, ProcessName, CpuPercent, WorkingSetBytes, EnergyWh, CO2Grams, CostEUR
						FROM Snapshots
						ORDER BY Timestamp;";
					using (SqlDataReader reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							ProcessSnapshot snapshot = new ProcessSnapshot
							{
								UtcTimestamp = reader.GetDateTime(0),
								Pid = reader.GetInt32(1),
								ProcessName = reader.GetString(2),
								CpuPercent = reader.GetDouble(3),
								WorkingSetBytes = reader.GetInt64(4),
								EnergyWh = reader.GetDouble(5),
								CO2Grams = reader.GetDouble(6),
								CostEUR = reader.GetDouble(7)
							};
							snapshots.Add(snapshot);
						}
					}
				}
			}
			return snapshots;
		}
	}
}
