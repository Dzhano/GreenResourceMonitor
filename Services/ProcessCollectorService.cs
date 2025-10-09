using GreenResourceMonitor.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GreenResourceMonitor.Services
{
	internal class ProcessCollectorService : IProcessCollector
	{
		private readonly TimeSpan interval;
		private readonly Dictionary<int, TimeSpan> lastCpuTimes = new Dictionary<int, TimeSpan>();
		private readonly int _logicalProcessors = Environment.ProcessorCount;
		private Task loop;
		private CancellationTokenSource cts;
		private readonly string csvPath;

		public event Action<IEnumerable<Models.ProcessSnapshot>> OnProcessSnapshot;

		public ProcessCollectorService(TimeSpan? interval = null, string csvPath = null)
		{
			this.interval = interval ?? TimeSpan.FromSeconds(1);
			this.csvPath = csvPath;
			if (!string.IsNullOrEmpty(this.csvPath))
			{
				var dir = Path.GetDirectoryName(this.csvPath);
				if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
				if (!File.Exists(this.csvPath)) File.AppendAllText(this.csvPath, "utc_timestamp,pid,process_name,cpu_percent,working_set_bytes\r\n", Encoding.UTF8);
			}
		}

		public async Task StartAsync(CancellationToken cancellationToken)
		{
			if (loop != null && loop.IsCompleted) return;
			cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			CancellationToken token = cts.Token;
			loop = Task.Run(async () =>
			{
				while (!token.IsCancellationRequested)
				{
					try
					{
						SampleAndEmit();
					}
					catch (Exception ex)
					{
						Console.WriteLine($"[ProcessCollectorService] Error: {ex}");
					}

					await Task.Delay(interval, token).ConfigureAwait(false);
				}
			}, token);

			await Task.CompletedTask;
		}
		public async Task StopAsync()
		{
			if (cts != null) return;
			cts.Cancel();
			try
			{
				if (loop != null) await loop;
			}
			catch (OperationCanceledException) { }
			loop = null;
			cts.Dispose();
			cts = null;
		}

		private void SampleAndEmit()
		{
			DateTime now = DateTime.UtcNow;
			var processes = System.Diagnostics.Process.GetProcesses();
			var result = new List<ProcessSnapshot>(processes.Length);

			foreach (var process in processes)
			{
				try
				{
					// Gather data for the current process
					// Note: Accessing some properties may throw exceptions (e.g., for system processes or if access is denied)
					var pID = process.Id;
					var pName = process.ProcessName;
					var cpuTime = process.TotalProcessorTime;

					var last = lastCpuTimes.TryGetValue(pID, out var previous) ? previous : TimeSpan.Zero; // Last recorded CPU time
					var deltaMs = (cpuTime - last).TotalMilliseconds; // CPU time used since last check
					var cpuPercent = (interval.TotalMilliseconds > 0) ? (deltaMs / interval.TotalMilliseconds) / _logicalProcessors * 100.0 : 0.0; // CPU usage percentage
					lastCpuTimes[pID] = cpuTime; // Update last recorded CPU time

					// Create snapshot for this process and add to result list
					var snapshot = new ProcessSnapshot
					{
						UtcTimestamp = now,
						ProcessId = pID,
						ProcessName = pName,
						CpuUsagePercent = Math.Round(cpuPercent, 3),
						WorkingSetBytes = process.WorkingSet64
					};
					result.Add(snapshot);

					if (!string.IsNullOrEmpty(csvPath))
					{
						var csvLine = string.Format(CultureInfo.InvariantCulture, $"{now:O},{pID},{pName},{snapshot.CpuUsagePercent},{snapshot.WorkingSetBytes}");
						System.IO.File.AppendAllLines(csvPath, new[] { csvLine }, Encoding.UTF8);
					}
				}
				catch { }
			}
			var active = processes.Select(p => p.Id).ToHashSet();
			var removed = lastCpuTimes.Keys.Where(id => !active.Contains(id)).ToList();
			foreach (var id in removed) lastCpuTimes.Remove(id);

			OnProcessSnapshot?.Invoke(result);
		}

		public void Dispose() => _ = StopAsync();
	}
}