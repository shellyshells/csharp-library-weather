// ============================================================
// WeatherPro — Actions & Event Handlers
// ============================================================

using WeatherApp.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace WeatherApp.Forms
{
    public partial class MainForm
    {
        // ── SEARCH ───────────────────────────────────────────────────
        private async void ExecuteSearch()
        {
            string query = (_txtCitySearch?.Text ?? "").Trim();
            if (string.IsNullOrEmpty(query)) return;

            SetLoading(true);
            _btnSearch!.Enabled = false;
            _detailPanel!.Visible = false;
            _selectedDay = null;

            try
            {
                var (city, forecasts) = await _weatherService.GetForecast(query);
                _currentCity = city;
                _currentForecasts = forecasts;

                _mapCenterLat = city.Latitude;
                _mapCenterLon = city.Longitude;

                _lblCityName!.Text = $"📍  {city.Name}   ({city.Country})";
                _lblCoords!.Text   = $"lat {city.Latitude:F3}°  ·  lon {city.Longitude:F3}°  ·  {forecasts.Count} days";
                _btnAddFavorite!.Enabled = true;
                UpdateFavoriteButton();
                DisplayCardsWithAnimation(forecasts);
                _mapPanel?.Invalidate();
                SetStatus($"✓  {city.Name} ({city.Country})  ·  {forecasts.Count} days  ·  {DateTime.Now:HH:mm:ss}");
            }
            catch (Exception ex)
            {
                string msg = ex.Message.Contains("API") || ex.Message.Contains("key")
                    ? ex.Message : "City not found or network error.";
                MessageBox.Show(msg, "WeatherPro", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                SetStatus("✗  Search failed");
            }
            finally
            {
                SetLoading(false);
                _btnSearch!.Enabled = true;
            }
        }

        // ── CARD ANIMATION ───────────────────────────────────────────
        private void DisplayCardsWithAnimation(List<WeatherDay> forecasts)
        {
            if (_cardsPanel == null) return;
            _cardsPanel.Controls.Clear();
            _cardsPanel.AutoScroll = true;

            const int cardWidth = 220, cardGap = 18;
            int totalWidth = forecasts.Count * (cardWidth + cardGap);
            // Set a fixed layout so cards don't overflow/overlap
            _cardsPanel.AutoScrollMinSize = new System.Drawing.Size(totalWidth, 500);

            for (int i = 0; i < forecasts.Count; i++)
            {
                var card = CreateForecastCard(forecasts[i], i);
                int targetX = 10 + i * (cardWidth + cardGap);
                card.Location = new Point(targetX, 5);
                _cardsPanel.Controls.Add(card);
            }
        }

        // ── FAVORITES ────────────────────────────────────────────────
        private void UpdateFavoriteButton()
        {
            if (_currentCity == null || _btnAddFavorite == null) return;
            bool isFav = _favorites.Exists(f =>
                string.Equals(f.Name, _currentCity.Name, StringComparison.OrdinalIgnoreCase));
            _btnAddFavorite.Text = isFav ? "★ Remove Fav" : "☆ Favourite";
            _btnAddFavorite.ForeColor = isFav ? Color.FromArgb(220, 53, 69) : AccentGold;
        }

        private void FavoriteButton_Click(object? sender, EventArgs e)
        {
            if (_currentCity == null) return;
            bool isFav = _favorites.Exists(f =>
                string.Equals(f.Name, _currentCity.Name, StringComparison.OrdinalIgnoreCase));
            if (isFav)
            {
                _favoritesService.Remove(_currentCity.Name);
                _favorites.RemoveAll(f => string.Equals(f.Name, _currentCity.Name, StringComparison.OrdinalIgnoreCase));
                SetStatus($"✕  {_currentCity.Name} removed from favourites.");
            }
            else
            {
                _favoritesService.Add(_currentCity.Name, _currentCity.Country, _currentCity.Latitude, _currentCity.Longitude);
                _favorites.Add(new FavoriteCity { Name = _currentCity.Name, Country = _currentCity.Country, Latitude = _currentCity.Latitude, Longitude = _currentCity.Longitude });
                SetStatus($"★  {_currentCity.Name} added to favourites.");
            }
            UpdateFavoriteButton();
            _mapPanel?.Invalidate();
        }

        // ── LOADING STATE ─────────────────────────────────────────────
        private void SetLoading(bool loading)
        {
            _isLoading = loading;
            if (_spinnerPanel != null)
            {
                _spinnerPanel.Visible = loading;
                if (loading) _spinnerPanel.BringToFront();
            }
        }

        // ── TIMERS ───────────────────────────────────────────────────
        private void StartTimers()
        {
            _mapExploreTimer = new Timer { Interval = 600 };
            _mapExploreTimer.Tick += async (_, _) =>
            {
                _mapExploreTimer.Stop();
                if (_mapZoom >= 1.5)
                {
                    _nearbyCities = await _weatherService.GetNearbyCities(_mapCenterLat, _mapCenterLon);
                    _mapPanel?.Invalidate();
                }
                else { _nearbyCities.Clear(); _mapPanel?.Invalidate(); }
            };

            _spinnerTimer = new Timer { Interval = 40 };
            _spinnerTimer.Tick += (_, _) =>
            {
                _spinnerAngle = (_spinnerAngle + 15) % 360;
                if (_isLoading) _spinnerPanel?.Invalidate();
                _pulseAlpha += _pulseGoingUp ? 10 : -10;
                if (_pulseAlpha >= 175) _pulseGoingUp = false;
                if (_pulseAlpha <= 15)  _pulseGoingUp = true;
                _mapPanel?.Invalidate();
            };
            _spinnerTimer.Start();

            _clockTimer = new Timer { Interval = 1000 };
            _clockTimer.Tick += (_, _) =>
            {
                if (_timeLabel != null && !_timeLabel.IsDisposed)
                    _timeLabel.Text = DateTime.Now.ToString("HH:mm");
            };
            _clockTimer.Start();
        }
    }
}
