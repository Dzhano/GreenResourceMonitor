using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GreenResourceMonitor
{
	/// <summary>
	/// Interaction logic for AboutPage.xaml
	/// </summary>
	public partial class AboutPage : Page
	{
		public event EventHandler CloseRequested;

		public AboutPage()
		{
			InitializeComponent();
		}

		private void LinkedInButton_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				// Opens the default browser
				Process.Start(new ProcessStartInfo
				{
					FileName = "https://www.linkedin.com/in/dzhano-mihaylov/",
					UseShellExecute = true
				});
			}
			catch (Exception ex)
			{
				MessageBox.Show("Could not open browser: " + ex.Message);
			}
		}

		private void GoBackButton_Click(object sender, RoutedEventArgs e)
		{
			// Trigger the event to tell MainWindow to hide this page
			CloseRequested?.Invoke(this, EventArgs.Empty);
		}
	}
}
