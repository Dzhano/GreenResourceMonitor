using GreenResourceMonitor.Models;
using GreenResourceMonitor.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace GreenResourceMonitor
{
	/// <summary>
	/// Interaction logic for SettingsWindow.xaml
	/// </summary>
	public partial class SettingsWindow : Window
	{
		private readonly SettingsService service;
		private readonly AppSettings appSettings;

		public AppSettings UpdatedSettings => appSettings;

		public SettingsWindow()
		{
			InitializeComponent();
			service = new SettingsService();
			appSettings = service.Load();

			LoadInfoFields();
		}

		private void LoadInfoFields()
		{
			PriceBox.Text = appSettings.CostPerKWhUSD.ToString();
			CO2Box.Text = appSettings.Co2PerWh.ToString();
			IntervalBox.Text = appSettings.SamplingSeconds.ToString();
			CalibBox.Text = appSettings.CalibrationFactor.ToString();
			
			try
			{
				switch (appSettings.StorageMode)
				{
					case StorageMode.SQLiteOnly:
						StorageModeCombo.SelectedIndex = 1;
						break;
					case StorageMode.Both:
						StorageModeCombo.SelectedIndex = 2;
						break;
					case StorageMode.CSVOnly:
					default:
						StorageModeCombo.SelectedIndex = 0;
						break;
				}
			}
			catch (Exception ex)
			{
				StorageModeCombo.SelectedIndex = 0;
				Debug.WriteLine("Failed to set StorageModeCombo: " + ex.Message);
			}
		}

		private void Save_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				string tag = null;
				if (StorageModeCombo.SelectedItem is ComboBoxItem item)
					tag = item.Tag?.ToString();
				else tag = StorageModeCombo.SelectedValue as string;
				if (!string.IsNullOrEmpty(tag))
				{
					switch (tag)
					{
						case "SQLiteOnly":
							appSettings.StorageMode = StorageMode.SQLiteOnly;
							break;
						case "Both":
							appSettings.StorageMode = StorageMode.Both;
							break;
						case "SVOnly":
						default:
							appSettings.StorageMode = StorageMode.CSVOnly;
							break;
					}
				}
				else appSettings.StorageMode = StorageMode.CSVOnly;

				if (string.IsNullOrEmpty(appSettings.SQLPath))
					appSettings.SQLPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GreenResourceMonitor", "snapshots.db");
					// The directory already exists since SettingsService.Load() creates it if missing
			}
			catch (Exception ex)
			{
				MessageBox.Show("Failed to save storage mode: " + ex.Message);
			}
			try
			{
				appSettings.CostPerKWhUSD = double.Parse(PriceBox.Text);
				appSettings.Co2PerWh = double.Parse(CO2Box.Text);
				appSettings.SamplingSeconds = int.Parse(IntervalBox.Text);
				appSettings.CalibrationFactor = double.Parse(CalibBox.Text);

				service.Save(appSettings);
				DialogResult = true;
				Close();
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error saving settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void Cancel_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}

		private void MigrateCsvToDbButton_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				string csvPath = Path.Combine(
					AppDomain.CurrentDomain.BaseDirectory,
					"logs",
					"snapshots.csv"
				);

				if (!File.Exists(csvPath))
				{
					MessageBox.Show("No CSV file found to import.");
					return;
				}

				string tempPath = Path.Combine(Path.GetTempPath(), "snapshots_migration_" + Guid.NewGuid().ToString() + ".csv");
				File.Copy(csvPath, tempPath, overwrite: true);

				List<ProcessSnapshot> snapshots = new List<ProcessSnapshot>();
				var lines = File.ReadAllLines(tempPath).Skip(1); // Skip header
				
				if (lines.Count() <= 1)
				{
					MessageBox.Show("CSV contains no data rows to import.", "Import CSV", MessageBoxButton.OK, MessageBoxImage.Information);
					try { File.Delete(tempPath); } catch { }
					return;
				}
				
				foreach (var line in lines)
				{
					if (string.IsNullOrEmpty(line)) continue;
					string[] parts = line.Split(',', (char)StringSplitOptions.RemoveEmptyEntries);
					if (parts.Length < 8) continue;

					try
					{
						snapshots.Add(new ProcessSnapshot
						{
							UtcTimestamp = DateTime.Parse(parts[0]),
							Pid = int.Parse(parts[1]),
							ProcessName = parts[2],
							CpuPercent = double.Parse(parts[3]),
							WorkingSetBytes = long.Parse(parts[4]),
							EnergyWh = double.Parse(parts[5]),
							CO2Grams = double.Parse(parts[6]),
							CostUSD = double.Parse(parts[7])
						});
					}
					catch (Exception)
					{
						// Skip malformed line but continue importing
						continue;
					}
				}
				if (snapshots.Count == 0)
				{
					MessageBox.Show("No valid records found in CSV to import.", "Import CSV", MessageBoxButton.OK, MessageBoxImage.Information);
					try { File.Delete(tempPath); } catch { }
					return;
				}

				try
				{
					SqlServerService sql = new SqlServerService(appSettings.SQLPath);
					sql.InsertSnapshots(snapshots);
				}
				catch (Exception ex)
				{
					MessageBox.Show("Failed to import into SQL Server database: " + ex.Message, "Import CSV", MessageBoxButton.OK, MessageBoxImage.Error);
					try { File.Delete(tempPath); } catch { }
					return;
				}

				try { File.Delete(tempPath); } catch { /*ignore*/ }
				MessageBox.Show($"Successfully imported {snapshots.Count} rows into SQL database.", "Import CSV", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Unexpected error during import: " + ex.Message, "Import CSV", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		// Deletes CSV data either by clearing rows or deleting the file
		private void DeleteCsvDataButton_Click(object sender, RoutedEventArgs e)
		{
			string csvPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, 
				"logs", 
				"snapshots.csv");
			if (!File.Exists(csvPath))
			{
				MessageBox.Show("CSV file not found: " + csvPath, "Clear CSV", MessageBoxButton.OK, MessageBoxImage.Information);
				return;
			}

			var confirm = MessageBox.Show(
				"This will permanently delete the CSV file that store all of the information about the snaphots from the moment of creation. Do you want to continue?",
				"Confirm Clear CSV",
				MessageBoxButton.YesNo,
				MessageBoxImage.Warning);

			if (confirm != MessageBoxResult.Yes) return;
			
			var delete = MessageBox.Show("Do you want to delete only the snapshot rows from the CSV (header will be kept) or the file itself?" +
				"\nYes - delete only the snaphotrows" +
				"\nNo - delete the file.",
				"Delete Options",
				MessageBoxButton.YesNoCancel,
				MessageBoxImage.Warning);
			if (delete == MessageBoxResult.Cancel) return;
			if (delete == MessageBoxResult.Yes)
			{
				// Deleting only the snapshot rows, keeping the header
				try
				{
					var header = File.ReadLines(csvPath).FirstOrDefault();
					File.WriteAllText(csvPath, header + Environment.NewLine);
					MessageBox.Show("CSV data cleared successfully.", "Clear CSV", MessageBoxButton.OK, MessageBoxImage.Information);
				}
				catch (Exception ex)
				{
					MessageBox.Show("Failed to clear CSV data: " + ex.Message, "Clear CSV", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
			else if (delete == MessageBoxResult.No)
			{
				// Deleting the file itself without making a new one to avoid file locks
				try
				{
					File.Delete(csvPath);
					MessageBox.Show("CSV file successfully deleted.", "Clear CSV", MessageBoxButton.OK, MessageBoxImage.Information);
				}
				catch (Exception ex)
				{
					MessageBox.Show("Failed to delete CSV data: " + ex.Message, "Delete CSV", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		// Deletes all SQL data from the database
		private void DeleteSqlDataButton_Click(object sender, RoutedEventArgs e)
		{
			var confirm = MessageBox.Show(
				"This will delete ALL snapshots from the SQL database. This action is irreversible. Do you want to continue?",
				"Confirm Clear SQL DB",
				MessageBoxButton.YesNo,
				MessageBoxImage.Warning);

			if (confirm != MessageBoxResult.Yes) return;

			try
			{
				SqlServerService sql = new SqlServerService();
				sql.DeleteSnapshots();

				MessageBox.Show("SQL database cleared successfully.", "Clear SQL DB", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Failed to clear SQL DB: " + ex.Message, "Clear SQL DB", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
	}
}
