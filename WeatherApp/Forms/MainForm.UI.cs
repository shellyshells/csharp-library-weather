// ============================================================
// WeatherPro — Main Form UI Builder
// ============================================================
// Layout matches LibraryApp: purple sidebar + content area
// ============================================================

using WeatherApp.Models;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace WeatherApp.Forms
{
    public partial class MainForm
    {
        private void BuildUI()
        {
            SuspendLayout();

            // ── SIDEBAR ───────────────────────────────────────────────
            _sidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 212,
                BackColor = Sidebar
            };

            var logoPanel = new Panel { Dock = DockStyle.Top, Height = 78, BackColor = Color.Transparent };
            var logoLabel = new Label
            {
                Text = "WeatherPro",
                Font = new Font("Segoe UI Semibold", 11F),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(14, 0, 0, 0),
                BackColor = Color.Transparent
            };
            logoPanel.Controls.Add(logoLabel);

            var sidebarSep = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(65, 55, 100) };

            _navSearch    = MakeNavBtn("🔍   Search");
            _navFavorites = MakeNavBtn(" ✦  Favourites");
            _navGlobe     = MakeNavBtn("🌐   Globe");
            _navSettings  = MakeNavBtn("⚙️   Settings");
            _navButtons.AddRange(new[] { _navSearch, _navFavorites, _navGlobe, _navSettings });

            var dbLabel = new Label
            {
                Dock = DockStyle.Bottom, Height = 30,
                Text = "◈  WeatherPro v1.0",
                ForeColor = Color.FromArgb(120, 120, 155),
                Font = new Font("Segoe UI", 8.5F),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };

            _sidebar.Controls.Add(dbLabel);
            _sidebar.Controls.Add(_navSettings);
            _sidebar.Controls.Add(_navGlobe);
            _sidebar.Controls.Add(_navFavorites);
            _sidebar.Controls.Add(_navSearch);
            _sidebar.Controls.Add(sidebarSep);
            _sidebar.Controls.Add(logoPanel);

            // ── CONTENT AREA ─────────────────────────────────────────
            var content = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 12, 16, 8) };

            BuildSearchPanel();
            BuildFavoritesPanel();
            BuildGlobePanel();
            BuildSettingsPanel();

            _contentPanels.AddRange(new[] { _pnlSearch!, _pnlFavorites!, _pnlGlobe!, _pnlSettings! });
            foreach (var p in _contentPanels) { p.Dock = DockStyle.Fill; content.Controls.Add(p); }

            // ── STATUS BAR ────────────────────────────────────────────
            var statusStrip = new StatusStrip { SizingGrip = false, BackColor = Surface };
            _lblStatus = new ToolStripStatusLabel("Ready  ·  Enter a city to search") { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
            _timeLabel = new Label
            {
                Text = DateTime.Now.ToString("HH:mm"),
                Font = new Font("Segoe UI", 9F),
                ForeColor = TextMuted,
                AutoSize = true,
                BackColor = Color.Transparent
            };
            var timeTool = new ToolStripControlHost(_timeLabel);
            statusStrip.Items.AddRange(new ToolStripItem[] { _lblStatus, timeTool });

            Controls.Add(content);
            Controls.Add(_sidebar);
            Controls.Add(statusStrip);

            // ── WIRE NAV BUTTONS ──────────────────────────────────────
            _navSearch.Click    += (_, _) => { ActivatePanel(_pnlSearch!,    _navSearch); };
            _navFavorites.Click += (_, _) => { ActivatePanel(_pnlFavorites!, _navFavorites); RefreshFavoritesList(); };
            _navGlobe.Click     += (_, _) => { ActivatePanel(_pnlGlobe!,     _navGlobe); _mapPanel?.Invalidate(); };
            _navSettings.Click  += (_, _) => { ActivatePanel(_pnlSettings!,  _navSettings); };

            ActivatePanel(_pnlSearch!, _navSearch);
            ResumeLayout(true);
        }

        // ── BUILD SEARCH PANEL ────────────────────────────────────────
        private void BuildSearchPanel()
        {
            _pnlSearch = new Panel { BackColor = Bg };

            var header = MakePageHeader("Search / Forecast");

            // ── Search bar card ───────────────────────────────────────
            _searchCard = new Panel
            {
                Dock = DockStyle.Top,
                Height = 78,
                BackColor = Surface,
                Padding = new Padding(10, 12, 10, 12)
            };
            _searchCard.Paint += CardBorderPaint;

            _txtCitySearch = new TextBox
            {
                PlaceholderText = "City name (e.g. Marseille, FR)...",
                Font = new Font("Segoe UI", 11F),
                BorderStyle = BorderStyle.FixedSingle,
                Height = 34,
                Left = 10,
                Top = 15,
                Width = 360
            };

            _btnSearch = MakeAccentButton("🔍  Search", 380, 15, 140, 34);
            _btnAddFavorite = new Button
            {
                Text = "☆ Favourite",
                Left = 530, Top = 15, Width = 140, Height = 34,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(255, 250, 236), ForeColor = Color.FromArgb(161, 114, 22),
                Font = new Font("Segoe UI Semibold", 9.5F),
                Enabled = false, Cursor = Cursors.Hand
            };
            _btnAddFavorite.FlatAppearance.BorderColor = AccentGold;
            _btnAddFavorite.FlatAppearance.BorderSize = 2;
            _btnAddFavorite.FlatAppearance.MouseOverBackColor = Color.FromArgb(255, 244, 214);
            _btnAddFavorite.FlatAppearance.MouseDownBackColor = Color.FromArgb(255, 236, 194);

            _txtCitySearch.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; ExecuteSearch(); } };
            _btnSearch.Click += (_, _) => ExecuteSearch();
            _btnAddFavorite.Click += FavoriteButton_Click;

            void SyncSearchRowHeights()
            {
                int h = _txtCitySearch!.Height;
                _btnSearch!.Height = h;
                _btnAddFavorite!.Height = h;
                _btnSearch.Top = _txtCitySearch.Top;
                _btnAddFavorite.Top = _txtCitySearch.Top;
            }

            _searchCard.Controls.AddRange(new Control[] { _txtCitySearch, _btnSearch, _btnAddFavorite });
            SyncSearchRowHeights();
            _searchCard.Resize += (_, _) =>
            {
                int avail = _searchCard.ClientSize.Width - 20;
                _txtCitySearch!.Width = Math.Max(170, avail - 310);
                _btnSearch!.Left = _txtCitySearch.Right + 10;
                _btnAddFavorite!.Left = _btnSearch.Right + 10;
                SyncSearchRowHeights();
            };

            // ── City info labels ──────────────────────────────────────
            var infoBar = new Panel { Dock = DockStyle.Top, Height = 136, BackColor = Color.Transparent, Padding = new Padding(0, 10, 0, 8) };
            _lblCityName = new Label
            {
                Text = "Enter a city name above to get the forecast",
                Font = new Font("Segoe UI Semibold", 16F),
                ForeColor = TextMain,
                AutoSize = false,
                Dock = DockStyle.Top,
                Height = 80,
                TextAlign = ContentAlignment.TopLeft,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 8, 0, 16)
            };
            _lblCoords = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 10.5F),
                ForeColor = TextMuted,
                AutoSize = false,
                Dock = DockStyle.Bottom,
                Height = 40,
                TextAlign = ContentAlignment.BottomLeft,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 3, 0, 3)
            };
            infoBar.Controls.Add(_lblCityName);
            infoBar.Controls.Add(_lblCoords);

            // ── Loading spinner ───────────────────────────────────────
            _spinnerPanel = new Panel
            {
                Dock = DockStyle.None,
                Size = new Size(980, 290),
                Location = new Point(0, 0),
                BackColor = Color.Transparent,
                Visible = false
            };
            _spinnerPanel.Paint += SpinnerPanel_Paint;

            // ── Cards scroll area ─────────────────────────────────────
            var cardsWrapper = new Panel
            {
                Dock = DockStyle.Top,
                Height = 510,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 8, 0, 0)
            };
            _cardsPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                AutoScroll = false
            };
            cardsWrapper.Controls.Add(_cardsPanel);
            cardsWrapper.Controls.Add(_spinnerPanel);

            // ── Detail panel ──────────────────────────────────────────
            _detailPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Visible = false
            };
            _detailPanel.Paint += DetailPanel_Paint;
            _detailPanel.MouseMove += DetailPanel_MouseMove;
            _detailPanel.MouseLeave += (_, _) => { if (_hoveredChartIndex != -1) { _hoveredChartIndex = -1; _detailPanel.Invalidate(); } };
            _detailPanel.MouseClick += DetailPanel_MouseClick;

            _pnlSearch.Controls.Add(_detailPanel);
            _pnlSearch.Controls.Add(cardsWrapper);
            _pnlSearch.Controls.Add(infoBar);
            _pnlSearch.Controls.Add(_searchCard);
            _pnlSearch.Controls.Add(header);
        }

        // ── BUILD FAVOURITES PANEL ────────────────────────────────────
        private void BuildFavoritesPanel()
        {
            _pnlFavorites = new Panel { BackColor = Bg };
            var header = MakePageHeader("Favourites");

            _favoritesCard = new Panel { Dock = DockStyle.Fill, BackColor = Surface, Padding = new Padding(12) };
            _favoritesCard.Paint += CardBorderPaint;

            _favoritesToolbar = new Panel { Dock = DockStyle.Top, Height = 62, BackColor = Color.Transparent, Padding = new Padding(0, 10, 0, 0) };
            _btnLoadFavorite = MakeAccentButton("Load Weather", 0, 2, 150, 40);
            _btnRemoveFavorite = new Button
            {
                Text = "Remove", Left = 160, Top = 2, Width = 136, Height = 40,
                FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White, Font = new Font("Segoe UI Semibold", 10F), Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter
            };
            _btnRemoveFavorite.FlatAppearance.BorderSize = 0;

            _lstFavorites = new ListBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10.5F),
                BorderStyle = BorderStyle.None,
                BackColor = Surface,
                ForeColor = TextMain,
                ItemHeight = 30
            };

            _btnLoadFavorite.Click += (_, _) =>
            {
                if (_lstFavorites.SelectedItem is FavoriteCity fav)
                {
                    _txtCitySearch!.Text = fav.Name;
                    ActivatePanel(_pnlSearch!, _navSearch!);
                    ExecuteSearch();
                }
            };
            _btnRemoveFavorite.Click += (_, _) =>
            {
                if (_lstFavorites.SelectedItem is FavoriteCity fav)
                {
                    _favoritesService.Remove(fav.Name);
                    _favorites.RemoveAll(f => string.Equals(f.Name, fav.Name, StringComparison.OrdinalIgnoreCase));
                    RefreshFavoritesList();
                    SetStatus($"Removed {fav.Name} from favourites.");
                }
            };

            _favoritesToolbar.Controls.AddRange(new Control[] { _btnLoadFavorite, _btnRemoveFavorite });
            _favoritesCard.Controls.Add(_lstFavorites);
            _favoritesCard.Controls.Add(_favoritesToolbar);
            _pnlFavorites.Controls.Add(_favoritesCard);
            _pnlFavorites.Controls.Add(header);
        }

        // ── BUILD GLOBE PANEL ─────────────────────────────────────────
        private void BuildGlobePanel()
        {
            _pnlGlobe = new Panel { BackColor = Bg };
            var header = MakePageHeader("Interactive Globe");

            _mapPanel = new BufferedPanel { Dock = DockStyle.Fill, BackColor = Bg };
            _mapPanel.Paint += GlobePanel_Paint;

            _mapPanel.MouseDown  += (_, e) => { if (e.Button == MouseButtons.Left) { _isDraggingMap = true; _lastDragPos = e.Location; } };
            _mapPanel.MouseUp    += (_, e) => { if (e.Button == MouseButtons.Left) { _isDraggingMap = false; _mapExploreTimer?.Stop(); _mapExploreTimer?.Start(); } };
            _mapPanel.MouseEnter += (_, _) => _mapPanel.Focus();
            _mapPanel.MouseWheel += (_, e) =>
            {
                _mapZoom = e.Delta > 0 ? Math.Min(_mapZoom * 1.25, 30.0) : Math.Max(_mapZoom / 1.25, 1.0);
                _mapPanel.Invalidate(); _mapExploreTimer?.Stop(); _mapExploreTimer?.Start();
            };
            _mapPanel.MouseMove  += GlobePanel_MouseMove;
            _mapPanel.MouseClick += GlobePanel_MouseClick;

            _pnlGlobe.Controls.Add(_mapPanel);
            _pnlGlobe.Controls.Add(header);
        }

        // ── BUILD SETTINGS PANEL ──────────────────────────────────────
        private void BuildSettingsPanel()
        {
            _pnlSettings = new Panel { BackColor = Bg };
            var header = MakePageHeader("Settings");

            var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16), AutoScroll = true };
            _settingsCard = new Panel { Dock = DockStyle.Top, Height = 232, BackColor = Surface, Padding = new Padding(16) };
            _settingsCard.Paint += CardBorderPaint;

            _settingsTitle = new Label
            {
                Text = "Appearance",
                Dock = DockStyle.Top, Height = 36,
                Font = new Font("Segoe UI Semibold", 11F),
                ForeColor = TextMain,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _chkDarkMode = new CheckBox
            {
                Text = "  Dark Mode",
                Dock = DockStyle.Top, Height = 36,
                Font = new Font("Segoe UI", 10.5F),
                ForeColor = TextMain,
                BackColor = Color.Transparent,
                Checked = _darkMode,
                Cursor = Cursors.Hand
            };
            _chkDarkMode.CheckedChanged += (_, _) =>
            {
                _darkMode = _chkDarkMode.Checked;
                ApplyTheme();
            };

            _settingsApiNote = new Label
            {
                Text = "API Key: Set API_KEY in the .env file next to the executable.\nGet a free key at: openweathermap.org/api",
                Dock = DockStyle.Top, Height = 84,
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = TextMuted,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.TopLeft,
                Padding = new Padding(0, 2, 0, 4)
            };

            _settingsCard.Controls.Add(_settingsApiNote);
            _settingsCard.Controls.Add(_chkDarkMode);
            _settingsCard.Controls.Add(_settingsTitle);
            body.Controls.Add(_settingsCard);
            _pnlSettings.Controls.Add(body);
            _pnlSettings.Controls.Add(header);
        }

        // ── HELPERS ───────────────────────────────────────────────────
        private Button MakeNavBtn(string text)
        {
            var b = new Button
            {
                Text = text, Dock = DockStyle.Top, Height = 42,
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(18, 0, 0, 0),
                Font = new Font("Segoe UI Semibold", 10F),
                ForeColor = Color.FromArgb(170, 170, 200),
                BackColor = Sidebar, Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = SideHover;
            return b;
        }

        private Label MakePageHeader(string text) => new Label
        {
            Text = text, Dock = DockStyle.Top, Height = 58,
            Font = new Font("Segoe UI Semibold", 15F),
            ForeColor = TextMain,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(2, 0, 0, 0),
            Tag = "page-header"
        };

        private Button MakeAccentButton(string text, int x, int y, int w, int h)
        {
            var b = new Button
            {
                Text = text, Left = x, Top = y, Width = w, Height = h,
                FlatStyle = FlatStyle.Flat,
                BackColor = Accent, ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 9.5F),
                Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        private void CardBorderPaint(object? sender, PaintEventArgs e)
        {
            if (sender is not Control c) return;
            using var pen = new Pen(Border, 1f);
            e.Graphics.DrawRectangle(pen, 0, 0, c.Width - 1, c.Height - 1);
        }

        private void ActivatePanel(Panel p, Button nav)
        {
            foreach (var pp in _contentPanels) pp.Visible = false;
            p.Visible = true;
            foreach (var b in _navButtons)
            {
                b.BackColor = Sidebar;
                b.Font = new Font("Segoe UI Semibold", 10F);
                b.ForeColor = Color.FromArgb(170, 170, 200);
            }
            nav.BackColor = SideActive;
            nav.Font = new Font("Segoe UI Semibold", 10F);
            nav.ForeColor = Color.White;
        }

        private void ApplyTheme()
        {
            BackColor = Bg;
            foreach (var p in _contentPanels) p.BackColor = Bg;
            if (_searchCard != null) _searchCard.BackColor = Surface;
            if (_txtCitySearch != null)
            {
                _txtCitySearch.BackColor = Surface;
                _txtCitySearch.ForeColor = TextMain;
            }
            if (_btnSearch != null)
            {
                _btnSearch.BackColor = Accent;
                _btnSearch.ForeColor = Color.White;
            }
            if (_btnAddFavorite != null)
            {
                _btnAddFavorite.BackColor = _darkMode ? Color.FromArgb(58, 49, 27) : Color.FromArgb(255, 250, 236);
                _btnAddFavorite.ForeColor = _darkMode ? AccentGold : Color.FromArgb(161, 114, 22);
                _btnAddFavorite.FlatAppearance.BorderColor = AccentGold;
            }
            if (_favoritesCard != null) _favoritesCard.BackColor = Surface;
            if (_favoritesToolbar != null) _favoritesToolbar.BackColor = Color.Transparent;
            if (_btnLoadFavorite != null)
            {
                _btnLoadFavorite.BackColor = Accent;
                _btnLoadFavorite.ForeColor = Color.White;
            }
            if (_btnRemoveFavorite != null)
            {
                _btnRemoveFavorite.BackColor = Color.FromArgb(220, 53, 69);
                _btnRemoveFavorite.ForeColor = Color.White;
            }
            if (_lstFavorites != null)
            {
                _lstFavorites.BackColor = Surface;
                _lstFavorites.ForeColor = TextMain;
            }
            if (_settingsCard != null) _settingsCard.BackColor = Surface;
            if (_settingsTitle != null) _settingsTitle.ForeColor = TextMain;
            if (_settingsApiNote != null) _settingsApiNote.ForeColor = TextMuted;
            if (_chkDarkMode != null) _chkDarkMode.ForeColor = TextMain;
            if (_lblCityName != null) _lblCityName.ForeColor = TextMain;
            if (_lblCoords != null) _lblCoords.ForeColor = TextMuted;
            foreach (var p in _contentPanels)
            {
                foreach (Control c in p.Controls)
                    if (c is Label l && Equals(l.Tag, "page-header")) l.ForeColor = TextMain;
            }
            if (_currentForecasts != null) DisplayCardsWithAnimation(_currentForecasts);
            _mapPanel?.Invalidate();
            _detailPanel?.Invalidate();
        }

        private void RefreshFavoritesList()
        {
            if (_lstFavorites == null) return;
            _lstFavorites.Items.Clear();
            foreach (var f in _favorites) _lstFavorites.Items.Add(f);
        }

        private void SetStatus(string msg) => _lblStatus!.Text = "  " + msg;

        private void DetailPanel_MouseMove(object? sender, MouseEventArgs e)
        {
            if (_chartHitZones == null) return;
            int hit = -1;
            for (int i = 0; i < _chartHitZones.Length; i++) if (_chartHitZones[i].Contains(e.Location)) { hit = i; break; }
            if (_hoveredChartIndex != hit) { _hoveredChartIndex = hit; _detailPanel!.Invalidate(); }
        }

        private void DetailPanel_MouseClick(object? sender, MouseEventArgs e)
        {
            if (_hoveredChartIndex >= 0 && _hoveredChartIndex < (_currentForecasts?.Count ?? 0))
            {
                _selectedDay = _currentForecasts![_hoveredChartIndex];
                _detailPanel!.Invalidate();
                foreach (Control c in _cardsPanel!.Controls) c.Invalidate();
                SetStatus($"📅  {DateTime.Parse(_selectedDay.Date):dddd dd MMMM yyyy}");
            }
        }

        private void GlobePanel_MouseMove(object? sender, MouseEventArgs e)
        {
            if (_isDraggingMap)
            {
                double sens = 0.6 / _mapZoom;
                _mapCenterLon -= (e.X - _lastDragPos.X) * sens;
                _mapCenterLat += (e.Y - _lastDragPos.Y) * sens;
                _mapCenterLat = Math.Clamp(_mapCenterLat, -90, 90);
                if (_mapCenterLon > 180) _mapCenterLon -= 360;
                if (_mapCenterLon < -180) _mapCenterLon += 360;
                _lastDragPos = e.Location;
                _mapPanel!.Invalidate();
                return;
            }
            int W = _mapPanel!.Width, H = _mapPanel.Height;
            var markers = BuildVisibleMarkers(W, H);
            string? hov = null;
            double hoverRadius = Math.Clamp(7.0 + _mapZoom * 2.0, 12.0, 28.0);
            double hoverRadiusSq = hoverRadius * hoverRadius;
            foreach (var (n, pt) in markers)
                if (Math.Pow(e.X - pt.X, 2) + Math.Pow(e.Y - pt.Y, 2) <= hoverRadiusSq) { hov = n; break; }
            if (_hoveredCityName != hov)
            {
                _hoveredCityName = hov;
                _mapPanel.Invalidate();
                _mapPanel.Cursor = hov != null ? Cursors.Hand : Cursors.Default;
            }
        }

        private void GlobePanel_MouseClick(object? sender, MouseEventArgs e)
        {
            if (_hoveredCityName == null) return;
            _txtCitySearch!.Text = _hoveredCityName;
            ActivatePanel(_pnlSearch!, _navSearch!);
            ExecuteSearch();
        }
    }
}
