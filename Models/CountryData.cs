using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GreenResourceMonitor.Models
{
	public class CountryData
	{
		public DateTime UtcTimestamp { get; set; }
		public string Country { get; set; }
		public string CountryCode { get; set; }
		public double Co2PerKWh { get; set; }
		public double CostPerKWhUSD { get; set; }
		public string Source { get; set; }
	}
}
