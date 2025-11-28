using DocumentFormat.OpenXml.Wordprocessing;
using GreenResourceMonitor.Models;
using GreenResourceMonitor.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GreenResourceMonitor.ViewModels
{
	/// <summary>
	/// Interaction logic for GraphsWindow.xaml
	/// </summary>
	public partial class GraphsWindow : Window
	{
		private readonly string csvPath;
		private List<ProcessSnapshot> points = new List<ProcessSnapshot>();

		// Fast searching via process name
		private Dictionary<string, List<ProcessSnapshot>> pointsByProcess = new Dictionary<string, List<ProcessSnapshot>>();

		private DateTime startDate, endDate;

		private readonly AppSettings appSettings;
		private readonly SqlServerService sqlServerService;

		private readonly Dictionary<string, Func<ProcessSnapshot, double>> metricSelectors =
			new Dictionary<string, Func<ProcessSnapshot, double>>()
		{
			{"CPU (%)", p => p.CpuPercent },
			{"Memory (Bytes)", p => (double)p.WorkingSetBytes }, // Convert long to double for plotting
            {"Energy (Wh)", p => p.EnergyWh },
			{"CO2 (g)", p => p.CO2Grams },
			{"Cost (USD)", p => p.CostUSD }
		};

		public GraphsWindow(string csvPath, AppSettings appSettings, SqlServerService sql)
		{
			InitializeComponent();
			this.csvPath = csvPath;

			PlotView.Plot.Title("Energy (Wh) over time");
			PlotView.Plot.Axes.Bottom.Label.Text = "Time";
			PlotView.Plot.Axes.Left.Label.Text = "Energy (Wh)";
			
			this.appSettings = appSettings;
			this.sqlServerService = sql;

			if (appSettings.StorageMode == StorageMode.CSVOnly || appSettings.StorageMode == StorageMode.Both)
				LoadCsv();
			if (appSettings.StorageMode == StorageMode.SQLiteOnly || appSettings.StorageMode == StorageMode.Both)
			{
				// Load from SQL Server
				var sqlPoints = sqlServerService.GetSnapshots();
				points.AddRange(sqlPoints); // In case of both, SQL data is appended to the CSV data
				points = points
					.OrderBy(p => p.UtcTimestamp) // Ensure that the new snapshots are added in the correct positions
					.ToList();
			}

			PopulateProcessList();
			MetricCombo.ItemsSource = metricSelectors.Keys.ToList();
			MetricCombo.SelectedIndex = 2;
			PlotData("All", "Energy (Wh)");
		}

		private void LoadCsv()
		{
			if (!File.Exists(csvPath))
			{
				MessageBox.Show("No snapshot data found.");
				return;
			}

			try
			{
				points = File.ReadAllLines(csvPath)
							  .Skip(1) // skip header
							  .Select(line =>
							  {
								  string[] p = line.Split(',');
								  if (p.Length < 8) return null;
								  return new ProcessSnapshot
								  {
									  UtcTimestamp = DateTime.Parse(p[0]),
									  Pid = int.Parse(p[1]),
									  ProcessName = p[2],
									  CpuPercent = double.Parse(p[3]),
									  WorkingSetBytes = long.Parse(p[4]),
									  EnergyWh = double.Parse(p[5]),
									  CO2Grams = double.Parse(p[6]),
									  CostUSD = double.Parse(p[7])
								  };
							  })
							  .Where(ps => ps != null)
							  .OrderBy(ps => ps.UtcTimestamp) // Ensure that the snapshots are sorted by timestamp so that binary search works correctly
															// Also important for plotting time series and keep everything in a chronological order.
							  .ToList();
				
				pointsByProcess = points
					.GroupBy(p => p.ProcessName)
					.ToDictionary(g => g.Key, g => g.ToList());
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error loading CSV: {ex.Message}");
			}
		}

		private void PopulateProcessList()
		{
			if (points == null || points.Count == 0) return;

			List<string> names = points.Where(p => p != null).Select(p => p.ProcessName)
				.Distinct()
				.OrderBy(n => n)
				.ToList();

			ProcessCombo.Items.Clear();
			ProcessCombo.Items.Add("All");
			foreach (string n in names)
				ProcessCombo.Items.Add(n);

			ProcessCombo.SelectedIndex = 0;
		}

		private void PlotData(string process, string metrics)
		{
			if ((StartDatePicker.SelectedDate != null && EndDatePicker.SelectedDate != null)
				&& StartDatePicker.SelectedDate.Value > EndDatePicker.SelectedDate.Value)
			{
				MessageBox.Show($"End date cannot be earlier than Start date.");
				StartDatePicker.SelectedDate = null;
				EndDatePicker.SelectedDate = null;

				return;
			}

			// Clear previous plot and setup titles/labels for the new plot
			PlotView.Plot.Clear();
			PlotView.Plot.Title($"{metrics} over time");
			PlotView.Plot.Axes.Left.Label.Text = metrics;
			if (points.Count == 0) return;

			// Determine date range
			if (StartDatePicker.SelectedDate == null) startDate = DateTime.MinValue;
			else startDate = StartDatePicker.SelectedDate.Value;
			if (EndDatePicker.SelectedDate == null) endDate = DateTime.MaxValue;
			else endDate = EndDatePicker.SelectedDate.Value;

			// Filter points by given conditions via binary search
			List<ProcessSnapshot> filteredPoints 
				= FilterPointsByRange(process, startDate, endDate);
			if (filteredPoints.Count == 0)
			{
				MessageBox.Show("No data available for the selected filters.");
				PlotView.Refresh();
				return;
			}

			// Plotting logic
			if (process == "All")
			{
				// Group by timestamp to sum energy of ALL processes at that specific time
				var groupedData = filteredPoints
					// .Where(p => p.UtcTimestamp >= startDate && p.UtcTimestamp <= endDate) // Already filtered; Bulky filtering
					.GroupBy(p => p.UtcTimestamp)
					.OrderBy(g => g.Key)
					.Select(g => new
					{
						Time = g.Key.ToOADate(),
						TotalMetrics = g.Sum(metricSelectors[metrics])
					})
					.ToList();

				if (groupedData.Count > 0)
				{
					double[] xs = groupedData.Select(d => d.Time).ToArray();
					double[] ys = groupedData.Select(d => d.TotalMetrics).ToArray();

					var scatter = PlotView.Plot.Add.Scatter(xs, ys); // Add Scatter Plot
					switch (metrics)
					{
						case "CPU (%)":
							scatter.LegendText = "Total CPU (%)";
							break;
						case "Memory (Bytes)":
							scatter.LegendText = "Total Memory (Bytes)";
							break;
						case "Energy (Wh)":
							scatter.LegendText = "Total Energy (Wh)";
							break;
						case "CO2 (g)":
							scatter.LegendText = "Total CO2 (g)";
							break;
						case "Cost (USD)":
							scatter.LegendText = "Total Cost (USD)";
							break;
					}
					scatter.LineWidth = 2;
					scatter.Color = ScottPlot.Colors.Blue;
				}
			}
			else
			{
				// Filter for specific process
				var data = filteredPoints
					// .Where(p => p.ProcessName == process) // Already filtered in FilterPointByRange
					// .Where(p => p.UtcTimestamp >= startDate && p.UtcTimestamp <= endDate) // Bulky filtering
					.GroupBy(p => p.UtcTimestamp)
					.OrderBy(g => g.Key)
					.Select(g => new
					{
						Time = g.Key.ToOADate(),
						Metric = g.Sum(metricSelectors[metrics])
					})
					.ToList();

				if (data.Count > 0)
				{
					double[] xs = data.Select(d => d.Time).ToArray();
					double[] ys = data.Select(d => d.Metric).ToArray();

					var scatter = PlotView.Plot.Add.Scatter(xs, ys); // Add Scatter Plot
					scatter.LegendText = process;
					scatter.LineWidth = 2;
				}
			}

			PlotView.Plot.Axes.DateTimeTicksBottom(); // Configure Axis to show Dates
			PlotView.Plot.ShowLegend();
			PlotView.Refresh();
		}

		///////////// Binary search: Efficiently filter points by process and date range
		private List<ProcessSnapshot> FilterPointsByRange(string process, DateTime startDate, DateTime endDate)
		{
			if (string.IsNullOrEmpty(process) || process == "All")
			{
				if (points == null || points.Count == 0) 
					return new List<ProcessSnapshot>();
				
				return GetPointsByProcess(points, startDate, endDate);
			}

			if (pointsByProcess != null && pointsByProcess.TryGetValue(process, out var processPoints))
			{
				 return GetPointsByProcess(processPoints, startDate, endDate);
			}

			return new List<ProcessSnapshot>(); // Process not found
		}

		// Saving from repeating code: Get points for a specific process within date range
		private List<ProcessSnapshot> GetPointsByProcess(List<ProcessSnapshot> snapshots, DateTime startDate, DateTime endDate)
		{
			int startIndex = LowerBoundIndex(snapshots, startDate);
			int endIndex = UpperBoundIndex(snapshots, endDate);
			if (startIndex > endIndex)
				return new List<ProcessSnapshot>();
			return snapshots.GetRange(startIndex, endIndex - startIndex + 1);
		}

		// Finding the lower bound index (first index with timestamp >= target)
		private int LowerBoundIndex(List<ProcessSnapshot> list, DateTime target)
		{
			int left = 0;
			int right = list.Count - 1;
			int result = list.Count;
			while (left <= right)
			{
				int mid = left + (right - left) / 2;
				if (list[mid].UtcTimestamp >= target)
				{
					result = mid;
					right = mid - 1;
				}
				else
					left = mid + 1;
			}
			return result;
		}

		// Finding the upper bound index (last index with timestamp <= target)
		private int UpperBoundIndex(List<ProcessSnapshot> list, DateTime target)
		{
			int left = 0;
			int right = list.Count - 1;
			int result = -1;
			while (left <= right)
			{
				int mid = left + (right - left) / 2;
				if (list[mid].UtcTimestamp <= target)
				{
					result = mid;
					left = mid + 1;
				}
				else
					right = mid - 1;
			}
			return result;
		}
		///////////// End of binary search methods


		private void Refresh_Click(object sender, RoutedEventArgs e)
		{
			LoadCsv();
			int processIndex = ProcessCombo.SelectedIndex;
			PopulateProcessList();
			ProcessCombo.SelectedIndex = processIndex >= 0 ? processIndex : 0;
			if (ProcessCombo.SelectedItem != null)
				PlotData(ProcessCombo.SelectedItem.ToString(), MetricCombo.SelectedItem.ToString());
		}

		private void ProcessCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (ProcessCombo.SelectedItem != null)
			{
				if (MetricCombo.SelectedItem == null) PlotData(ProcessCombo.SelectedItem.ToString(), "Energy (Wh)");
				else PlotData(ProcessCombo.SelectedItem.ToString(), MetricCombo.SelectedItem.ToString());
			}
		}

		private void MetricCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (MetricCombo.SelectedItem != null && ProcessCombo.SelectedItem != null)
				PlotData(ProcessCombo.SelectedItem.ToString(), MetricCombo.SelectedItem.ToString());
		}

		private void ExportButton_Click(object sender, RoutedEventArgs e)
		{
			List<ProcessSnapshot> exportedSnapshots = FilterPointsByRange(
				ProcessCombo.SelectedItem.ToString(), startDate, endDate);

			if (exportedSnapshots.Count == 0)
			{
				MessageBox.Show("No data available for the selected filters to export.");
				return;
			}
			
			ExportWindow exportWindow = new ExportWindow(appSettings, sqlServerService, exportedSnapshots);
			exportWindow.Show();
		}
	}
}