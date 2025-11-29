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
		private readonly AppSettings appSettings;

		private readonly SqlServerService sqlService;
		private readonly List<ProcessSnapshot> sqlBuffer = new List<ProcessSnapshot>();
		private readonly int batchSize = 50; // Number of snapshots to insert per batch

		public event Action<IEnumerable<ProcessSnapshot>> OnProcessSnapshot;

		public ProcessCollectorService(TimeSpan? interval = null, string csvPath = null,
			AppSettings settings = null, SqlServerService sql = null)
		{
			this.interval = interval ?? TimeSpan.FromSeconds(1);
			this.csvPath = csvPath;
			appSettings = settings ?? new AppSettings();
			sqlService = sql ?? new SqlServerService(appSettings.SQLPath);

			if (!string.IsNullOrEmpty(this.csvPath))
			{
				var dir = Path.GetDirectoryName(this.csvPath);
				if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
				if (!File.Exists(this.csvPath)) 
					File.AppendAllText(this.csvPath, "utc_timestamp,pid,Process_Name,CPU_percent," +
						"Working_set_bytes,Energy_Wh,CO2_Grams,Cost_EUR\r\n", Encoding.UTF8);
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

			// Flush any remaining SQL buffer
			if (appSettings.StorageMode == StorageMode.SQLiteOnly || appSettings.StorageMode == StorageMode.Both)
			{
				lock (sqlBuffer)
				{
					if (sqlBuffer.Count > 0)
					{
						var toInsert = new List<ProcessSnapshot>(sqlBuffer);
						sqlBuffer.Clear();
						try
						{
							foreach (var snapshot in toInsert)
							{
								sqlService.InsertSnapshot(snapshot);
							}
						}
						catch (Exception ex)
						{
							Debug.WriteLine("SQL final insert error: " + ex.Message);
						}
					}
				}
			}
		}

		private void SampleAndEmit()
		{
			DateTime now = DateTime.UtcNow;
			var processes = Process.GetProcesses();
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

					const double cpuTDPWatts = 15.0; // Average TDP for a CPU core in Watts (assumed)
					double intervalSeconds = interval.TotalSeconds; // Actual interval in seconds
					double co2PerWh = appSettings.Co2PerWh; // Average CO2 emissions per Wh in grams in Bulgaria = 0.475 kg or 475 grams
					double costPerKWhEUR_BG = appSettings.CostPerKWhEUR; // Average cost of electricity per kWh in Bulgaria = 0.13 EUR or 13 cents
					double energyWh = (cpuPercent / 100.0) * cpuTDPWatts * (intervalSeconds / 3600.0); // Energy in Watt-hours
					double calibrationFactor = appSettings.CalibrationFactor; // Calibration factor to adjust energy estimates

					// Create snapshot for this process and add to result list
					var snapshot = new ProcessSnapshot
					{
						UtcTimestamp = now,
						Pid = pID,
						ProcessName = pName,
						CpuPercent = Math.Round(cpuPercent, 3),
						WorkingSetBytes = process.WorkingSet64,
						EnergyWh = Math.Round(energyWh, 6), // Energy in Watt-hours
						CO2Grams = Math.Round(energyWh * co2PerWh, 6), // CO2 emissions in grams
						CostEUR = Math.Round(energyWh * (costPerKWhEUR_BG / 1000.0) * calibrationFactor, 12) // Cost in EUR
					};
					result.Add(snapshot);

					if (appSettings.StorageMode == StorageMode.CSVOnly || appSettings.StorageMode == StorageMode.Both)
					{
						if (!string.IsNullOrEmpty(csvPath))
						{
							string csvLine = string.Format(CultureInfo.InvariantCulture, $"{now:O},{pID},{pName},{snapshot.CpuPercent},{snapshot.WorkingSetBytes}, {snapshot.EnergyWh}, {snapshot.CO2Grams}, {snapshot.CostEUR}");
							File.AppendAllLines(csvPath, new[] { csvLine }, Encoding.UTF8);
						}
					}
					if (appSettings.StorageMode == StorageMode.SQLiteOnly || appSettings.StorageMode == StorageMode.Both)
					{
						lock (sqlBuffer)
						{
							sqlBuffer.Add(snapshot);
							if (sqlBuffer.Count >= batchSize)
							{
								// copy and clear buffer quickly, then insert in background
								var toInsert = new List<ProcessSnapshot>(sqlBuffer);
								sqlBuffer.Clear();
								// fire-and-forget background insert (don't block UI/timer)
								_ = Task.Run(() =>
								{
									try
									{
										sqlService.InsertSnapshot(snapshot);
									}
									catch (Exception ex)
									{
										// log but don't crash
										Debug.WriteLine("SQL insert batch error: " + ex.Message);
									}
								});
							}
						}
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