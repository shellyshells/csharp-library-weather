using WeatherApp.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace WeatherApp.Services
{
    public class FavoritesService
    {
        private readonly string _filePath;
        private List<FavoriteCity> _favorites = new();

        public FavoritesService(string? filePath = null)
        {
            _filePath = filePath
                ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "favorites.json");
            LoadFromDisk();
        }

        public IReadOnlyList<FavoriteCity> GetAll() => _favorites.AsReadOnly();

        public bool Contains(string cityName)
            => _favorites.Exists(f =>
                string.Equals(f.Name, cityName, StringComparison.OrdinalIgnoreCase));

        public bool Add(string name, string country, double lat = 0, double lon = 0)
        {
            if (Contains(name)) return false;
            _favorites.Add(new FavoriteCity { Name = name, Country = country, Latitude = lat, Longitude = lon });
            SaveToDisk();
            return true;
        }

        public void Remove(string cityName)
        {
            int removed = _favorites.RemoveAll(f =>
                string.Equals(f.Name, cityName, StringComparison.OrdinalIgnoreCase));
            if (removed > 0) SaveToDisk();
        }

        private void LoadFromDisk()
        {
            try
            {
                if (!File.Exists(_filePath)) { _favorites = new(); return; }
                string json = File.ReadAllText(_filePath);
                _favorites = JsonSerializer.Deserialize<List<FavoriteCity>>(json) ?? new();
            }
            catch { _favorites = new(); }
        }

        private void SaveToDisk()
        {
            try
            {
                string json = JsonSerializer.Serialize(_favorites, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_filePath, json);
            }
            catch { }
        }
    }
}
