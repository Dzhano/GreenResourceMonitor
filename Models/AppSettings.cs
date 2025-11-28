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
		public double CostPerKWhUSD { get; set; } = 0.15; // Default cost per kWh in USD
		public int SamplingSeconds { get; set; } = 1; // Default sampling interval in seconds
		public double CalibrationFactor { get; set; } = 1.0; // Default calibration factor to adjust energy estimates for accuracy

		public StorageMode StorageMode { get; set; } = StorageMode.CSVOnly; // Default storage mode
		public string SQLitePath { get; set; } = ""; // Default SQLite database path if empty, SQLite storage is disabled
	}

	public enum StorageMode
	{
		CSVOnly = 0,
		SQLiteOnly = 1,
		Both = 2
	}
}
