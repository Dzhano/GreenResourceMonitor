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
	}
}
