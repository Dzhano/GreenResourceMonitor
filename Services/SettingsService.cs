using GreenResourceMonitor.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace GreenResourceMonitor.Services
{
	internal class SettingsService
	{
		private readonly string settingsFilePath;

		public SettingsService()
		{
			string folder = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
				"GreenResourceMonitor");

			if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

			settingsFilePath = Path.Combine(folder, "settings.json");
		}

		public AppSettings Load()
		{
			if (!File.Exists(settingsFilePath)) return new AppSettings();

			string json = File.ReadAllText(settingsFilePath);
			return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
		}

		public void Save(AppSettings settings)
		{
			string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
			{
				WriteIndented = true
			});
			File.WriteAllText(settingsFilePath, json);
		}
	}
}
