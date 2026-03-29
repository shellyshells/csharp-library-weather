using LibraryApp.Models;
namespace LibraryApp.Helpers;
public static class ValidationHelper
{
    public static List<string> ValidateBook(Book b)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(b.Title)) errors.Add("Title is required.");
        if (string.IsNullOrWhiteSpace(b.Author)) errors.Add("Author is required.");
        if (b.PublicationYear != 0 && (b.PublicationYear < 1000 || b.PublicationYear > DateTime.Now.Year + 1))
            errors.Add($"Publication year must be between 1000 and {DateTime.Now.Year + 1}.");
        if (!string.IsNullOrWhiteSpace(b.ISBN) && !IsValidISBN(b.ISBN))
            errors.Add("ISBN format is invalid (expected 10 or 13 digits).");
        return errors;
    }
    public static bool IsValidISBN(string isbn)
    {
        isbn = isbn.Replace("-", "").Replace(" ", "");
        return isbn.Length == 10 || isbn.Length == 13;
    }
}
