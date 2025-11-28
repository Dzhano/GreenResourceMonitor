using GreenResourceMonitor.Models;
using GreenResourceMonitor.Services;
using GreenResourceMonitor.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;

namespace GreenResourceMonitor
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private readonly ProcessSnapshotViewModel _vm = new ProcessSnapshotViewModel();
		private IProcessCollector collector;
		private CancellationTokenSource cancellation;

		private double sessionTotalEnergyWh = 0;
		private double sessionTotalCO2Grams = 0;

		private SettingsService settingsService;
		private AppSettings appSettings;
		private SqlServerService sqlService;

		public MainWindow()
		{
			InitializeComponent();
			DataContext = _vm;
			ProcessesGrid.ItemsSource = _vm.Snapshots;

			StopButton.IsEnabled = false;

			settingsService = new SettingsService();
			appSettings = settingsService.Load();
			sqlService = new SqlServerService();
		}

		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);
			collector?.StopAsync().ConfigureAwait(false).GetAwaiter().GetResult(); // ensure collector stopped
			cancellation?.Cancel();
			collector?.Dispose();
		}

		private async void StartButton_Click(object sender, RoutedEventArgs e)
		{
			StartButton.IsEnabled = false;
			StopButton.IsEnabled = true;
			_vm.Status = "Running";

			cancellation = new CancellationTokenSource();
			collector = new ProcessCollectorService(TimeSpan.FromSeconds(appSettings.SamplingSeconds), 
						System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "snapshots.csv"),
						appSettings, sqlService);
			collector.OnProcessSnapshot += Collector_OnProcessSnapshot;
			await collector.StartAsync(cancellation.Token);
		}

		private async void StopButton_Click(object sender, RoutedEventArgs e)
		{
			StopButton.IsEnabled = false;
			StartButton.IsEnabled = false; // disable both while stopping
			_vm.Status = "Stopping";
			StartButton.IsEnabled = true; // re-enable start after stopped

			if (collector != null)
			{
				collector.OnProcessSnapshot -= Collector_OnProcessSnapshot;
				await collector.StopAsync();
				collector.Dispose();
				collector = null;
			}

			cancellation?.Cancel();
		}


		private void Collector_OnProcessSnapshot(IEnumerable<ProcessSnapshot> snapshot)
		{
			Dispatcher.Invoke(() =>
			{
				_vm.Snapshots.Clear();
				foreach (var snap in snapshot.OrderByDescending(s => s.CpuPercent)) 
					_vm.Snapshots.Add(snap);

				sessionTotalEnergyWh += snapshot.Sum(s => s.EnergyWh);
				sessionTotalCO2Grams += snapshot.Sum(s => s.CO2Grams);

				TotalEnergyLabel.Content = $"Total Energy at the time: {sessionTotalEnergyWh:F4} Wh";
				TotalCO2Label.Content = $"Total CO₂ at the time: {sessionTotalCO2Grams:F3} g";
			});
		}

		private void SettingsButton_Click(object sender, RoutedEventArgs e)
		{
			var settingsWindow = new SettingsWindow();
			if (settingsWindow.ShowDialog() == true)
			{
				appSettings = settingsWindow.UpdatedSettings;
				MessageBox.Show("Settings updated.", "Settings", MessageBoxButton.OK);

				settingsService.Save(appSettings);
			}
		}

		private void GraphsButton_Click(object sender, RoutedEventArgs e)
		{
			string csv = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "snapshots.csv");
			GraphsWindow w = new GraphsWindow(csv, appSettings, sqlService);
			w.Show();
		}
	}
}