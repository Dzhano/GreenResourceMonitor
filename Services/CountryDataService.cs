using GreenResourceMonitor.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Diagnostics;
using Newtonsoft.Json.Linq;

namespace GreenResourceMonitor.Services
{
	internal class CountryDataService
	{
		private readonly HttpClient http;
		private readonly string cachePath;

		private double GetFallbackPriceData(string countryCode)
		{
			// In case of failure, use some hardcoded fallback values
			// Fallback price data in EUR per MWh. Because of that, I change the structure of the program to be for EUR instead of USD.
			Dictionary<string, double> fallbackPrices = new Dictionary<string, double>
			{
				{ "BG", 130 }, // Bulgaria
				{ "US", 130 }, // USA
				{ "DE", 300 }, // Germany
				{ "FR", 200 }, // France
				{ "GB", 250 }, // UK
				{ "IT", 280 }, // Italy
				{ "ES", 220 }, // Spain
				{ "CN", 100 }, // China
				{ "JP", 180 }  // Japan
			};
			if (fallbackPrices.ContainsKey(countryCode))
				return fallbackPrices[countryCode];
			else return 130; // Default fallback value (for Bulgaria)
		}

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
			double cost = await FetchPriceDataAsync(countryCode);

			CountryData newData = new CountryData
			{
				Country = countryName,
				CountryCode = countryCode,
				Co2PerKWh = co2,
				CostPerKWhEUR = cost
			};
			
			return newData;
		}

		private async Task<double> FetchPriceDataAsync(string countryCode)
		{
			// Using Electricity Maps API to get the latest price day-ahead
			/* API Documentation: https://app.electricitymaps.com/developer-hub/api/signals#price-day-ahead
								https://app.electricitymaps.com/developer-hub/api/reference#price-day-ahead-latest	*/
			/* Developer hub: https://app.electricitymaps.com/developer-hub/playground?datatype=price-day-ahead&temporality=latest
				where you can test and find information about the different regions. */
			/* Note: Currently, the Electricity Maps API does not provide real-time price data for all countries.
			  Thus, that's why we I didn't implemented it at the same time with the CO2 data fetch.
			  Still, I want to keep the structure ready for future improvements.
			  Also, I can demonstrate the usage of the fallback local database of prices per country. Thus, not needing to rely solely on the API. And not to change the countries ;) */
			string apiUrl = $"https://api.electricitymaps.com/v3/price-day-ahead/latest?zone={countryCode.ToUpper()}";
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
						return GetFallbackPriceData(countryCode);
					}
					string json = await response.Content.ReadAsStringAsync();
					var root = JObject.Parse(json);

					var val = root["value"];
					if (val != null) return val.Value<double>();
					else
					{
						Debug.WriteLine("value (Price Day-Ahead) was not found in response");
						return GetFallbackPriceData(countryCode);
					}
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"API Exception for Price Day-Ahead in {countryCode}: {ex.Message}");
			}

			return GetFallbackPriceData(countryCode); // In case of failure, return fallback value
		}

		private async Task<double> FetchCo2DataAsync(string countryCode)
		{
			// Using Electricity Maps API to get real-time CO2 intensity data
			/* API Documentation: https://app.electricitymaps.com/developer-hub/api/signals#carbon-intensity
								https://app.electricitymaps.com/developer-hub/api/reference#carbon-intensity-latest */
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
						Debug.WriteLine("carbonIntensity was not found in response");
						return GetFallbackCO2Data(countryCode);
					}
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"API Exception for Carbon Intensity in {countryCode}: {ex.Message}");
			}

			return GetFallbackCO2Data(countryCode); // In case of failure, return fallback value
		}
	}
}