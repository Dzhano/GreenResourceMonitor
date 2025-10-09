using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GreenResourceMonitor.Services
{
	public interface IProcessCollector : IDisposable
	{
		Task StartAsync(CancellationToken cancellationToken);
		Task StopAsync();

		event Action<IEnumerable<Models.ProcessSnapshot>> OnProcessSnapshot;
	}
}
