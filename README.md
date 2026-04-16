# CSharpProjects — LibraryManager Pro & WeatherPro

**Group:** Michel Mustafov · Edwin Wehbe  
**Module:** C# / WinForms  
**Framework:** .NET 8 (Windows only)

---

## Table of contents

1. [Prerequisites](#prerequisites)
2. [LibraryApp — setup & run](#libraryapp--setup--run)
3. [LibraryApp — features](#libraryapp--features)
4. [LibraryApp — architecture](#libraryapp--architecture)
5. [WeatherApp — setup & run](#weatherapp--setup--run)
6. [WeatherApp — features](#weatherapp--features)
7. [WeatherApp — architecture](#weatherapp--architecture)
8. [Project structure](#project-structure)

---

## Prerequisites

| Tool | Minimum version | Purpose |
|------|----------------|---------|
| .NET SDK | 8.0 | Build & run both apps |
| MySQL Server | 8.0 | LibraryApp database |
| OpenWeatherMap API key | free tier | WeatherApp live data |

> **Windows only** — both apps use WinForms which is not cross-platform.

---

## LibraryApp — setup & run

### 1. Create the database

```bash
mysql -u root -p < LibraryApp/SQL/library_schema.sql
```

This creates the `library_db` database, both tables, indexes, and 10 sample books.

### 2. Run the app

```bash
cd LibraryApp
dotnet restore
dotnet run
```

### 3. Configure the database connection

On first launch, click **Settings** in the sidebar and fill in your MySQL credentials, then click **Test Connection**. The connection string is saved to `db_config.txt` next to the executable for future launches.

Default values: `server=localhost`, `port=3306`, `database=library_db`, `user=root`, `password=` (empty).

---

## LibraryApp — features

### Book management
- **Add** a book via a dedicated form (title, author, ISBN, publication year, genre, shelf/row, availability, cover image URL, description).
- **Edit** any book by selecting it in the table and clicking Edit.
- **Delete** a book (with confirmation dialog).
- **Search** by title, author, genre, ISBN, or availability status — all filters work simultaneously.
- **Export** the current search results to a CSV file.

### Borrow system *(bonus)*
- **Borrow** an available book: select borrower name, email, and due date.
- **Return** a borrowed book: marks the record with the return date and makes the book available again.
- **Active borrows** and full **history** are shown on separate tabs.
- Overdue records are flagged automatically.

### Statistics *(bonus)*
- Summary panel showing total books, available, borrowed, and overdue counts.
- GDI+ bar chart displaying book count by genre.

### Cover images *(bonus)*
- Enter any image URL in the Add/Edit form and click **Load** to preview the cover in the form.
- The URL is saved to the database and shown when editing the book.

### UI
- Light and dark themes (toggle in Settings).
- DataGridView with alternating row colours, colour-coded availability status.
- Full input validation with user-friendly error messages.

---

## LibraryApp — architecture

```
LibraryApp/
├── Program.cs                  Entry point
├── Models/
│   ├── Book.cs                 Book entity
│   └── BorrowRecord.cs         Borrow record entity
├── Database/
│   └── DatabaseHelper.cs       All SQL queries (parameterised)
├── Forms/
│   ├── MainForm.cs             Main window — navigation, panels
│   ├── AddEditBookForm.cs       Add / edit book dialog
│   └── BorrowForm.cs           Borrow book dialog
├── Helpers/
│   ├── ThemeManager.cs         Light / dark theme colours & styling
│   └── ValidationHelper.cs     Input validation rules
└── SQL/
    └── library_schema.sql      Database creation script
```

**Data flow:** `MainForm` → `DatabaseHelper` → MySQL. All queries use parameterised `MySqlCommand` objects to prevent SQL injection. Transactions are used for multi-statement operations (borrow / return).

---

## WeatherApp — setup & run

### 1. Get a free API key

Register at [openweathermap.org/api](https://openweathermap.org/api) and copy your key. The free plan includes the 5-day / 3-hour forecast endpoint used by this app.

### 2. Create the .env file

Create a file named `.env` in the `WeatherApp/` folder (next to `WeatherApp.csproj`):

```
API_KEY=your_openweathermap_key_here
```

A template is provided in `.env.example`.

### 3. Run the app

```bash
cd WeatherApp
dotnet restore
dotnet run
```

---

## WeatherApp — features

### Search & forecast
- Enter any city name and press **Search** or hit **Enter**.
- Displays a 5-day forecast (OpenWeatherMap free tier limit) as interactive cards.
- Each card shows: temperature, min/max, weather icon, description, humidity bar, precipitation probability, wind speed.

### Detail panel
- Click any forecast card to open the detail panel.
- Shows a temperature chart (max/min curves) with hover interaction.
- Stats grid: feels-like temperature, pressure, visibility, sunrise/sunset, rain probability, humidity, cloud cover, wind speed.
- Animated wind compass.

### Favourites
- Add or remove the current city with the **Favourite** button.
- The **Favourites** tab lists all saved cities; click **Load Weather** to fetch the forecast for any of them.
- Favourites are persisted to `favorites.json` next to the executable.

### Interactive globe *(bonus)*
- The **Globe** tab shows a 3D orthographic projection of Earth.
- **Drag** to rotate; **scroll** to zoom.
- Favourite cities, the currently searched city, and world capitals are shown as markers.
- Clicking a marker navigates to the Search tab and fetches that city's forecast.
- At higher zoom levels, nearby cities are loaded from the OWM API.

### Dark mode *(bonus)*
- Toggle dark/light theme from the **Settings** tab.

---

## WeatherApp — architecture

```
WeatherApp/
├── Program.cs                  Entry point; loads .env before UI starts
├── Models/
│   └── WeatherModels.cs        WeatherDay, CityInfo, FavoriteCity, CitySearchResult
├── Services/
│   ├── WeatherService.cs       OpenWeatherMap API calls & JSON parsing
│   └── FavoritesService.cs     Read/write favorites.json
└── Forms/
    ├── MainForm.cs             Fields, constructor, projection math, coastlines
    ├── MainForm.UI.cs          BuildUI() — all control creation & layout
    ├── MainForm.Rendering.cs   GDI+ painting (globe, forecast cards, charts)
    └── MainForm.Actions.cs     Event handlers & async operations
```

**API used:** [OpenWeatherMap — 5 Day / 3 Hour Forecast](https://openweathermap.org/forecast5)  
Endpoint: `GET /data/2.5/forecast?q={city}&appid={key}&units=metric&cnt=40`

One data point per day is selected (preferring the 12:00 slot) to produce a clean 5-day view.

---

## Project structure

```
CSharpProjects/
├── CSharpProjects.sln
├── README.md
├── .gitignore
├── LibraryApp/
│   └── ...  (see LibraryApp architecture above)
└── WeatherApp/
    └── ...  (see WeatherApp architecture above)
```

### .gitignore

The following files are excluded from version control:

| File | Reason |
|------|--------|
| `bin/`, `obj/`, `.vs/` | Build artefacts |
| `db_config.txt` | Contains database password |
| `.env` | Contains API key |
| `favorites.json` | Local user data |
