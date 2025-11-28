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

		private DateTime startDate, endDate;
		private string programName;

		private readonly AppSettings appSettings;
		private readonly SQLiteService sqliteService;

		private readonly Dictionary<string, Func<ProcessSnapshot, double>> metricSelectors =
			new Dictionary<string, Func<ProcessSnapshot, double>>()
		{
			{"CPU (%)", p => p.CpuPercent },
			{"Memory (Bytes)", p => (double)p.WorkingSetBytes }, // Convert long to double for plotting
            {"Energy (Wh)", p => p.EnergyWh },
			{"CO2 (g)", p => p.CO2Grams },
			{"Cost (USD)", p => p.CostUSD }
		};

		public GraphsWindow(string csvPath, AppSettings appSettings, SQLiteService sql)
		{
			InitializeComponent();
			this.csvPath = csvPath;

			PlotView.Plot.Title("Energy (Wh) over time");
			PlotView.Plot.Axes.Bottom.Label.Text = "Time";
			PlotView.Plot.Axes.Left.Label.Text = "Energy (Wh)";
			
			this.appSettings = appSettings;
			this.sqliteService = sql;

			if (appSettings.StorageMode == StorageMode.CSVOnly || appSettings.StorageMode == StorageMode.Both)
				LoadCsv();
			if (appSettings.StorageMode == StorageMode.SQLiteOnly || appSettings.StorageMode == StorageMode.Both)
			{
				// var series = sql.GetProcessEnergySeries().ToList(); // Soon to be added
			}

			PopulateProcessList();
			MetricCombo.ItemsSource = metricSelectors.Keys.ToList();
			MetricCombo.SelectedIndex = 2;
			PlotData(programName, "Energy (Wh)");
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
							  .ToList();
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

			PlotView.Plot.Clear();
			PlotView.Plot.Title($"{metrics} over time");
			PlotView.Plot.Axes.Left.Label.Text = metrics;
			if (points.Count == 0) return;

			if (StartDatePicker.SelectedDate == null) startDate = DateTime.MinValue;
			else startDate = StartDatePicker.SelectedDate.Value;
			if (EndDatePicker.SelectedDate == null) endDate = DateTime.MaxValue;
			else endDate = EndDatePicker.SelectedDate.Value;

			if (process == "All")
			{
				// Group by timestamp to sum energy of ALL processes at that specific time
				var groupedData = points
					.Where(p => p.UtcTimestamp >= startDate && p.UtcTimestamp <= endDate)
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
				var data = points
					.Where(p => p.ProcessName == process)
					.Where(p => p.UtcTimestamp >= startDate && p.UtcTimestamp <= endDate)
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

			programName = process;
		}

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
			if (MetricCombo.SelectedItem != null)
				PlotData(ProcessCombo.SelectedItem.ToString(), MetricCombo.SelectedItem.ToString());
		}

		private void ExportButton_Click(object sender, RoutedEventArgs e)
		{
			List<ProcessSnapshot> exportedSnapshots = points
					.Where(p => p.UtcTimestamp >= startDate && p.UtcTimestamp <= endDate)
					.ToList();

			if (ProcessCombo.SelectedItem.ToString() != "All")
			{
				exportedSnapshots = exportedSnapshots
					.Where(p => p.ProcessName == ProcessCombo.SelectedItem.ToString())
					.ToList();
			}
			if (exportedSnapshots.Count == 0)
			{
				MessageBox.Show("No data available for the selected filters to export.");
				return;
			}
			/*if (MetricCombo.SelectedItem.ToString() != "All")
			{
				exportedSnapshots = exportedSnapshots
					.Select(p => new ProcessSnapshot
					{
						UtcTimestamp = p.UtcTimestamp,
						Pid = p.Pid,
						ProcessName = p.ProcessName,
						CpuPercent = MetricCombo.SelectedItem.ToString() == "CPU (%)" ? p.CpuPercent : 0,
						WorkingSetBytes = MetricCombo.SelectedItem.ToString() == "Memory (Bytes)" ? p.WorkingSetBytes : 0,
						EnergyWh = MetricCombo.SelectedItem.ToString() == "Energy (Wh)" ? p.EnergyWh : 0,
						CO2Grams = MetricCombo.SelectedItem.ToString() == "CO2 (g)" ? p.CO2Grams : 0,
						CostUSD = MetricCombo.SelectedItem.ToString() == "Cost (USD)" ? p.CostUSD : 0
					})
					.ToList();
			}*/ // Potential usage in the future

			ExportWindow exportWindow = new ExportWindow(appSettings, sqliteService, exportedSnapshots);
			exportWindow.Show();
		}
	}
}