// Book model -- first draft, missing CoverUrl and Description
namespace LibraryApp.Models;
public class Book
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public string ISBN { get; set; } = "";
    public int PublicationYear { get; set; }
    public string Genre { get; set; } = "";
    public string Shelf { get; set; } = "";
    public string Row { get; set; } = "";
    public bool IsAvailable { get; set; } = true;
    public string AvailabilityLabel => IsAvailable ? "Available" : "Borrowed";
    public string Location => $"{Shelf} / {Row}";
}
