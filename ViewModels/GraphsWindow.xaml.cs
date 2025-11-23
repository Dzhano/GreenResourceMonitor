using GreenResourceMonitor.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace GreenResourceMonitor.ViewModels
{
	/// <summary>
	/// Interaction logic for GraphsWindow.xaml
	/// </summary>
	public partial class GraphsWindow : Window
	{
		private readonly string csvPath;
		private List<ProcessSnapshot> points = new List<ProcessSnapshot>();

		public GraphsWindow(string csvPath)
		{
			InitializeComponent();
			this.csvPath = csvPath;

			PlotView.Plot.Title("Energy Consumption Over Time");
			PlotView.Plot.Axes.Bottom.Label.Text = "Time";
			PlotView.Plot.Axes.Left.Label.Text = "Energy (Wh)";

			LoadCsv();
			PopulateProcessList();
			PlotData("All");
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
			foreach (var n in names)
				ProcessCombo.Items.Add(n);

			ProcessCombo.SelectedIndex = 0;
		}

		private void PlotData(string process)
		{
			PlotView.Plot.Clear();

			if (points.Count == 0) return;

			if (process == "All")
			{
				// Group by timestamp to sum energy of ALL processes at that specific time
				var groupedData = points
					.GroupBy(p => p.UtcTimestamp)
					.OrderBy(g => g.Key)
					.Select(g => new {
						Time = g.Key.ToOADate(),
						TotalEnergy = g.Sum(p => p.EnergyWh)
					})
					.ToList();

				if (groupedData.Count > 0)
				{
					double[] xs = groupedData.Select(d => d.Time).ToArray();
					double[] ys = groupedData.Select(d => d.TotalEnergy).ToArray();

					var scatter = PlotView.Plot.Add.Scatter(xs, ys); // Add Scatter Plot
					scatter.LegendText = "Total Energy (Wh)";
					scatter.LineWidth = 2;
					scatter.Color = ScottPlot.Colors.Blue;
				}
			}
			else
			{
				// Filter for specific process
				var data = points
					.Where(p => p.ProcessName == process)
					.GroupBy(p => p.UtcTimestamp)
					.OrderBy(g => g.Key)
					.Select(g => new {
						Time = g.Key.ToOADate(),
						Energy = g.Sum(x => x.EnergyWh)
					})
					.ToList();

				if (data.Count > 0)
				{
					double[] xs = data.Select(d => d.Time).ToArray();
					double[] ys = data.Select(d => d.Energy).ToArray();

					var scatter = PlotView.Plot.Add.Scatter(xs, ys); // Add Scatter Plot
					scatter.LegendText = process;
					scatter.LineWidth = 2;
				}
			}

			PlotView.Plot.Axes.DateTimeTicksBottom(); // Configure Axis to show Dates
			PlotView.Plot.ShowLegend();
			PlotView.Refresh();
		}

		private void Refresh_Click(object sender, RoutedEventArgs e)
		{
			LoadCsv();
			PopulateProcessList();
			if (ProcessCombo.SelectedItem != null)
				PlotData(ProcessCombo.SelectedItem.ToString());
		}

		private void ProcessCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		{
			if (ProcessCombo.SelectedItem != null)
				PlotData(ProcessCombo.SelectedItem.ToString());
		}
	}
}