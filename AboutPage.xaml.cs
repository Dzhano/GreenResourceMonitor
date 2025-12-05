using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

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
				MessageBox.Show("Could not open browser: " + ex.Message, "Browser unavailable", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void GoBackButton_Click(object sender, RoutedEventArgs e)
		{
			// Trigger the event to tell MainWindow to hide this page
			CloseRequested?.Invoke(this, EventArgs.Empty);
		}
	}
}