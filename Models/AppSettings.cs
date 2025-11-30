using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GreenResourceMonitor.Models
{
	public class AppSettings
	{
		public double Co2PerWh { get; set; } = 0.475; // Default CO2 grams per Wh
		public double CostPerKWhEUR { get; set; } = 0.13; // Default cost per kWh in EUR
		public int SamplingSeconds { get; set; } = 1; // Default sampling interval in seconds
		public double CalibrationFactor { get; set; } = 1.0; // Default calibration factor to adjust energy estimates for accuracy

		public StorageMode StorageMode { get; set; } = StorageMode.CSVOnly; // Default storage mode
		public string SQLPath { get; set; } = ""; // Default SQL Server path if empty, SQL Server storage is disabled
	}

	public enum StorageMode
	{
		CSVOnly = 0,
		SQLOnly = 1,
		Both = 2
	}
}
