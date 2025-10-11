using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GreenResourceMonitor.Models
{
	public sealed class ProcessSnapshot
	{
		// Process snapshot taken at this time

		// Process information
		public DateTime UtcTimestamp { get; set; }
		public int Pid { get; set; } // I had to change the name here so that it can matches binding "Pid"...
		public string ProcessName { get; set; }
		
		// CPU usage
		public double CpuPercent { get; set; } // I had to change the name here so that it can matches binding "CpuPercent"...
		public long WorkingSetBytes { get; set; }

		// Human-readable memory usage
		public string WorkingSetReadable => $"{WorkingSetBytes / 1024 / 1024} MB";

		// Energy and environmental impact estimates
		public double EnergyWh { get; set; }
		public double CO2Grams { get; set; }
		public double CostUSD { get; set; } // We are going to calculate all countries in USD to have a common ground for comparison.

	}
}
