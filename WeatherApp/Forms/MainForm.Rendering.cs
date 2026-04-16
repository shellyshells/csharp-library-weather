// ============================================================
// WeatherPro — Custom Rendering (GDI+ painting)
// ============================================================

using WeatherApp.Models;
using WeatherApp.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
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
            using var titleFont = new Font("Segoe UI Semibold", 15F);
            using var titleBr = new SolidBrush(TextMain);
            g.DrawString($"Details — {dt:dddd dd MMMM yyyy}", titleFont, titleBr, 16, 12);
            using var sepPen = new Pen(Border, 1);
            g.DrawLine(sepPen, 16, 44, W - 16, 44);

            int chartX = 16;
            int chartY = 56;
            const int statsBlockWidth = 570;
            int chartWidth = Math.Max(500, W - statsBlockWidth - 76);
            int chartHeight = Math.Max(280, H - chartY - 64);
            DrawTemperatureChart(g, chartX, chartY, chartWidth, chartHeight);

            // Right-side stats block in 3 columns with slightly roomier spacing.
            int statsX = Math.Max(chartX + chartWidth + 14, W - statsBlockWidth - 20);
            int statsY = 66;
            const int cols = 3;
            const int colWidth = 182;
            const int rowHeight = 82;

            var stats = new (string Icon, string Label, string Value)[]
            {
                ("🌡", "Feels Like", $"{_selectedDay.FeelsLike:F1}°C"),
                ("🌅", "Sunrise", _selectedDay.Sunrise),
                ("💧", "Humidity", $"{_selectedDay.Humidity}%"),
                ("📊", "Pressure", $"{_selectedDay.Pressure:F0} hPa"),
                ("🌇", "Sunset", _selectedDay.Sunset),
                ("☁", "Clouds", $"{_selectedDay.CloudCover}%"),
                ("👁", "Visibility", $"{_selectedDay.Visibility:F1} km"),
                ("☔", "Rain", $"{_selectedDay.PrecipitationProbability}%"),
                ("🌬", "Wind", $"{_selectedDay.WindSpeed:F0} km/h")
            };

            for (int i = 0; i < stats.Length; i++)
            {
                int col = i % cols;
                int row = i / cols;
                int x = statsX + col * colWidth;
                int y = statsY + row * rowHeight;
                DrawStatBox(g, x, y, stats[i].Icon, stats[i].Label, stats[i].Value);
            }

            // Compass
            DrawWindCompass(g, W - 78, H - 82, 74, _selectedDay.WindSpeed);
        }

        private void DrawStatBox(Graphics g, int x, int y, string icon, string label, string value)
        {
            using var iconFont  = new Font("Segoe UI Emoji", 11F);
            using var lblFont   = new Font("Segoe UI", 9.5f);
            using var valFont   = new Font("Segoe UI Semibold", 11.5f);
            using var accentBr  = new SolidBrush(Accent);
            using var mutedBr   = new SolidBrush(TextMuted);
            using var mainBr    = new SolidBrush(TextMain);
            const int iconColumnWidth = 39;
            g.DrawString(icon,  iconFont, accentBr, x,                        y);
            g.DrawString(label, lblFont,  mutedBr,  x + iconColumnWidth,      y + 2);
            g.DrawString(value, valFont,  mainBr,   x + iconColumnWidth + 1,  y + 28);
        }

        private void DrawWindCompass(Graphics g, int cx, int cy, int size, double windSpeed)
        {
            int r = size / 2;
            using var bg = new SolidBrush(_darkMode ? Color.FromArgb(36, 36, 52) : Color.FromArgb(235, 240, 255));
            g.FillEllipse(bg, cx - r, cy - r, size, size);
            using var border = new Pen(Border, 1.5f);
            g.DrawEllipse(border, cx - r, cy - r, size, size);
            using var cardFont = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            using var accentBr = new SolidBrush(Accent);
            using var mutedBr  = new SolidBrush(TextMuted);
            g.DrawString("N", cardFont, accentBr, cx - 6, cy - r - 16);
            g.DrawString("S", cardFont, mutedBr, cx - 5, cy + r + 3);
            g.DrawString("E", cardFont, mutedBr, cx + r + 4, cy - 7);
            g.DrawString("W", cardFont, mutedBr, cx - r - 16, cy - 7);
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
            using var gridFont = new Font("Segoe UI", 7.5f);
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

            using var boldSml = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            using var reg     = new Font("Segoe UI", 8.5f);
            using var mainBr  = new SolidBrush(TextMain);

            for (int i = 0; i < n; i++)
            {
                bool hov = i == _hoveredChartIndex;
                bool sel = _selectedDay == days[i];
                if (hov || sel)
                {
                    var zone = _chartHitZones![i];
                    using var hl = new SolidBrush(Color.FromArgb(20, Accent));
                    // Fill the full hover zone so the highlight reaches below the chart line area.
                    g.FillRectangle(hl, zone.X + zone.Width * 0.1f, zone.Y, zone.Width * 0.8f, zone.Height);
                }
                int ds = (hov || sel) ? 12 : 8;
                int doff = ds / 2;
                g.FillEllipse(new SolidBrush(Color.FromArgb(255, 110, 90)), maxPts[i].X - doff, maxPts[i].Y - doff, ds, ds);
                g.FillEllipse(new SolidBrush(Accent), minPts[i].X - doff, minPts[i].Y - doff, ds, ds);

                var tf = (hov || sel) ? boldSml : reg;
                using var maxC = new SolidBrush((hov || sel) ? Color.FromArgb(255, 130, 110) : TextMuted);
                using var minC = new SolidBrush((hov || sel) ? Accent : Color.FromArgb(140, Accent.R, Accent.G, Accent.B));
                g.DrawString($"{days[i].TempMax:F0}°", tf, maxC, maxPts[i].X - 12, maxPts[i].Y - 20);
                g.DrawString($"{days[i].TempMin:F0}°", tf, minC, minPts[i].X - 12, minPts[i].Y + 8);

                string abbr = DateTime.Parse(days[i].Date).ToString("ddd");
                using var dayC = new SolidBrush((hov || sel) ? TextMain : TextMuted);
                g.DrawString(abbr, (hov || sel) ? boldSml : reg, dayC, maxPts[i].X - 11, y + height + 6);
            }

            // Legend
            using var legFont = new Font("Segoe UI", 8.5f);
            g.FillEllipse(new SolidBrush(Color.FromArgb(255, 110, 90)), x, y + height + 28, 8, 8);
            g.DrawString("Max", legFont, mutedBr, x + 12, y + height + 26);
            g.FillEllipse(new SolidBrush(Accent), x + 58, y + height + 28, 8, 8);
            g.DrawString("Min", legFont, mutedBr, x + 70, y + height + 26);
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
            float globeR = (float)(Math.Min(W, H) / 2.0 * 0.85 * _mapZoom);
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
                g.DrawString(_currentCity.Name, nf, goldBr, 12, H - 56);
                g.DrawString($"{_currentCity.Country}  ·  {_currentCity.Latitude:F2}°  ·  {_currentCity.Longitude:F2}°", cf, mutedBr, 12, H - 30);
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
            float markerScale = (float)Math.Clamp(0.85 + _mapZoom * 0.35, 0.9, 3.2);
            int idleSize = (int)Math.Round(8 * markerScale);
            int idleInner = Math.Max(2, (int)Math.Round(2 * markerScale));
            float labelOffsetX = 8f + Math.Max(0, (markerScale - 1f) * 2f);
            float labelOffsetY = 6f + Math.Max(0, (markerScale - 1f) * 1.5f);
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
                    int aura = (int)Math.Round(30 * markerScale);
                    int auraBorder = (int)Math.Round(26 * markerScale);
                    int core = (int)Math.Round(12 * markerScale);
                    int auraHalf = aura / 2;
                    int auraBorderHalf = auraBorder / 2;
                    int coreHalf = core / 2;

                    using var pf = new SolidBrush(Color.FromArgb(alpha, glow));
                    using var pb = new Pen(Color.FromArgb(alpha, glow), 2);
                    using var glowBr = new SolidBrush(glow);
                    g.FillEllipse(pf, pt.X - auraHalf, pt.Y - auraHalf, aura, aura);
                    g.DrawEllipse(pb, pt.X - auraBorderHalf, pt.Y - auraBorderHalf, auraBorder, auraBorder);
                    g.FillEllipse(glowBr, pt.X - coreHalf, pt.Y - coreHalf, core, core);
                    float activeNameSize = (float)Math.Clamp(8.5 + (markerScale - 1f) * 1.2, 8.5, 11.5);
                    using var bf = new Font("Segoe UI Semibold", activeNameSize);
                    using var gb = new SolidBrush(glow);
                    g.DrawString(name, bf, gb, pt.X + 12 + (markerScale - 1f) * 2f, pt.Y - 8 - (markerScale - 1f));
                }
                else
                {
                    Color dc = isFav ? AccentGold : isCap ? (_darkMode ? Color.FromArgb(180, 200, 230) : Color.FromArgb(80, 110, 150)) : (_darkMode ? Color.FromArgb(85, 130, 210) : Color.FromArgb(50, 90, 180));
                    int idleHalf = idleSize / 2;
                    int innerHalf = idleInner / 2;
                    using var dcBr = new SolidBrush(dc);
                    g.FillEllipse(dcBr, pt.X - idleHalf, pt.Y - idleHalf, idleSize, idleSize);
                    g.FillEllipse(Brushes.White, pt.X - innerHalf, pt.Y - innerHalf, idleInner, idleInner);
                    float capSize = (float)Math.Clamp(8.5 + (markerScale - 1f) * 0.8, 8.5, 10.5);
                    Font lf = isCap ? new Font("Segoe UI Semibold", capSize) : cityFont;
                    using var mb = new SolidBrush(TextMuted);
                    g.DrawString(name, lf, mb, pt.X + labelOffsetX, pt.Y - labelOffsetY);
                    if (isCap) lf.Dispose();
                }
            }
        }

        // ── FORECAST CARD ─────────────────────────────────────────────
        private Panel CreateForecastCard(WeatherDay day, int index)
        {
            var card = new Panel
            {
                Size = new Size(220, 480),
                BackColor = Surface,
                Cursor = Cursors.Hand,
                Tag = day
            };

            var iconBox = new PictureBox
            {
                Location = new Point(37, 70),
                Size = new Size(146, 120),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent
            };
            LoadWeatherIcon(iconBox, day.IconCode);

            // Humidity bar background
            var barBg = new Panel
            {
                Location = new Point(14, 330),
                Size = new Size(192, 7),
                BackColor = CardBorder
            };
            var barFill = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size((int)(192 * day.Humidity / 100.0), 7),
                BackColor = Accent
            };
            barBg.Controls.Add(barFill);

            card.Controls.AddRange(new Control[] { iconBox, barBg });

            // Paint ALL text directly - no label clipping!
            card.Paint += (_, e2) =>
            {
                var g2 = e2.Graphics;
                g2.SmoothingMode = SmoothingMode.AntiAlias;
                g2.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                bool sel = _selectedDay == day;

                using var cp = RoundedRect(1, 1, card.Width - 2, card.Height - 2, 12);
                using var cb = new SolidBrush(Surface);
                g2.FillPath(cb, cp);
                using var bp = new Pen(sel ? Accent : CardBorder, sel ? 2f : 1f);
                g2.DrawPath(bp, cp);

                // TODAY badge
                if (index == 0)
                {
                    using var tp2 = RoundedRect(2, 2, card.Width - 4, 28, 8);
                    using var tb = new SolidBrush(Accent);
                    g2.FillPath(tb, tp2);
                    using var tf2 = new Font("Segoe UI Semibold", 9.5F);
                    using var todayFmt = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };
                    g2.DrawString("TODAY", tf2, Brushes.White, new RectangleF(2, 2, card.Width - 4, 28), todayFmt);
                }

                // Draw all text with proper alignment and NO clipping
                int yOff = index == 0 ? 36 : 10;

                using var dayFont = new Font("Segoe UI Semibold", 12F);
                using var dayBr = new SolidBrush(TextMain);
                g2.DrawString(DateTime.Parse(day.Date).ToString("dddd"), dayFont, dayBr, 10, yOff + 2);

                using var dateFont = new Font("Segoe UI", 10f);
                using var dateBr = new SolidBrush(TextMuted);
                g2.DrawString(DateTime.Parse(day.Date).ToString("dd MMM"), dateFont, dateBr, 10, yOff + 32);

                using var tempFont = new Font("Segoe UI Semibold", 28F);
                using var tempBr = new SolidBrush(GetTemperatureColor(day.Temperature));
                g2.DrawString($"{day.Temperature:F0}°C", tempFont, tempBr, 10, yOff + 174);

                using var minmaxFont = new Font("Segoe UI", 10.5f);
                using var minmaxBr = new SolidBrush(TextMuted);
                g2.DrawString($"↓{day.TempMin:F0}°  ↑{day.TempMax:F0}°", minmaxFont, minmaxBr, 10, yOff + 250);

                using var descFont = new Font("Segoe UI", 10.5f, FontStyle.Italic);
                using var descBr = new SolidBrush(TextMuted);
                g2.DrawString(day.Description, descFont, descBr, new RectangleF(10, yOff + 288, 200, 50), StringFormat.GenericDefault);

                using var statsFont = new Font("Segoe UI", 9.5f);
                using var statsBr = new SolidBrush(TextMuted);
                g2.DrawString($"💧{day.Humidity}%  ☔{day.PrecipitationProbability}%  💨{day.WindSpeed:F0}", statsFont, statsBr, 10, yOff + 350);
            };

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
                using var rawImage = Image.FromStream(ms);
                var adjustedImage = AdjustIconForLightCards(rawImage, iconCode);

                if (!box.IsDisposed)
                {
                    var oldImage = box.Image;
                    box.Image = adjustedImage;
                    oldImage?.Dispose();
                }
                else
                {
                    adjustedImage.Dispose();
                }
            }
            catch { }
        }

        private static Image AdjustIconForLightCards(Image source, string iconCode)
        {
            using var src = new Bitmap(source);
            var dst = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);
            bool isSunnyIcon = iconCode.StartsWith("01", StringComparison.OrdinalIgnoreCase);

            for (int y = 0; y < src.Height; y++)
            {
                for (int x = 0; x < src.Width; x++)
                {
                    Color p = src.GetPixel(x, y);
                    if (p.A == 0)
                    {
                        dst.SetPixel(x, y, p);
                        continue;
                    }

                    // Make clear-sky sun icons warm yellow instead of near-black.
                    if (isSunnyIcon && p.R < 120 && p.G < 120 && p.B < 120)
                    {
                        dst.SetPixel(x, y, Color.FromArgb(p.A, 245, 196, 66));
                    }
                    // Tone down near-white pixels so cloud bodies stay visible on white cards.
                    else if (p.R > 220 && p.G > 220 && p.B > 220)
                    {
                        dst.SetPixel(x, y, Color.FromArgb(p.A, 212, 216, 224));
                    }
                    else
                    {
                        dst.SetPixel(x, y, p);
                    }
                }
            }

            return dst;
        }
    }
}
