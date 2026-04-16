using MySql.Data.MySqlClient;
using LibraryApp.Models;
namespace LibraryApp.Database;
 
public class DatabaseHelper
{
    private string _cs;
    public DatabaseHelper(string cs) { _cs = cs; }
    public void UpdateConnectionString(string cs) => _cs = cs;
    private MySqlConnection Conn() => new(_cs);
 
    public bool TestConnection() { try { using var c = Conn(); c.Open(); return true; } catch { return false; } }
 
    public List<Book> GetBooks(string? title = null, string? author = null, string? genre = null, string? isbn = null, bool? available = null)
    {
        var books = new List<Book>(); var w = new List<string>(); var p = new List<MySqlParameter>();
        void F(string col, string a, string? v) { if (!string.IsNullOrWhiteSpace(v)) { w.Add($"{col} LIKE @{a}"); p.Add(new($"@{a}", $"%{v.Trim()}%")); } }
        F("title","title",title); F("author","author",author); F("genre","genre",genre); F("isbn","isbn",isbn);
        if (available.HasValue) { w.Add("is_available=@av"); p.Add(new("@av", available.Value ? 1 : 0)); }
        string sql = $"SELECT * FROM books {(w.Any() ? "WHERE " + string.Join(" AND ", w) : "")} ORDER BY title";
        try { using var c = Conn(); c.Open(); using var cmd = new MySqlCommand(sql, c); cmd.Parameters.AddRange(p.ToArray()); using var r = cmd.ExecuteReader(); while (r.Read()) books.Add(Map(r)); }
        catch (Exception ex) { throw new Exception("GetBooks: " + ex.Message, ex); }
        return books;
    }
 
    public int AddBook(Book b)
    {
        const string sql = "INSERT INTO books (title,author,isbn,publication_year,genre,shelf,`row_number`,is_available,cover_url,description) VALUES (@t,@a,@i,@y,@g,@s,@r,@av,@cu,@d); SELECT LAST_INSERT_ID();";
        try { using var c = Conn(); c.Open(); using var cmd = new MySqlCommand(sql, c); Bind(cmd, b); return Convert.ToInt32(cmd.ExecuteScalar()); }
        catch (Exception ex) { throw new Exception("AddBook: " + ex.Message, ex); }
    }
 
    public bool UpdateBook(Book b)
    {
        const string sql = "UPDATE books SET title=@t,author=@a,isbn=@i,publication_year=@y,genre=@g,shelf=@s,`row_number`=@r,is_available=@av,cover_url=@cu,description=@d WHERE id=@id";
        try { using var c = Conn(); c.Open(); using var cmd = new MySqlCommand(sql, c); Bind(cmd, b); cmd.Parameters.AddWithValue("@id", b.Id); return cmd.ExecuteNonQuery() > 0; }
        catch (Exception ex) { throw new Exception("UpdateBook: " + ex.Message, ex); }
    }
 
    public bool DeleteBook(int id)
    {
        try { using var c = Conn(); c.Open(); using var cmd = new MySqlCommand("DELETE FROM books WHERE id=@id", c); cmd.Parameters.AddWithValue("@id", id); return cmd.ExecuteNonQuery() > 0; }
        catch (Exception ex) { throw new Exception("DeleteBook: " + ex.Message, ex); }
    }
 
    public List<BorrowRecord> GetBorrowRecords(bool activeOnly = false)
    {
        var list = new List<BorrowRecord>();
        string sql = "SELECT br.*, b.title AS book_title FROM borrow_records br JOIN books b ON br.book_id=b.id" + (activeOnly ? " WHERE br.return_date IS NULL" : "") + " ORDER BY br.borrow_date DESC";
        try
        {
            using var c = Conn(); c.Open(); using var cmd = new MySqlCommand(sql, c); using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(new BorrowRecord { Id = r.GetInt32("id"), BookId = r.GetInt32("book_id"), BookTitle = r.GetString("book_title"), BorrowerName = r.GetString("borrower_name"), BorrowerEmail = r.GetString("borrower_email"), BorrowDate = r.GetDateTime("borrow_date"), DueDate = r.GetDateTime("due_date"), ReturnDate = r.IsDBNull(r.GetOrdinal("return_date")) ? null : r.GetDateTime("return_date") });
        }
        catch (Exception ex) { throw new Exception("GetBorrowRecords: " + ex.Message, ex); }
        return list;
    }
 
    public int BorrowBook(int bookId, string name, string email, DateTime due)
    {
        using var c = Conn(); c.Open(); using var tx = c.BeginTransaction();
        try
        {
            var ins = new MySqlCommand("INSERT INTO borrow_records (book_id,borrower_name,borrower_email,due_date) VALUES (@b,@n,@e,@d); SELECT LAST_INSERT_ID();", c, tx);
            ins.Parameters.AddWithValue("@b", bookId); ins.Parameters.AddWithValue("@n", name); ins.Parameters.AddWithValue("@e", email); ins.Parameters.AddWithValue("@d", due);
            int id = Convert.ToInt32(ins.ExecuteScalar());
            var upd = new MySqlCommand("UPDATE books SET is_available=0 WHERE id=@id", c, tx); upd.Parameters.AddWithValue("@id", bookId); upd.ExecuteNonQuery();
            tx.Commit(); return id;
        }
        catch { tx.Rollback(); throw; }
    }
 
    // FIX: was using raw string interpolation — now uses parameterised queries to prevent SQL injection
    public bool ReturnBook(int recordId, int bookId)
    {
        using var c = Conn(); c.Open(); using var tx = c.BeginTransaction();
        try
        {
            var updRecord = new MySqlCommand("UPDATE borrow_records SET return_date=NOW() WHERE id=@rid", c, tx);
            updRecord.Parameters.AddWithValue("@rid", recordId);
            updRecord.ExecuteNonQuery();
 
            var updBook = new MySqlCommand("UPDATE books SET is_available=1 WHERE id=@bid", c, tx);
            updBook.Parameters.AddWithValue("@bid", bookId);
            updBook.ExecuteNonQuery();
 
            tx.Commit(); return true;
        }
        catch { tx.Rollback(); throw; }
    }
 
    public Dictionary<string, int> GetStats()
    {
        var d = new Dictionary<string, int> { ["total"]=0, ["available"]=0, ["borrowed"]=0, ["overdue"]=0 };
        try
        {
            using var c = Conn(); c.Open();
            using var cmd = new MySqlCommand("SELECT COUNT(*) AS t, SUM(is_available=1) AS a, SUM(is_available=0) AS b, (SELECT COUNT(*) FROM borrow_records WHERE return_date IS NULL AND due_date<NOW()) AS o FROM books", c);
            using var r = cmd.ExecuteReader();
            if (r.Read()) { d["total"]=r.IsDBNull(0)?0:r.GetInt32(0); d["available"]=r.IsDBNull(1)?0:Convert.ToInt32(r[1]); d["borrowed"]=r.IsDBNull(2)?0:Convert.ToInt32(r[2]); d["overdue"]=r.IsDBNull(3)?0:Convert.ToInt32(r[3]); }
        } catch { }
        return d;
    }
 
    public List<(string Genre, int Count)> GetBooksByGenre()
    {
        var list = new List<(string, int)>();
        try { using var c = Conn(); c.Open(); using var cmd = new MySqlCommand("SELECT COALESCE(genre,'Unknown'),COUNT(*) FROM books GROUP BY genre ORDER BY COUNT(*) DESC LIMIT 10", c); using var r = cmd.ExecuteReader(); while (r.Read()) list.Add((r.GetString(0), r.GetInt32(1))); }
        catch { }
        return list;
    }
 
    public static string ToCsv(IEnumerable<Book> books)
    {
        var sb = new System.Text.StringBuilder(); sb.AppendLine("ID,Title,Author,ISBN,Year,Genre,Shelf,Row,Available");
        static string E(string? s)
        {
            s ??= string.Empty;
            bool needsQuotes = s.Contains(',') || s.Contains('"') || s.Contains('\r') || s.Contains('\n');
            return needsQuotes ? "\"" + s.Replace("\"", "\"\"") + "\"" : s;
        }

        foreach (var b in books)
            sb.AppendLine($"{b.Id},{E(b.Title)},{E(b.Author)},{E(b.ISBN)},{b.PublicationYear},{E(b.Genre)},{E(b.Shelf)},{E(b.Row)},{b.IsAvailable}");
        return sb.ToString();
    }
 
    private static Book Map(MySqlDataReader r) => new()
    {
        Id = r.GetInt32("id"), Title = r.GetString("title"), Author = r.GetString("author"),
        ISBN = r.IsDBNull(r.GetOrdinal("isbn")) ? "" : r.GetString("isbn"),
        PublicationYear = r.IsDBNull(r.GetOrdinal("publication_year")) ? 0 : r.GetInt32("publication_year"),
        Genre = r.IsDBNull(r.GetOrdinal("genre")) ? "" : r.GetString("genre"),
        Shelf = r.IsDBNull(r.GetOrdinal("shelf")) ? "" : r.GetString("shelf"),
        Row = r.IsDBNull(r.GetOrdinal("row_number")) ? "" : r.GetString("row_number"),
        IsAvailable = r.GetBoolean("is_available"),
        CoverUrl = r.IsDBNull(r.GetOrdinal("cover_url")) ? null : r.GetString("cover_url"),
        Description = r.IsDBNull(r.GetOrdinal("description")) ? null : r.GetString("description"),
        CreatedAt = r.GetDateTime("created_at")
    };
 
    // FIX: empty string ISBN stored as NULL so the UNIQUE constraint doesn't
    // collide when multiple books have no ISBN entered.
    private static void Bind(MySqlCommand cmd, Book b)
    {
        cmd.Parameters.AddWithValue("@t", b.Title);
        cmd.Parameters.AddWithValue("@a", b.Author);
        // Store empty ISBN as NULL — MySQL UNIQUE allows multiple NULLs
        cmd.Parameters.AddWithValue("@i", string.IsNullOrWhiteSpace(b.ISBN) ? (object)DBNull.Value : b.ISBN.Trim());
        cmd.Parameters.AddWithValue("@y", b.PublicationYear == 0 ? (object)DBNull.Value : b.PublicationYear);
        cmd.Parameters.AddWithValue("@g", b.Genre);
        cmd.Parameters.AddWithValue("@s", b.Shelf);
        cmd.Parameters.AddWithValue("@r", b.Row);
        cmd.Parameters.AddWithValue("@av", b.IsAvailable ? 1 : 0);
        cmd.Parameters.AddWithValue("@cu", (object?)b.CoverUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@d",  (object?)b.Description ?? DBNull.Value);
    }
}