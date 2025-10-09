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
		public int ProcessId { get; set; }
		public string ProcessName { get; set; }
		public double CpuUsagePercent { get; set; }
		public long WorkingSetBytes { get; set; }

		public string WorkingSetReadable => $"{WorkingSetBytes / 1024 / 1024} MB";
	}
}
