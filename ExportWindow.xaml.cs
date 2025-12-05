using GreenResourceMonitor.Models;
using System.Collections.Generic;
using System.Windows;
using Microsoft.Win32;

namespace GreenResourceMonitor
{
	/// <summary>
	/// Interaction logic for ExportWindow.xaml
	/// </summary>
	public partial class ExportWindow : Window
	{
		private readonly List<ProcessSnapshot> points;

		public ExportWindow(List<ProcessSnapshot> data)
		{
			InitializeComponent();
			points = data;
		}

		private void CSVButton_Click(object sender, RoutedEventArgs e)
		{
			var sfd = new SaveFileDialog { FileName = "snapshots.csv", Filter = "CSV files (*.csv)|*.csv" };
			if (sfd.ShowDialog() != true) return;
			using (var writer = new System.IO.StreamWriter(sfd.FileName))
			{
				// header
				writer.WriteLine("Timestamp,Pid,ProcessName,CpuPercent,WorkingSetBytes,EnergyWh,CO2Grams,CostEUR");
				foreach (var p in points)
				{
					writer.WriteLine($"{p.UtcTimestamp},{p.Pid},{p.ProcessName},{p.CpuPercent},{p.WorkingSetBytes},{p.EnergyWh},{p.CO2Grams},{p.CostEUR}");
				}
			}
			MessageBox.Show($"Exported {points.Count} rows to \n{sfd.FileName}");
		}

		private void ExcelButton_Click(object sender, RoutedEventArgs e)
		{
			var sfd = new SaveFileDialog { FileName = "snapshots.xlsx", Filter = "Excel files (*.xlsx)|*.xlsx" };
			if (sfd.ShowDialog() != true) return;

			using (var wb = new ClosedXML.Excel.XLWorkbook()){

				var ws = wb.Worksheets.Add("Snapshots");
				// header
				ws.Cell(1, 1).Value = "Timestamp";
				ws.Cell(1, 2).Value = "Pid";
				ws.Cell(1, 3).Value = "ProcessName";
				ws.Cell(1, 4).Value = "CpuPercent";
				ws.Cell(1, 5).Value = "WorkingSetBytes";
				ws.Cell(1, 6).Value = "EnergyWh";
				ws.Cell(1, 7).Value = "CO2Grams";
				ws.Cell(1, 8).Value = "CostEUR";
				ws.Row(1).Style.Font.Bold = true;
				int r = 2;
				foreach (var p in points)
				{
					ws.Cell(r, 1).Value = p.UtcTimestamp;
					ws.Cell(r, 2).Value = p.Pid;
					ws.Cell(r, 3).Value = p.ProcessName;
					ws.Cell(r, 4).Value = p.CpuPercent;
					ws.Cell(r, 5).Value = p.WorkingSetBytes;
					ws.Cell(r, 6).Value = p.EnergyWh;
					ws.Cell(r, 7).Value = p.CO2Grams;
					ws.Cell(r, 8).Value = p.CostEUR;
					r++;
				}
				ws.Columns().AdjustToContents();
				wb.SaveAs(sfd.FileName);
			}
			MessageBox.Show($"Exported {points.Count} rows to \n{sfd.FileName}");
		}

		private void JSONButton_Click(object sender, RoutedEventArgs e)
		{
			// Save data as JSON
			var sfd = new SaveFileDialog { FileName = "snapshots.json", Filter = "JSON files (*.json)|*.json" };
			if (sfd.ShowDialog() != true) return;
			var json = System.Text.Json.JsonSerializer.Serialize(points, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
			System.IO.File.WriteAllText(sfd.FileName, json);
			MessageBox.Show($"Exported {points.Count} rows to \n{sfd.FileName}");
		}
	}
}