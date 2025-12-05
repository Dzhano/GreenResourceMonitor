using GreenResourceMonitor.Models;
using GreenResourceMonitor.Services;
using System;
using System.Windows;
using System.Windows.Controls;

namespace GreenResourceMonitor
{
	/// <summary>
	/// Interaction logic for CountrySelectionWindow.xaml
	/// </summary>
	public partial class CountrySelectionWindow : Window
	{
		private readonly CountryDataService countryDataService;

		public CountryData SelectedCountryData { get; private set; }

		public CountrySelectionWindow()
		{
			InitializeComponent();
			countryDataService = new CountryDataService();
		}

		private async void CountryButton_Click(object sender, RoutedEventArgs e)
		{
			if (sender is Button button && button.Tag is string countryCode)
			{
				string countryName = button.Content.ToString();

				this.IsEnabled = false; // Disable the window to prevent multiple clicks
				StatusText.Text = $"Retrieving data for {countryName}...";

				try
				{
					var data = await countryDataService.GetCountryDataAsync(countryCode, countryName);
					if (data != null)
					{
						SelectedCountryData = await countryDataService.GetCountryDataAsync(countryCode, countryName);
						DialogResult = true;
						StatusText.Text = $"Data for {countryName} retrieved successfully.";
						MessageBox.Show($"Data for {countryName} retrieved successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
						// I wanted to keep the window open after selection but without "DialogResult = true" SettingsWindow does not recognize selection
					}
					else MessageBox.Show($"No data available for {countryName}.", "Data Unavailable", MessageBoxButton.OK, MessageBoxImage.Warning);
				}
				catch (Exception ex)
				{
					MessageBox.Show($"Error retrieving country data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				}
				finally
				{
					this.IsEnabled = true; // Re-enable the window
					StatusText.Text = string.Empty;
				}
			}
		}

		private void Cancel_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}
	}
}