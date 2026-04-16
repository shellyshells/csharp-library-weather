using LibraryApp.Helpers;
using LibraryApp.Models;
 
namespace LibraryApp.Forms;
 
public sealed class AddEditBookForm : Form
{
    public Book? Result { get; private set; }
    private readonly Book? _existing;
    private TextBox txtTitle=null!,txtAuthor=null!,txtISBN=null!,txtYear=null!,txtGenre=null!,txtShelf=null!,txtRow=null!,txtCoverUrl=null!,txtDesc=null!;
    private CheckBox chkAvail=null!;
    private PictureBox pic=null!;
    private Label lblCoverStatus=null!;
 
    private static readonly HttpClient _http = new();
 
    public AddEditBookForm(Book? existing=null)
    {
        _existing=existing;
        Text=existing==null?"Add New Book":"Edit Book";
        Size=new Size(1240,980); MinimumSize=new Size(1240,980);
        FormBorderStyle=FormBorderStyle.FixedDialog; MaximizeBox=false;
        StartPosition=FormStartPosition.CenterParent;
        Font=new Font("Segoe UI",10F); BackColor=ThemeManager.Bg; ForeColor=ThemeManager.Text;
 
        var scroll=new Panel{Dock=DockStyle.Fill,AutoScroll=false,Padding=new Padding(25,16,25,16)};
        var inner=new Panel{Left=0,Top=0,Width=1170};
        int y=0;
        TextBox MakeField(string label,bool multi=false)
        {
            inner.Controls.Add(new Label{Text=label,Left=0,Top=y,Width=760,Height=28,Font=new Font("Segoe UI Semibold",9F),ForeColor=ThemeManager.TextMuted,TextAlign=ContentAlignment.TopLeft});
            y+=36;
            var tb=new TextBox{Left=0,Top=y,Width=760,BorderStyle=BorderStyle.FixedSingle,Font=new Font("Segoe UI",10.5F),BackColor=ThemeManager.IsDark?Color.FromArgb(42,42,60):Color.White,ForeColor=ThemeManager.Text};
            if(multi){tb.Multiline=true;tb.BorderStyle=BorderStyle.Fixed3D;tb.Height=162;tb.ScrollBars=ScrollBars.Vertical;y+=202;}else{tb.Height=28;y+=48;}
            inner.Controls.Add(tb); return tb;
        }
        txtTitle=MakeField("Title *"); txtAuthor=MakeField("Author *"); txtISBN=MakeField("ISBN");
        txtYear=MakeField("Publication Year"); txtGenre=MakeField("Genre"); txtShelf=MakeField("Shelf (Rayon)");
        txtRow=MakeField("Row (Etagere)");
 
        // Cover URL field with Load button on the same row
        inner.Controls.Add(new Label{Text="Cover Image URL",Left=0,Top=y,Width=760,Height=28,Font=new Font("Segoe UI Semibold",9F),ForeColor=ThemeManager.TextMuted,TextAlign=ContentAlignment.TopLeft});
        y+=36;
        txtCoverUrl=new TextBox{Left=0,Top=y,Width=648,Height=28,BorderStyle=BorderStyle.FixedSingle,Font=new Font("Segoe UI",10.5F),BackColor=ThemeManager.IsDark?Color.FromArgb(42,42,60):Color.White,ForeColor=ThemeManager.Text};
        var loadBase=ThemeManager.Accent;
        var btnLoad=new Button
        {
            Text="Load",
            Left=656,
            Top=y,
            Width=112,
            Height=48,
            FlatStyle=FlatStyle.Flat,
            BackColor=loadBase,
            ForeColor=Color.White,
            Cursor=Cursors.Hand,
            Font=new Font("Segoe UI Semibold",9.5F)
        };
        btnLoad.FlatAppearance.BorderSize=1;
        btnLoad.FlatAppearance.BorderColor=ControlPaint.Dark(loadBase,0.18f);
        btnLoad.FlatAppearance.MouseOverBackColor=ControlPaint.Light(loadBase,0.10f);
        btnLoad.FlatAppearance.MouseDownBackColor=ControlPaint.Dark(loadBase,0.08f);
        btnLoad.Click+=async(_,_)=>await LoadCoverImageAsync();
        // Also load when the user presses Enter inside the URL box
        txtCoverUrl.KeyDown+=async(_,e)=>{if(e.KeyCode==Keys.Enter){e.SuppressKeyPress=true;await LoadCoverImageAsync();}};
        inner.Controls.Add(txtCoverUrl);
        inner.Controls.Add(btnLoad);
        y+=48;
 
        // Small status label under the URL field
        lblCoverStatus=new Label{Left=0,Top=y,Width=760,Height=16,Font=new Font("Segoe UI",8F),ForeColor=ThemeManager.TextMuted,Text="",TextAlign=ContentAlignment.TopLeft};
        inner.Controls.Add(lblCoverStatus);
        y+=28;
 
        txtDesc=MakeField("Description",multi:true);
 
        chkAvail=new CheckBox{Text="  Available for borrowing",Left=0,Top=y,Checked=true,AutoSize=true,Font=new Font("Segoe UI",10F),ForeColor=ThemeManager.Text};
        inner.Controls.Add(chkAvail); y+=56;
 
        // Cover preview box — in the right column
        pic=new PictureBox
        {
            Left=840,Top=180,Width=280,Height=380,
            SizeMode=PictureBoxSizeMode.Zoom,
            BackColor=ThemeManager.IsDark?Color.FromArgb(42,42,60):Color.FromArgb(245,245,250),
            BorderStyle=BorderStyle.FixedSingle
        };
        var picLabel=new Label{Text="Cover preview",Left=840,Top=570,AutoSize=true,Font=new Font("Segoe UI",8F),ForeColor=ThemeManager.TextMuted,TextAlign=ContentAlignment.TopLeft};
        inner.Controls.Add(pic);
        inner.Controls.Add(picLabel);
        inner.Height=Math.Max(y+20,picLabel.Bottom+20); scroll.Controls.Add(inner);
 
        var bottom=new Panel{Dock=DockStyle.Bottom,Height=70,Padding=new Padding(20,15,20,15)};
        var btnSave=new Button{Text=existing==null?"Add Book":"Save Changes",Width=140,Height=36,Left=200,Top=10,Anchor=AnchorStyles.Bottom|AnchorStyles.Right,FlatStyle=FlatStyle.Flat,BackColor=ThemeManager.Accent,ForeColor=Color.White,Font=new Font("Segoe UI Semibold",10F)};
        btnSave.FlatAppearance.BorderSize=0; btnSave.Click+=(_,_)=>Save();
        var btnCancel=new Button{Text="Cancel",Width=90,Height=36,Left=350,Top=10,Anchor=AnchorStyles.Bottom|AnchorStyles.Right,FlatStyle=FlatStyle.Flat,BackColor=ThemeManager.Surface,ForeColor=ThemeManager.Text,DialogResult=DialogResult.Cancel};
        btnCancel.FlatAppearance.BorderColor=ThemeManager.Border;
        bottom.Controls.AddRange(new Control[]{btnSave,btnCancel});
        Controls.AddRange(new Control[]{scroll,bottom});
        AcceptButton=btnSave; CancelButton=btnCancel;
 
        if(_existing!=null) Populate(_existing);
    }
 
    // ── Load cover image from URL ──────────────────────────────────────
    private async Task LoadCoverImageAsync()
    {
        string url = txtCoverUrl.Text.Trim();
        if(string.IsNullOrWhiteSpace(url))
        {
            pic.Image=null;
            lblCoverStatus.Text="";
            return;
        }
        lblCoverStatus.ForeColor=ThemeManager.TextMuted;
        lblCoverStatus.Text="Loading…";
        try
        {
            byte[] data = await _http.GetByteArrayAsync(url);
            using var ms = new MemoryStream(data);
            var img = Image.FromStream(ms);
            // Marshal back to UI thread
            if(!IsDisposed)
                Invoke(()=>{
                    pic.Image=img;
                    lblCoverStatus.ForeColor=ThemeManager.Success;
                    lblCoverStatus.Text="✔ Image loaded";
                });
        }
        catch
        {
            if(!IsDisposed)
                Invoke(()=>{
                    pic.Image=null;
                    lblCoverStatus.ForeColor=ThemeManager.Danger;
                    lblCoverStatus.Text="✖ Could not load image";
                });
        }
    }
 
    private void Save()
    {
        var book=new Book{Id=_existing?.Id??0,Title=txtTitle.Text.Trim(),Author=txtAuthor.Text.Trim(),ISBN=txtISBN.Text.Trim(),PublicationYear=int.TryParse(txtYear.Text,out int yr)?yr:0,Genre=txtGenre.Text.Trim(),Shelf=txtShelf.Text.Trim(),Row=txtRow.Text.Trim(),IsAvailable=chkAvail.Checked,CoverUrl=string.IsNullOrWhiteSpace(txtCoverUrl.Text)?null:txtCoverUrl.Text.Trim(),Description=string.IsNullOrWhiteSpace(txtDesc.Text)?null:txtDesc.Text.Trim()};
        var errs=ValidationHelper.ValidateBook(book);
        if(errs.Any()){MessageBox.Show(string.Join("\n",errs),"Validation",MessageBoxButtons.OK,MessageBoxIcon.Warning);return;}
        Result=book;DialogResult=DialogResult.OK;Close();
    }
 
    private async void Populate(Book b)
    {
        txtTitle.Text=b.Title; txtAuthor.Text=b.Author; txtISBN.Text=b.ISBN;
        txtYear.Text=b.PublicationYear>0?b.PublicationYear.ToString():"";
        txtGenre.Text=b.Genre; txtShelf.Text=b.Shelf; txtRow.Text=b.Row;
        txtCoverUrl.Text=b.CoverUrl??""; txtDesc.Text=b.Description??"";
        chkAvail.Checked=b.IsAvailable;
        // Auto-load cover when editing an existing book that has a URL
        if(!string.IsNullOrWhiteSpace(b.CoverUrl))
            await LoadCoverImageAsync();
    }
}