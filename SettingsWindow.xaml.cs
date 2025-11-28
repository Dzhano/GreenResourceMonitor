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

				if (string.IsNullOrEmpty(appSettings.SQLitePath))
					appSettings.SQLitePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GreenResourceMonitor", "snapshots.db");
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
	}
}
