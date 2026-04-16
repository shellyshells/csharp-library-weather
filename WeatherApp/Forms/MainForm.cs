// ============================================================
// WeatherPro — Main Form (Fields, Constructor, Math Helpers)
// ============================================================
// Partial class split:
//   MainForm.cs           ← Fields, constructor, helpers, math
//   MainForm.UI.cs        ← BuildUI() — all control creation
//   MainForm.Rendering.cs ← GDI+ painting (globe, cards, charts)
//   MainForm.Actions.cs   ← Event handlers & async operations
// ============================================================

using WeatherApp.Models;
using WeatherApp.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Net.Http;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace WeatherApp.Forms
{
    /// <summary>Double-buffered Panel to prevent flicker during custom painting.</summary>
    public class BufferedPanel : Panel
    {
        public BufferedPanel() { DoubleBuffered = true; }
    }

    public partial class MainForm : Form
    {
        // ── Services ─────────────────────────────────────────────────
        private readonly WeatherService   _weatherService   = new WeatherService();
        private readonly FavoritesService _favoritesService = new FavoritesService();

        // ── Application State ─────────────────────────────────────────
        private CityInfo?          _currentCity;
        private List<WeatherDay>?  _currentForecasts;
        private WeatherDay?        _selectedDay;
        private List<FavoriteCity> _favorites;
        private List<string>       _searchHistory = new();

        // ── Theme ─────────────────────────────────────────────────────
        private bool _darkMode = false; // Match LibraryApp default (light)

        // ── Chart interaction ─────────────────────────────────────────
        private RectangleF[]? _chartHitZones;
        private int _hoveredChartIndex = -1;

        // ── Globe state ───────────────────────────────────────────────
        private double _mapCenterLat = 46.0;
        private double _mapCenterLon = 2.0;
        private double _mapZoom      = 1.0;
        private bool   _isDraggingMap = false;
        private Point  _lastDragPos;
        private List<float[]>         _coastlines       = new();
        private bool                  _detailedMapLoaded = false;
        private List<CitySearchResult> _nearbyCities     = new();
        private string? _hoveredCityName;

        // ── World capitals ────────────────────────────────────────────
        private readonly List<(string Name, double Lat, double Lon)> _worldCapitals = new()
        {
            ("Paris",        48.8566,   2.3522), ("Tokyo",       35.6762,  139.6503),
            ("New York",     40.7128,  -74.0060), ("London",      51.5074,   -0.1278),
            ("Beijing",      39.9042,  116.4074), ("Moscow",      55.7558,   37.6173),
            ("Sydney",      -33.8688,  151.2093), ("Cairo",       30.0444,   31.2357),
            ("Buenos Aires",-34.6037,  -58.3816), ("Los Angeles", 34.0522, -118.2437),
            ("Toronto",      43.6510,  -79.3470), ("Cape Town",  -33.9249,   18.4241),
            ("Seoul",        37.5665,  126.9780), ("Istanbul",    41.0082,   28.9784),
            ("Berlin",       52.5200,   13.4050), ("Madrid",      40.4168,   -3.7038),
            ("Rome",         41.9028,   12.4964), ("Bangkok",     13.7563,  100.5018),
            ("Dubai",        25.2048,   55.2708), ("Singapore",    1.3521,  103.8198),
            ("Mumbai",       19.0760,   72.8777), ("Mexico City", 19.4326,  -99.1332),
            ("São Paulo",   -23.5505,  -46.6333), ("Shanghai",    31.2304,  121.4737),
            ("Delhi",        28.7041,   77.1025), ("Nairobi",     -1.2864,   36.8172),
        };

        // ── Animation ────────────────────────────────────────────────
        private Timer? _spinnerTimer;
        private Timer? _clockTimer;
        private Timer? _mapExploreTimer;
        private bool   _isLoading    = false;
        private int    _spinnerAngle = 0;
        private int    _pulseAlpha   = 20;
        private bool   _pulseGoingUp = true;

        // ── UI Controls ───────────────────────────────────────────────
        // Sidebar
        private Panel?  _sidebar;
        private Button? _navSearch;
        private Button? _navFavorites;
        private Button? _navGlobe;
        private Button? _navSettings;
        private readonly List<Button> _navButtons  = new();
        private readonly List<Panel>  _contentPanels = new();

        // Content panels
        private Panel? _pnlSearch;
        private Panel? _pnlFavorites;
        private Panel? _pnlGlobe;
        private Panel? _pnlSettings;

        // Search panel controls
        private TextBox?   _txtCitySearch;
        private Button?    _btnSearch;
        private Button?    _btnAddFavorite;
        private Label?     _lblCityName;
        private Label?     _lblCoords;
        private Panel?     _cardsPanel;
        private Panel?     _spinnerPanel;
        private Panel?     _detailPanel;
        private Panel?     _searchCard;

        // Globe panel
        private BufferedPanel? _mapPanel;

        // Favorites panel
        private ListBox?  _lstFavorites;
        private Panel?    _favoritesCard;
        private Panel?    _favoritesToolbar;
        private Button?   _btnLoadFavorite;
        private Button?   _btnRemoveFavorite;

        // Settings
        private CheckBox? _chkDarkMode;
        private Panel?    _settingsCard;
        private Label?    _settingsTitle;
        private Label?    _settingsApiNote;

        // Status bar
        private ToolStripStatusLabel? _lblStatus;
        private Label?                _timeLabel;

        // Shared HttpClient
        private static readonly HttpClient _iconHttpClient = new HttpClient();

        // ── Theme colours (match LibraryApp ThemeManager) ─────────────
        private Color Bg        => _darkMode ? Color.FromArgb(24, 24, 36)  : Color.FromArgb(247, 248, 252);
        private Color Surface   => _darkMode ? Color.FromArgb(36, 36, 52)  : Color.White;
        private Color Sidebar   => Color.FromArgb(44, 32, 80);
        private Color SideHover => Color.FromArgb(62, 48, 105);
        private Color SideActive=> Color.FromArgb(75, 58, 120);
        private Color TextMain  => _darkMode ? Color.FromArgb(230, 232, 240): Color.FromArgb(33, 37, 41);
        private Color TextMuted => _darkMode ? Color.FromArgb(148, 155, 170): Color.FromArgb(108, 117, 125);
        private Color Accent    => _darkMode ? Color.FromArgb(130, 100, 220): Color.FromArgb(99, 69, 180);
        private Color Border    => _darkMode ? Color.FromArgb(55, 55, 75)   : Color.FromArgb(222, 226, 230);
        private Color Success   => Color.FromArgb(40, 167, 69);
        private Color AccentGold => Color.FromArgb(255, 196, 57);
        private Color CardBorder => _darkMode ? Color.FromArgb(55, 55, 75) : Color.FromArgb(222, 226, 230);

        // ── Constructor ───────────────────────────────────────────────
        public MainForm()
        {
            Text            = "WeatherPro";
            Size            = new Size(1360, 960);
            MinimumSize     = new Size(1200, 860);
            StartPosition   = FormStartPosition.CenterScreen;
            Font            = new Font("Segoe UI", 10F);
            BackColor       = Bg;
            DoubleBuffered  = true;

            _favorites = new List<FavoriteCity>(_favoritesService.GetAll());
            InitFallbackCoastlines();
            BuildUI();
            ApplyTheme();
            StartTimers();
            LoadDetailedMapAsync();
        }

        // ── Geometry helper ───────────────────────────────────────────
        public static GraphicsPath RoundedRect(float x, float y, float w, float h, float radius)
        {
            var path = new GraphicsPath();
            path.AddArc(x, y, radius * 2, radius * 2, 180, 90);
            path.AddArc(x + w - radius * 2, y, radius * 2, radius * 2, 270, 90);
            path.AddArc(x + w - radius * 2, y + h - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(x, y + h - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseFigure();
            return path;
        }

        // ── Temperature colour ────────────────────────────────────────
        private static Color GetTemperatureColor(double t)
        {
            if (t <= 0)  return Color.FromArgb(120, 190, 255);
            if (t <= 10) return Color.FromArgb(80,  155, 255);
            if (t <= 18) return Color.FromArgb(52,  211, 153);
            if (t <= 25) return Color.FromArgb(255, 196, 57);
            if (t <= 32) return Color.FromArgb(255, 140, 50);
            return Color.FromArgb(255, 80, 60);
        }

        // ── Globe projection ──────────────────────────────────────────
        private PointF? ProjectGlobe(double lat, double lon, int W, int H)
        {
            double radius = Math.Min(W, H) / 2.0 * 0.85 * _mapZoom;
            double cLat = _mapCenterLat * Math.PI / 180.0;
            double cLon = _mapCenterLon * Math.PI / 180.0;
            double pLat = lat * Math.PI / 180.0;
            double pLon = lon * Math.PI / 180.0;
            double dLon = pLon - cLon;
            double x = Math.Cos(pLat) * Math.Sin(dLon);
            double y = Math.Cos(cLat) * Math.Sin(pLat) - Math.Sin(cLat) * Math.Cos(pLat) * Math.Cos(dLon);
            double z = Math.Sin(cLat) * Math.Sin(pLat) + Math.Cos(cLat) * Math.Cos(pLat) * Math.Cos(dLon);
            if (z < 0) return null;
            return new PointF(W / 2f + (float)(x * radius), H / 2f - (float)(y * radius));
        }

        // ── Fallback coastlines ───────────────────────────────────────
        private void InitFallbackCoastlines()
        {
            _coastlines = new List<float[]>
            {
                new float[]{80,-160,70,-160,70,-140,73,-120,75,-70,60,-60,50,-55,40,-70,30,-80,15,-90,8,-75,12,-85,20,-95,30,-115,45,-125,60,-140,60,-165,70,-160,80,-160},
                new float[]{15,-75,12,-70,5,-50,-5,-35,-20,-40,-30,-50,-40,-60,-55,-70,-40,-75,-20,-70,-5,-80,15,-75},
                new float[]{70,-10,75,20,75,80,70,120,65,170,50,140,40,120,30,120,25,110,15,110,10,105,10,80,20,60,10,45,20,40,35,35,40,20,35,-10,45,-5,55,0,60,10,70,-10},
                new float[]{35,-10,35,30,35,45,25,40,10,50,0,40,-10,40,-20,35,-35,25,-20,15,-10,10,5,5,5,-15,20,-15,35,-10},
                new float[]{-15,115,-15,140,-20,150,-25,155,-35,150,-40,145,-35,115,-20,115,-15,115},
                new float[]{50,-5,58,-5,58,2,50,2,50,-5},
                new float[]{31,130,35,135,40,140,45,142,45,144,40,142,35,140,31,132,31,130}
            };
        }

        private async void LoadDetailedMapAsync()
        {
            try
            {
                using var client = new HttpClient();
                string geoJson = await client.GetStringAsync(
                    "https://raw.githubusercontent.com/nvkelso/natural-earth-vector/master/geojson/ne_110m_coastline.geojson");
                using var doc = System.Text.Json.JsonDocument.Parse(geoJson);
                var lines = new List<float[]>();
                foreach (var feature in doc.RootElement.GetProperty("features").EnumerateArray())
                {
                    var geom = feature.GetProperty("geometry");
                    string? t = geom.GetProperty("type").GetString();
                    if (t == "LineString") lines.Add(ParseCoords(geom.GetProperty("coordinates")));
                    else if (t == "MultiLineString")
                        foreach (var l in geom.GetProperty("coordinates").EnumerateArray())
                            lines.Add(ParseCoords(l));
                }
                _coastlines = lines; _detailedMapLoaded = true;
                _mapPanel?.Invalidate();
            }
            catch { }
        }

        private static float[] ParseCoords(System.Text.Json.JsonElement arr)
        {
            var pts = new List<float>();
            foreach (var p in arr.EnumerateArray()) { pts.Add(p[1].GetSingle()); pts.Add(p[0].GetSingle()); }
            return pts.ToArray();
        }

        private Dictionary<string, PointF> BuildVisibleMarkers(int W, int H)
        {
            var d = new Dictionary<string, PointF>();
            foreach (var f in _favorites) { var p = ProjectGlobe(f.Latitude, f.Longitude, W, H); if (p.HasValue) d[f.Name] = p.Value; }
            if (_currentCity != null) { var p = ProjectGlobe(_currentCity.Latitude, _currentCity.Longitude, W, H); if (p.HasValue) d[_currentCity.Name] = p.Value; }
            if (_mapZoom >= 1.2) foreach (var (n, la, lo) in _worldCapitals) { if (!d.ContainsKey(n)) { var p = ProjectGlobe(la, lo, W, H); if (p.HasValue) d[n] = p.Value; } }
            if (_mapZoom >= 1.5) foreach (var c in _nearbyCities) { if (!d.ContainsKey(c.Name)) { var p = ProjectGlobe(c.Latitude, c.Longitude, W, H); if (p.HasValue) d[c.Name] = p.Value; } }
            return d;
        }
    }
}
