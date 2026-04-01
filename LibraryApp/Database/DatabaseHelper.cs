// DatabaseHelper v1 -- read-only stub, full CRUD coming next commit
using MySql.Data.MySqlClient;
using LibraryApp.Models;
namespace LibraryApp.Database;
public class DatabaseHelper
{
    private string _cs;
    public DatabaseHelper(string cs) { _cs = cs; }
    public void UpdateConnectionString(string cs) => _cs = cs;
    private MySqlConnection Conn() => new(_cs);
    public bool TestConnection()
    { try { using var c = Conn(); c.Open(); return true; } catch { return false; } }
    public List<Book> GetBooks()
    {
        var books = new List<Book>();
        try { using var c = Conn(); c.Open(); using var cmd = new MySqlCommand("SELECT * FROM books ORDER BY title", c); using var r = cmd.ExecuteReader(); while (r.Read()) books.Add(Map(r)); }
        catch (Exception ex) { throw new Exception("GetBooks: " + ex.Message, ex); }
        return books;
    }
    private static Book Map(MySqlDataReader r) => new() { Id = r.GetInt32("id"), Title = r.GetString("title"), Author = r.GetString("author"), ISBN = r.IsDBNull(r.GetOrdinal("isbn")) ? "" : r.GetString("isbn"), PublicationYear = r.IsDBNull(r.GetOrdinal("publication_year")) ? 0 : r.GetInt32("publication_year"), Genre = r.IsDBNull(r.GetOrdinal("genre")) ? "" : r.GetString("genre"), Shelf = r.IsDBNull(r.GetOrdinal("shelf")) ? "" : r.GetString("shelf"), Row = r.IsDBNull(r.GetOrdinal("row_number")) ? "" : r.GetString("row_number"), IsAvailable = r.GetBoolean("is_available"), CoverUrl = r.IsDBNull(r.GetOrdinal("cover_url")) ? null : r.GetString("cover_url"), Description = r.IsDBNull(r.GetOrdinal("description")) ? null : r.GetString("description"), CreatedAt = r.GetDateTime("created_at") };
}
