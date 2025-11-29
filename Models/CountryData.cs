using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GreenResourceMonitor.Models
{
	public class CountryData
	{
		public string Country { get; set; }
		public string CountryCode { get; set; }
		public double Co2PerKWh { get; set; }
		public double CostPerKWhEUR { get; set; }
	}
}
