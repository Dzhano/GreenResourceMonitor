using GreenResourceMonitor.Models;
using GreenResourceMonitor.Services;
using System;
using System.Windows;

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
		}

		private void Save_Click(object sender, RoutedEventArgs e)
		{
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
