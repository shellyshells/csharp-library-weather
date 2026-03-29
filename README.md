# CSharpProjects - LibraryManager Pro and WeatherPro
Group: Michel Mustafov + Edwin Wehbe

## Run LibraryApp
mysql -u root -p < LibraryApp/SQL/library_schema.sql
cd LibraryApp && dotnet run

## Run WeatherApp
Add API_KEY=your_key to WeatherApp/.env
cd WeatherApp && dotnet run
