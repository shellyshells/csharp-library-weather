using WeatherApp.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace WeatherApp.Services
{
    public class WeatherService
    {
        private static readonly string API_KEY =
            Environment.GetEnvironmentVariable("API_KEY") ?? "";
        private const string OWM_BASE = "https://api.openweathermap.org/data/2.5";
        private static readonly HttpClient _http = new HttpClient();

        public async Task<(CityInfo city, List<WeatherDay> forecasts)> GetForecast(string cityQuery)
        {
            EnsureApiKey();
            string url = $"{OWM_BASE}/forecast?q={Uri.EscapeDataString(cityQuery)}&appid={API_KEY}&units=metric&lang=en&cnt=40";
            return await FetchForecastData(url);
        }

        public async Task<(CityInfo city, List<WeatherDay> forecasts)> GetForecastByCoords(double lat, double lon)
        {
            EnsureApiKey();
            string latS = lat.ToString(System.Globalization.CultureInfo.InvariantCulture);
            string lonS = lon.ToString(System.Globalization.CultureInfo.InvariantCulture);
            string url = $"{OWM_BASE}/forecast?lat={latS}&lon={lonS}&appid={API_KEY}&units=metric&lang=en&cnt=40";
            return await FetchForecastData(url);
        }

        public async Task<List<CitySearchResult>> GetNearbyCities(double lat, double lon)
        {
            if (string.IsNullOrEmpty(API_KEY)) return new();
            try
            {
                string latS = lat.ToString(System.Globalization.CultureInfo.InvariantCulture);
                string lonS = lon.ToString(System.Globalization.CultureInfo.InvariantCulture);
                string url = $"{OWM_BASE}/find?lat={latS}&lon={lonS}&cnt=15&appid={API_KEY}";
                string json = await _http.GetStringAsync(url);
                using var doc = JsonDocument.Parse(json);
                var results = new List<CitySearchResult>();
                if (doc.RootElement.TryGetProperty("list", out var list))
                    foreach (var item in list.EnumerateArray())
                    {
                        var coord = item.GetProperty("coord");
                        results.Add(new CitySearchResult
                        {
                            Name = item.GetProperty("name").GetString() ?? "",
                            Country = item.TryGetProperty("sys", out var sys) && sys.TryGetProperty("country", out var c) ? c.GetString() ?? "" : "",
                            Latitude  = coord.GetProperty("lat").GetDouble(),
                            Longitude = coord.GetProperty("lon").GetDouble()
                        });
                    }
                return results;
            }
            catch { return new(); }
        }

        public async Task<List<CitySearchResult>> SearchCities(string query)
        {
            try
            {
                string url = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(query)}&count=50&language=en&format=json";
                string json = await _http.GetStringAsync(url);
                using var doc = JsonDocument.Parse(json);
                var results = new List<CitySearchResult>();
                if (!doc.RootElement.TryGetProperty("results", out var el)) return results;
                foreach (var item in el.EnumerateArray())
                    results.Add(new CitySearchResult
                    {
                        Name    = item.GetProperty("name").GetString() ?? "",
                        Country = item.TryGetProperty("country", out var co)  ? co.GetString()  ?? "" : "",
                        Region  = item.TryGetProperty("admin1",  out var adm) ? adm.GetString() ?? "" : "",
                        Latitude  = item.GetProperty("latitude").GetDouble(),
                        Longitude = item.GetProperty("longitude").GetDouble()
                    });
                return results;
            }
            catch { return new(); }
        }

        public static string GetIconUrl(string iconCode)
            => $"https://openweathermap.org/img/wn/{iconCode}@2x.png";

        private async Task<(CityInfo city, List<WeatherDay> forecasts)> FetchForecastData(string url)
        {
            string json = await _http.GetStringAsync(url);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var cityEl = root.GetProperty("city");
            var city = new CityInfo
            {
                Name    = cityEl.GetProperty("name").GetString()    ?? "",
                Country = cityEl.GetProperty("country").GetString() ?? "",
                Latitude  = cityEl.GetProperty("coord").GetProperty("lat").GetDouble(),
                Longitude = cityEl.GetProperty("coord").GetProperty("lon").GetDouble()
            };
            long sunriseTs = cityEl.GetProperty("sunrise").GetInt64();
            long sunsetTs  = cityEl.GetProperty("sunset").GetInt64();
            string sunrise = DateTimeOffset.FromUnixTimeSeconds(sunriseTs).ToLocalTime().ToString("HH:mm");
            string sunset  = DateTimeOffset.FromUnixTimeSeconds(sunsetTs).ToLocalTime().ToString("HH:mm");

            var seenDates = new HashSet<string>();
            var forecasts = new List<WeatherDay>();
            foreach (var item in root.GetProperty("list").EnumerateArray())
            {
                string dtTxt = item.GetProperty("dt_txt").GetString() ?? "";
                string date  = dtTxt[..10];
                string time  = dtTxt[11..16];
                if (seenDates.Contains(date) && time != "12:00") continue;
                if (seenDates.Contains(date)) continue;
                seenDates.Add(date);
                var main    = item.GetProperty("main");
                var weather = item.GetProperty("weather")[0];
                var wind    = item.GetProperty("wind");
                var clouds  = item.GetProperty("clouds");
                double visKm = 10.0;
                if (item.TryGetProperty("visibility", out var vis)) visKm = Math.Round(vis.GetDouble() / 1000.0, 1);
                int precipPct = 0;
                if (item.TryGetProperty("pop", out var pop)) precipPct = (int)(pop.GetDouble() * 100);
                forecasts.Add(new WeatherDay
                {
                    Date        = date,
                    Description = Capitalize(weather.GetProperty("description").GetString() ?? ""),
                    IconCode    = weather.GetProperty("icon").GetString() ?? "01d",
                    Temperature  = Math.Round(main.GetProperty("temp").GetDouble(),      1),
                    TempMin      = Math.Round(main.GetProperty("temp_min").GetDouble(),   1),
                    TempMax      = Math.Round(main.GetProperty("temp_max").GetDouble(),   1),
                    FeelsLike    = Math.Round(main.GetProperty("feels_like").GetDouble(), 1),
                    Humidity     = main.GetProperty("humidity").GetInt32(),
                    Pressure     = main.GetProperty("pressure").GetDouble(),
                    CloudCover   = clouds.GetProperty("all").GetInt32(),
                    WindSpeed    = Math.Round(wind.GetProperty("speed").GetDouble() * 3.6, 1),
                    Visibility   = visKm,
                    PrecipitationProbability = precipPct,
                    Sunrise = sunrise, Sunset = sunset
                });
                if (forecasts.Count >= 5) break;
            }
            return (city, forecasts);
        }

        private static void EnsureApiKey()
        {
            if (string.IsNullOrEmpty(API_KEY))
                throw new InvalidOperationException(
                    "API key missing.\n\nCreate a .env file next to the executable with:\nAPI_KEY=your_openweathermap_key\n\nGet a free key at: https://openweathermap.org/api");
        }

        private static string Capitalize(string s)
            => string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s[1..];
    }
}
