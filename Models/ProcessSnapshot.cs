using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GreenResourceMonitor.Models
{
	public sealed class ProcessSnapshot
	{
		public DateTime UtcTimestamp { get; set; }
		public int Pid { get; set; } // I had to change the name here so that it can matches binding "Pid"...
		public string ProcessName { get; set; }
		public double CpuPercent { get; set; } // I had to change the name here so that it can matches binding "CpuPercent"...
		public long WorkingSetBytes { get; set; }

		public string WorkingSetReadable => $"{WorkingSetBytes / 1024 / 1024} MB";
	}
}
