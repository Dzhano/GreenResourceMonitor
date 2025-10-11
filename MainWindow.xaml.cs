using GreenResourceMonitor.Models;
using GreenResourceMonitor.Services;
using GreenResourceMonitor.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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

		public MainWindow()
		{
			InitializeComponent();
			DataContext = _vm;
			ProcessesGrid.ItemsSource = _vm.Snapshots;

			StopButton.IsEnabled = false;

		}

		private async void StartButton_Click(object sender, RoutedEventArgs e)
		{
			StartButton.IsEnabled = false;
			StopButton.IsEnabled = true;
			_vm.Status = "Running";

			cancellation = new CancellationTokenSource();
			collector = new ProcessCollectorService(TimeSpan.FromSeconds(1), System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "snapshots.csv"));
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
	}
}