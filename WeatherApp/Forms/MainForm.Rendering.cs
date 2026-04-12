// ============================================================
// WeatherPro — Custom Rendering (GDI+ painting)
// ============================================================

using WeatherApp.Models;
using WeatherApp.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Net.Http;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace WeatherApp.Forms
{
    public partial class MainForm
    {
        // ── SPINNER ───────────────────────────────────────────────────
        private void SpinnerPanel_Paint(object? sender, PaintEventArgs e)
        {
            if (!_isLoading) return;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int cx = _spinnerPanel!.Width / 2, cy = _spinnerPanel!.Height / 2;

            using var overlay = new SolidBrush(Color.FromArgb(160, 247, 248, 252));
            g.FillRectangle(overlay, _spinnerPanel.ClientRectangle);

            for (int i = 0; i < 8; i++)
            {
                int alpha = (int)(255 * (i + 1) / 8.0);
                double rad = ((_spinnerAngle + i * 45) % 360) * Math.PI / 180.0;
                float px = cx + (float)(30 * Math.Cos(rad));
                float py = cy + (float)(30 * Math.Sin(rad));
                using var br = new SolidBrush(Color.FromArgb(alpha, Accent));
                g.FillEllipse(br, px - 5, py - 5, 10, 10);
            }

            using var font = new Font("Segoe UI", 10, FontStyle.Bold);
            string lbl = "Loading...";
            var sz = g.MeasureString(lbl, font);
            using var br2 = new SolidBrush(Accent);
            g.DrawString(lbl, font, br2, cx - sz.Width / 2, cy + 44);
        }

        // ── DETAIL PANEL ─────────────────────────────────────────────
        private void DetailPanel_Paint(object? sender, PaintEventArgs e)
        {
            if (_selectedDay == null || _detailPanel == null) return;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            int W = _detailPanel.Width, H = _detailPanel.Height;
            if (W < 50 || H < 50) return;

            // Background
            using var bgPath = RoundedRect(1, 1, W - 2, H - 2, 14);
            using var bgBr = new SolidBrush(Surface);
            g.FillPath(bgBr, bgPath);
            using var borderPen = new Pen(Border, 1.5f);
            g.DrawPath(borderPen, bgPath);

            // Heading
            DateTime dt = DateTime.Parse(_selectedDay.Date);
            using var titleFont = new Font("Segoe UI Semibold", 11F);
            using var titleBr = new SolidBrush(TextMain);
            g.DrawString($"Details — {dt:dddd dd MMMM yyyy}", titleFont, titleBr, 16, 12);
            using var sepPen = new Pen(Border, 1);
            g.DrawLine(sepPen, 16, 36, W - 16, 36);

            // Chart
            DrawTemperatureChart(g, 16, 44, Math.Min(420, W - 300), 148);

            // Stats grid
            int statsX = Math.Min(460, W - 380);
            const int gapX = 115, gapY = 46;

            DrawStatBox(g, statsX,          44,          "🌡", "Feels Like",  $"{_selectedDay.FeelsLike:F1}°C");
            DrawStatBox(g, statsX,          44 + gapY,   "📊", "Pressure",    $"{_selectedDay.Pressure:F0} hPa");
            DrawStatBox(g, statsX,          44 + 2*gapY, "👁", "Visibility",  $"{_selectedDay.Visibility:F1} km");
            DrawStatBox(g, statsX + gapX,   44,          "🌅", "Sunrise",     _selectedDay.Sunrise);
            DrawStatBox(g, statsX + gapX,   44 + gapY,   "🌇", "Sunset",      _selectedDay.Sunset);
            DrawStatBox(g, statsX + gapX,   44 + 2*gapY, "☔", "Rain",        $"{_selectedDay.PrecipitationProbability}%");
            DrawStatBox(g, statsX + 2*gapX, 44,          "💧", "Humidity",    $"{_selectedDay.Humidity}%");
            DrawStatBox(g, statsX + 2*gapX, 44 + gapY,   "☁", "Clouds",      $"{_selectedDay.CloudCover}%");
            DrawStatBox(g, statsX + 2*gapX, 44 + 2*gapY, "🌬", "Wind",       $"{_selectedDay.WindSpeed:F0} km/h");

            // Compass
            DrawWindCompass(g, W - 76, H / 2 - 10, 70, _selectedDay.WindSpeed);
        }

        private void DrawStatBox(Graphics g, int x, int y, string icon, string label, string value)
        {
            using var iconFont  = new Font("Segoe UI", 10F);
            using var lblFont   = new Font("Segoe UI", 7.5f);
            using var valFont   = new Font("Segoe UI Semibold", 9.5f);
            using var accentBr  = new SolidBrush(Accent);
            using var mutedBr   = new SolidBrush(TextMuted);
            using var mainBr    = new SolidBrush(TextMain);
            g.DrawString(icon,  iconFont, accentBr, x,      y);
            g.DrawString(label, lblFont,  mutedBr,  x + 22, y + 2);
            g.DrawString(value, valFont,  mainBr,   x + 22, y + 17);
        }

        private void DrawWindCompass(Graphics g, int cx, int cy, int size, double windSpeed)
        {
            int r = size / 2;
            using var bg = new SolidBrush(_darkMode ? Color.FromArgb(36, 36, 52) : Color.FromArgb(235, 240, 255));
            g.FillEllipse(bg, cx - r, cy - r, size, size);
            using var border = new Pen(Border, 1.5f);
            g.DrawEllipse(border, cx - r, cy - r, size, size);
            using var cardFont = new Font("Segoe UI", 7, FontStyle.Bold);
            using var accentBr = new SolidBrush(Accent);
            using var mutedBr  = new SolidBrush(TextMuted);
            g.DrawString("N", cardFont, accentBr, cx - 5, cy - r - 14);
            g.DrawString("S", cardFont, mutedBr, cx - 4, cy + r + 2);
            g.DrawString("E", cardFont, mutedBr, cx + r + 3, cy - 6);
            g.DrawString("W", cardFont, mutedBr, cx - r - 14, cy - 6);
            double angle = (windSpeed * 7.0) % 360.0;
            double rad = (angle - 90.0) * Math.PI / 180.0;
            float ax = cx + (float)((r - 8) * Math.Cos(rad));
            float ay = cy + (float)((r - 8) * Math.Sin(rad));
            using var needle = new Pen(AccentGold, 2.5f) { EndCap = LineCap.ArrowAnchor };
            g.DrawLine(needle, cx, cy, ax, ay);
            g.FillEllipse(new SolidBrush(Accent), cx - 3, cy - 3, 6, 6);
        }

        private void DrawTemperatureChart(Graphics g, int x, int y, int width, int height)
        {
            if (_currentForecasts == null || _currentForecasts.Count < 2) return;
            var days = _currentForecasts;
            double minT = days.Min(d => d.TempMin) - 3;
            double maxT = days.Max(d => d.TempMax) + 3;
            double range = Math.Max(maxT - minT, 1.0);
            int n = days.Count;

            using var gridPen = new Pen(Color.FromArgb(30, Accent), 1) { DashStyle = DashStyle.Dash };
            using var gridFont = new Font("Segoe UI", 6.5f);
            using var mutedBr = new SolidBrush(TextMuted);

            for (int i = 0; i <= 4; i++)
            {
                float yl = y + height - (float)(i / 4.0 * height);
                g.DrawLine(gridPen, x, yl, x + width, yl);
                g.DrawString($"{minT + (maxT - minT) * i / 4.0:F0}°", gridFont, mutedBr, x - 24, yl - 6);
            }

            var maxPts = new PointF[n];
            var minPts = new PointF[n];
            _chartHitZones = new RectangleF[n];
            float step = (float)width / (n - 1);

            for (int i = 0; i < n; i++)
            {
                float cx = x + i * step;
                maxPts[i] = new PointF(cx, y + height - (float)((days[i].TempMax - minT) / range * height));
                minPts[i] = new PointF(cx, y + height - (float)((days[i].TempMin - minT) / range * height));
                _chartHitZones[i] = new RectangleF(cx - step / 2f, y, step, height + 30);
            }

            using var maxPen = new Pen(Color.FromArgb(255, 100, 80), 2.5f);
            using var minPen = new Pen(Accent, 2.5f);
            g.DrawCurve(maxPen, maxPts);
            g.DrawCurve(minPen, minPts);

            using var boldSml = new Font("Segoe UI", 7.5f, FontStyle.Bold);
            using var reg     = new Font("Segoe UI", 7.5f);
            using var mainBr  = new SolidBrush(TextMain);

            for (int i = 0; i < n; i++)
            {
                bool hov = i == _hoveredChartIndex;
                bool sel = _selectedDay == days[i];
                if (hov || sel)
                {
                    using var hl = new Pen(Color.FromArgb(20, Accent), _chartHitZones![i].Width * 0.8f);
                    g.DrawLine(hl, maxPts[i].X, y, maxPts[i].X, y + height);
                }
                int ds = (hov || sel) ? 12 : 8;
                int doff = ds / 2;
                g.FillEllipse(new SolidBrush(Color.FromArgb(255, 110, 90)), maxPts[i].X - doff, maxPts[i].Y - doff, ds, ds);
                g.FillEllipse(new SolidBrush(Accent), minPts[i].X - doff, minPts[i].Y - doff, ds, ds);

                var tf = (hov || sel) ? boldSml : reg;
                using var maxC = new SolidBrush((hov || sel) ? Color.FromArgb(255, 130, 110) : TextMuted);
                using var minC = new SolidBrush((hov || sel) ? Accent : Color.FromArgb(140, Accent.R, Accent.G, Accent.B));
                g.DrawString($"{days[i].TempMax:F0}°", tf, maxC, maxPts[i].X - 10, maxPts[i].Y - 18);
                g.DrawString($"{days[i].TempMin:F0}°", tf, minC, minPts[i].X - 10, minPts[i].Y + 6);

                string abbr = DateTime.Parse(days[i].Date).ToString("ddd");
                using var dayC = new SolidBrush((hov || sel) ? TextMain : TextMuted);
                g.DrawString(abbr, (hov || sel) ? boldSml : reg, dayC, maxPts[i].X - 9, y + height + 4);
            }

            // Legend
            using var legFont = new Font("Segoe UI", 7.5f);
            g.FillEllipse(new SolidBrush(Color.FromArgb(255, 110, 90)), x, y + height + 20, 8, 8);
            g.DrawString("Max", legFont, mutedBr, x + 12, y + height + 19);
            g.FillEllipse(new SolidBrush(Accent), x + 55, y + height + 20, 8, 8);
            g.DrawString("Min", legFont, mutedBr, x + 67, y + height + 19);
        }

        // ── GLOBE ─────────────────────────────────────────────────────
        private void GlobePanel_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            int W = _mapPanel!.Width, H = _mapPanel.Height;
            if (W < 50 || H < 50) return;

            // Background
            g.Clear(Bg);

            // Globe ocean
            float globeR = (float)(Math.Min(W, H) / 2.0 * 0.85);
            using var ocean = new SolidBrush(_darkMode ? Color.FromArgb(30, 20, 50) : Color.FromArgb(210, 228, 255));
            g.FillEllipse(ocean, W / 2f - globeR, H / 2f - globeR, globeR * 2, globeR * 2);
            using var oceanBorder = new Pen(Color.FromArgb(80, Accent), 1f);
            g.DrawEllipse(oceanBorder, W / 2f - globeR, H / 2f - globeR, globeR * 2, globeR * 2);

            // Grid
            using var gridPen = new Pen(Color.FromArgb(30, Accent), 1f);
            for (int lat = -80; lat <= 80; lat += 20) DrawGlobeLatLine(g, gridPen, lat, W, H);
            for (int lon = -180; lon <= 180; lon += 20) DrawGlobeLonLine(g, gridPen, lon, W, H);

            // Coastlines
            using var coastPen = new Pen(_darkMode ? Color.FromArgb(100, 150, 220) : Color.FromArgb(80, 130, 200), _detailedMapLoaded ? 1.2f : 2f) { LineJoin = LineJoin.Round };
            foreach (var poly in _coastlines) DrawCoastline(g, coastPen, poly, W, H);

            // Title
            using var titleFont = new Font("Segoe UI Semibold", 9.5f);
            using var accentBr = new SolidBrush(Accent);
            g.DrawString("⛯  Favorites Globe  (drag · scroll to zoom)", titleFont, accentBr, 12, 12);

            // City markers
            DrawCityMarkers(g, W, H);

            // Bottom info
            if (_currentCity != null)
            {
                using var nf = new Font("Segoe UI Semibold", 10F);
                using var cf = new Font("Segoe UI", 8.5f);
                using var goldBr = new SolidBrush(AccentGold);
                using var mutedBr = new SolidBrush(TextMuted);
                g.DrawString(_currentCity.Name, nf, goldBr, 12, H - 45);
                g.DrawString($"{_currentCity.Country}  ·  {_currentCity.Latitude:F2}°  ·  {_currentCity.Longitude:F2}°", cf, mutedBr, 12, H - 25);
            }
        }

        private void DrawGlobeLatLine(Graphics g, Pen pen, int lat, int W, int H)
        {
            var pts = new List<PointF>();
            for (int lon = -180; lon <= 180; lon += 5)
            {
                var pt = ProjectGlobe(lat, lon, W, H);
                if (pt.HasValue) pts.Add(pt.Value);
                else { if (pts.Count > 1) g.DrawLines(pen, pts.ToArray()); pts.Clear(); }
            }
            if (pts.Count > 1) g.DrawLines(pen, pts.ToArray());
        }

        private void DrawGlobeLonLine(Graphics g, Pen pen, int lon, int W, int H)
        {
            var pts = new List<PointF>();
            for (int lat = -90; lat <= 90; lat += 5)
            {
                var pt = ProjectGlobe(lat, lon, W, H);
                if (pt.HasValue) pts.Add(pt.Value);
                else { if (pts.Count > 1) g.DrawLines(pen, pts.ToArray()); pts.Clear(); }
            }
            if (pts.Count > 1) g.DrawLines(pen, pts.ToArray());
        }

        private void DrawCoastline(Graphics g, Pen pen, float[] poly, int W, int H)
        {
            var pts = new List<PointF>();
            for (int i = 0; i < poly.Length - 1; i += 2)
            {
                var pt = ProjectGlobe(poly[i], poly[i + 1], W, H);
                if (pt.HasValue) pts.Add(pt.Value);
                else { if (pts.Count > 1) g.DrawLines(pen, pts.ToArray()); pts.Clear(); }
            }
            if (pts.Count > 1) g.DrawLines(pen, pts.ToArray());
        }

        private void DrawCityMarkers(Graphics g, int W, int H)
        {
            using var cityFont = new Font("Segoe UI", 8.5f);
            var markers = new Dictionary<string, (double lat, double lon, bool isFav, bool isCapital)>();

            foreach (var f in _favorites)  markers[f.Name] = (f.Latitude, f.Longitude, true, false);
            if (_currentCity != null && !markers.ContainsKey(_currentCity.Name))
                markers[_currentCity.Name] = (_currentCity.Latitude, _currentCity.Longitude, false, false);
            if (_mapZoom >= 1.2)
                foreach (var (n, la, lo) in _worldCapitals)
                    if (!markers.ContainsKey(n)) markers[n] = (la, lo, false, true);

            foreach (var (name, (lat, lon, isFav, isCap)) in markers)
            {
                var sp = ProjectGlobe(lat, lon, W, H);
                if (!sp.HasValue) continue;
                var pt = sp.Value;
                bool isActive  = _currentCity != null && string.Equals(_currentCity.Name, name, StringComparison.OrdinalIgnoreCase);
                bool isHovered = string.Equals(_hoveredCityName, name, StringComparison.OrdinalIgnoreCase);

                if (isActive || isHovered)
                {
                    int alpha = isActive ? _pulseAlpha : 150;
                    Color glow = isHovered ? Color.White : AccentGold;
                    using var pf = new SolidBrush(Color.FromArgb(alpha, glow));
                    using var pb = new Pen(Color.FromArgb(alpha, glow), 2);
                    g.FillEllipse(pf, pt.X - 15, pt.Y - 15, 30, 30);
                    g.DrawEllipse(pb, pt.X - 13, pt.Y - 13, 26, 26);
                    g.FillEllipse(new SolidBrush(glow), pt.X - 6, pt.Y - 6, 12, 12);
                    using var bf = new Font("Segoe UI Semibold", 8.5f);
                    using var gb = new SolidBrush(glow);
                    g.DrawString(name, bf, gb, pt.X + 12, pt.Y - 8);
                }
                else
                {
                    Color dc = isFav ? AccentGold : isCap ? (_darkMode ? Color.FromArgb(180, 200, 230) : Color.FromArgb(80, 110, 150)) : (_darkMode ? Color.FromArgb(85, 130, 210) : Color.FromArgb(50, 90, 180));
                    g.FillEllipse(new SolidBrush(dc), pt.X - 4, pt.Y - 4, 8, 8);
                    g.FillEllipse(Brushes.White, pt.X - 1, pt.Y - 1, 2, 2);
                    Font lf = isCap ? new Font("Segoe UI Semibold", 8.5f) : cityFont;
                    using var mb = new SolidBrush(TextMuted);
                    g.DrawString(name, lf, mb, pt.X + 8, pt.Y - 6);
                    if (isCap) lf.Dispose();
                }
            }
        }

        // ── FORECAST CARD ─────────────────────────────────────────────
        private Panel CreateForecastCard(WeatherDay day, int index)
        {
            var card = new Panel
            {
                Size = new Size(162, 235),
                BackColor = Surface,
                Cursor = Cursors.Hand,
                Tag = day
            };

            // Paint the card border/background (NO child labels – all drawn via Paint)
            card.Paint += (_, e2) =>
            {
                var g2 = e2.Graphics;
                g2.SmoothingMode = SmoothingMode.AntiAlias;
                bool sel = _selectedDay == day;

                using var cp = RoundedRect(1, 1, card.Width - 2, card.Height - 2, 12);
                using var cb = new SolidBrush(Surface);
                g2.FillPath(cb, cp);
                using var bp = new Pen(sel ? Accent : CardBorder, sel ? 2f : 1f);
                g2.DrawPath(bp, cp);

                // TODAY badge
                if (index == 0)
                {
                    using var tp2 = RoundedRect(2, 2, card.Width - 4, 22, 8);
                    using var tb = new SolidBrush(Accent);
                    g2.FillPath(tb, tp2);
                    using var tf2 = new Font("Segoe UI Semibold", 7F);
                    g2.DrawString("TODAY", tf2, Brushes.White, 32, 6);
                }
            };

            int yOff = index == 0 ? 28 : 8;

            // Day label
            var lblDay = new Label
            {
                Text = DateTime.Parse(day.Date).ToString("dddd"),
                Font = new Font("Segoe UI Semibold", 9F),
                ForeColor = TextMain,
                Location = new Point(5, yOff),
                Size = new Size(152, 20),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };

            var lblDate = new Label
            {
                Text = DateTime.Parse(day.Date).ToString("dd MMM"),
                Font = new Font("Segoe UI", 7.5f),
                ForeColor = TextMuted,
                Location = new Point(5, yOff + 18),
                Size = new Size(152, 16),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };

            var iconBox = new PictureBox
            {
                Location = new Point(31, yOff + 36),
                Size = new Size(100, 68),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent
            };
            LoadWeatherIcon(iconBox, day.IconCode);

            var lblTemp = new Label
            {
                Text = $"{day.Temperature:F0}°C",
                Font = new Font("Segoe UI Semibold", 18F),
                ForeColor = GetTemperatureColor(day.Temperature),
                Location = new Point(5, yOff + 108),
                Size = new Size(152, 34),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };

            var lblMinMax = new Label
            {
                Text = $"↓{day.TempMin:F0}°   ↑{day.TempMax:F0}°",
                Font = new Font("Segoe UI", 7.5f),
                ForeColor = TextMuted,
                Location = new Point(5, yOff + 142),
                Size = new Size(152, 15),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };

            var lblDesc = new Label
            {
                Text = day.Description,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Italic),
                ForeColor = TextMuted,
                Location = new Point(5, yOff + 158),
                Size = new Size(152, 14),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };

            // Humidity bar background
            var barBg = new Panel
            {
                Location = new Point(14, yOff + 176),
                Size = new Size(134, 5),
                BackColor = CardBorder
            };
            var barFill = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size((int)(134 * day.Humidity / 100.0), 5),
                BackColor = Accent
            };
            barBg.Controls.Add(barFill);

            var lblStats = new Label
            {
                Text = $"💧{day.Humidity}%  ☔{day.PrecipitationProbability}%  💨{day.WindSpeed:F0}",
                Font = new Font("Segoe UI", 6.8f),
                ForeColor = TextMuted,
                Location = new Point(4, yOff + 184),
                Size = new Size(154, 14),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };

            card.Controls.AddRange(new Control[] { lblDay, lblDate, iconBox, lblTemp, lblMinMax, lblDesc, barBg, lblStats });

            Action onClick = () =>
            {
                _selectedDay = day;
                _detailPanel!.Visible = true;
                _detailPanel.Invalidate();
                foreach (Control c in _cardsPanel!.Controls) c.Invalidate();
                SetStatus($"📅  {DateTime.Parse(day.Date):dddd dd MMMM yyyy}");
            };

            card.Click += (_, _) => onClick();
            foreach (Control child in card.Controls) child.Click += (_, _) => onClick();

            return card;
        }

        private async void LoadWeatherIcon(PictureBox box, string iconCode)
        {
            try
            {
                byte[] data = await _iconHttpClient.GetByteArrayAsync(WeatherService.GetIconUrl(iconCode));
                using var ms = new System.IO.MemoryStream(data);
                if (!box.IsDisposed) box.Image = Image.FromStream(ms);
            }
            catch { }
        }
    }
}
