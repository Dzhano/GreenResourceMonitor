using GreenResourceMonitor.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Newtonsoft.Json.Linq;

namespace GreenResourceMonitor.Services
{
	internal class CountryDataService
	{
		private readonly HttpClient http;
		private readonly string cachePath;

		private readonly Dictionary<string, double> fallbackPrices = new Dictionary<string, double>
		{
			{ "BG", 0.15 }, // Bulgaria
            { "US", 0.17 }, // USA
            { "DE", 0.40 }, // Germany
            { "FR", 0.22 }, // France
            { "GB", 0.34 }, // UK
            { "IT", 0.30 }, // Italy
            { "ES", 0.24 }, // Spain
            { "CN", 0.08 }, // China
            { "JP", 0.26 }  // Japan
        };

		private double GetFallbackCO2Data(string countryCode)
		{
			// In case of failure, use some hardcoded fallback values
			// Fallback CO2 data in grams per kWh
			Dictionary<string, double> fallbackData = new Dictionary<string, double>
			{
				{ "BG", 400 }, // Bulgaria
				{ "US", 450 }, // USA
				{ "DE", 500 }, // Germany
				{ "FR", 60 },  // France
				{ "GB", 200 }, // UK
				{ "IT", 300 }, // Italy
				{ "ES", 250 }, // Spain
				{ "CN", 700 }, // China
				{ "JP", 500 }  // Japan
			};
			if (fallbackData.ContainsKey(countryCode))
				return fallbackData[countryCode];
			else return 400; // Default fallback value (for Bulgaria)
		}

		public CountryDataService(HttpClient http = null)
		{
			this.http = http ?? new HttpClient();
			this.cachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
				"GreenResourceMonitor",
				"country_cache.json");

			string directory = Path.GetDirectoryName(this.cachePath);
			if (!Directory.Exists(directory))
				Directory.CreateDirectory(directory);
		}

		public async Task<CountryData> GetCountryDataAsync(string countryCode, string countryName)
		{
			double co2 = await FetchCo2DataAsync(countryCode);

			//// Further work
			double cost = fallbackPrices.ContainsKey(countryCode) ? fallbackPrices[countryCode] : 0.15; // Default price for Bulgaria if not found
			////

			CountryData newData = new CountryData
			{
				UtcTimestamp = DateTime.UtcNow,
				Country = countryName,
				CountryCode = countryCode,
				Co2PerKWh = co2,
				CostPerKWhUSD = cost,
				Source = "ElectricityMap API + Local Price DB"
			};
			
			return newData;
		}

		private async Task<double> FetchCo2DataAsync(string countryCode)
		{
			// Using Electricity Maps API to get real-time CO2 intensity data
			// API Documentation: https://app.electricitymaps.com/developer-hub/api/getting-started
			/* Developer hub: https://app.electricitymaps.com/developer-hub/playground?datatype=carbon-intensity&temporality=latest
				where you can test and find information about the different regions. */
			string apiUrl = $"https://api.electricitymaps.com/v3/carbon-intensity/latest?zone={countryCode.ToUpper()}";
			string apiKey = "BvXgS2s129tKn87TwpfR"; // The API key I have requested from Electricity Maps

			try
			{
				using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, apiUrl))
				{
					request.Headers.Add("auth-token", apiKey);

					HttpResponseMessage response = await http.SendAsync(request);
					if (!response.IsSuccessStatusCode)
					{
						Debug.WriteLine($"ElectricityMap request failed with status code: {response.StatusCode}");
						return GetFallbackCO2Data(countryCode);
					}
					string json = await response.Content.ReadAsStringAsync();
					var root = JObject.Parse(json);

					var val = root["carbonIntensity"];
					if (val != null) return val.Value<double>();
					else
					{
						Debug.WriteLine("carbonIntensity not found in response");
						return GetFallbackCO2Data(countryCode);
					}
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"API Exception: {ex.Message}");
			}

			return GetFallbackCO2Data(countryCode);
		}
	}
}
