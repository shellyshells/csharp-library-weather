namespace WeatherApp.Models
{
    public class WeatherDay
    {
        public string Date { get; set; } = "";
        public string Description { get; set; } = "";
        public string IconCode { get; set; } = "";
        public double Temperature { get; set; }
        public double TempMin { get; set; }
        public double TempMax { get; set; }
        public double FeelsLike { get; set; }
        public int Humidity { get; set; }
        public double Pressure { get; set; }
        public int CloudCover { get; set; }
        public double WindSpeed { get; set; }
        public double Visibility { get; set; }
        public int PrecipitationProbability { get; set; }
        public string Sunrise { get; set; } = "";
        public string Sunset { get; set; } = "";
    }

    public class CityInfo
    {
        public string Name    { get; set; } = "";
        public string Country { get; set; } = "";
        public double Latitude  { get; set; }
        public double Longitude { get; set; }
    }

    public class CitySearchResult
    {
        public string Name    { get; set; } = "";
        public string Country { get; set; } = "";
        public string Region  { get; set; } = "";
        public double Latitude  { get; set; }
        public double Longitude { get; set; }

        public override string ToString()
        {
            string label = Name;
            if (!string.IsNullOrEmpty(Region) && Region != Name)
                label += $", {Region}";
            if (!string.IsNullOrEmpty(Country))
                label += $" ({Country})";
            return label;
        }
    }

    public class FavoriteCity
    {
        public string Name      { get; set; } = "";
        public string Country   { get; set; } = "";
        public double Latitude  { get; set; }
        public double Longitude { get; set; }
        public override string ToString() => $"{Name} ({Country})";
    }
}
