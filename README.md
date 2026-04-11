# CSharpProjects - LibraryManager Pro and WeatherPro
Group: Michel Mustafov + Edwin Wehbe

## Prerequisites
- .NET 8 SDK (Windows only)
- MySQL 8+ (LibraryApp only)
- OpenWeatherMap free API key (WeatherApp only)

## Run LibraryApp
  mysql -u root -p < LibraryApp/SQL/library_schema.sql
  cd LibraryApp && dotnet restore && dotnet run

## Run WeatherApp
  echo API_KEY=your_key_here > WeatherApp\.env
  cd WeatherApp && dotnet restore && dotnet run
  Free key at https://openweathermap.org/api

## LibraryApp Features
- Add / Edit / Delete books (title, author, ISBN, genre, shelf, row)
- Search by Title, Author, Genre, ISBN, availability
- Borrow / Return with due dates and overdue detection
- Statistics panel with GDI+ bar chart by genre
- Dark / Light mode, CSV export

## WeatherApp Features
- 5-day forecast via OpenWeatherMap API
- Detail panel: temperature chart, stats grid, wind compass
- Favourites saved to favorites.json
- Interactive globe: drag to pan, scroll to zoom
- Dark / Light mode
