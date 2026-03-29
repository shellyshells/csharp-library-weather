namespace LibraryApp.Models;
public class BorrowRecord
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public string BookTitle { get; set; } = "";
    public string BorrowerName { get; set; } = "";
    public string BorrowerEmail { get; set; } = "";
    public DateTime BorrowDate { get; set; } = DateTime.Now;
    public DateTime DueDate { get; set; } = DateTime.Now.AddDays(14);
    public DateTime? ReturnDate { get; set; }
    public bool IsReturned => ReturnDate.HasValue;
    public bool IsOverdue => !IsReturned && DueDate < DateTime.Now;
    public string StatusLabel => IsReturned ? "Returned" : IsOverdue ? "OVERDUE" : "Active";
}
