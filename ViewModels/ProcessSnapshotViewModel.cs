using GreenResourceMonitor.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace GreenResourceMonitor.ViewModels
{
	public class ProcessSnapshotViewModel : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;
		private void Raise([CallerMemberName] string n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

		public ObservableCollection<ProcessSnapshot> Snapshots { get; } = new ObservableCollection<ProcessSnapshot>();

		private string status = "Stopped";
		public string Status
		{
			get => status;
			set
			{
				if (status != value)
				{
					status = value;
					Raise();
				}
			}
		}
	}
}
