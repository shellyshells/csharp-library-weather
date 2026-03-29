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

    public AddEditBookForm(Book? existing=null)
    {
        _existing=existing;
        Text=existing==null?"Add New Book":"Edit Book";
        Size=new Size(560,620); MinimumSize=new Size(560,620);
        FormBorderStyle=FormBorderStyle.FixedDialog; MaximizeBox=false;
        StartPosition=FormStartPosition.CenterParent;
        Font=new Font("Segoe UI",10F); BackColor=ThemeManager.Bg; ForeColor=ThemeManager.Text;

        var scroll=new Panel{Dock=DockStyle.Fill,AutoScroll=true,Padding=new Padding(20,16,20,16)};
        var inner=new Panel{Left=0,Top=0,Width=500};
        int y=0;
        TextBox MakeField(string label,bool multi=false)
        {
            inner.Controls.Add(new Label{Text=label,Left=0,Top=y,Width=480,Height=18,Font=new Font("Segoe UI Semibold",9F),ForeColor=ThemeManager.TextMuted});
            y+=22;
            var tb=new TextBox{Left=0,Top=y,Width=480,BorderStyle=BorderStyle.FixedSingle,Font=new Font("Segoe UI",10.5F),BackColor=ThemeManager.IsDark?Color.FromArgb(42,42,60):Color.White,ForeColor=ThemeManager.Text};
            if(multi){tb.Multiline=true;tb.Height=55;tb.ScrollBars=ScrollBars.Vertical;y+=62;}else{tb.Height=28;y+=36;}
            inner.Controls.Add(tb); return tb;
        }
        txtTitle=MakeField("Title *"); txtAuthor=MakeField("Author *"); txtISBN=MakeField("ISBN");
        txtYear=MakeField("Publication Year"); txtGenre=MakeField("Genre"); txtShelf=MakeField("Shelf (Rayon)");
        txtRow=MakeField("Row (Etagere)"); txtCoverUrl=MakeField("Cover Image URL"); txtDesc=MakeField("Description",multi:true);

        chkAvail=new CheckBox{Text="  Available for borrowing",Left=0,Top=y,Checked=true,AutoSize=true,Font=new Font("Segoe UI",10F),ForeColor=ThemeManager.Text};
        inner.Controls.Add(chkAvail); y+=35;
        pic=new PictureBox{Left=340,Top=0,Width=140,Height=180,SizeMode=PictureBoxSizeMode.Zoom,BackColor=ThemeManager.IsDark?Color.FromArgb(42,42,60):Color.FromArgb(245,245,250),BorderStyle=BorderStyle.FixedSingle};
        inner.Controls.Add(pic); inner.Height=y+10; scroll.Controls.Add(inner);

        var bottom=new Panel{Dock=DockStyle.Bottom,Height=56,Padding=new Padding(20,10,20,10)};
        var btnSave=new Button{Text=existing==null?"Add Book":"Save Changes",Width=140,Height=36,Left=200,Top=10,Anchor=AnchorStyles.Bottom|AnchorStyles.Right,FlatStyle=FlatStyle.Flat,BackColor=ThemeManager.Accent,ForeColor=Color.White,Font=new Font("Segoe UI Semibold",10F)};
        btnSave.FlatAppearance.BorderSize=0; btnSave.Click+=(_,_)=>Save();
        var btnCancel=new Button{Text="Cancel",Width=90,Height=36,Left=350,Top=10,Anchor=AnchorStyles.Bottom|AnchorStyles.Right,FlatStyle=FlatStyle.Flat,BackColor=ThemeManager.Surface,ForeColor=ThemeManager.Text,DialogResult=DialogResult.Cancel};
        btnCancel.FlatAppearance.BorderColor=ThemeManager.Border;
        bottom.Controls.AddRange(new Control[]{btnSave,btnCancel});
        Controls.AddRange(new Control[]{scroll,bottom});
        AcceptButton=btnSave; CancelButton=btnCancel;
        if(_existing!=null)Populate(_existing);
    }

    private void Save()
    {
        var book=new Book{Id=_existing?.Id??0,Title=txtTitle.Text.Trim(),Author=txtAuthor.Text.Trim(),ISBN=txtISBN.Text.Trim(),PublicationYear=int.TryParse(txtYear.Text,out int yr)?yr:0,Genre=txtGenre.Text.Trim(),Shelf=txtShelf.Text.Trim(),Row=txtRow.Text.Trim(),IsAvailable=chkAvail.Checked,CoverUrl=string.IsNullOrWhiteSpace(txtCoverUrl.Text)?null:txtCoverUrl.Text.Trim(),Description=string.IsNullOrWhiteSpace(txtDesc.Text)?null:txtDesc.Text.Trim()};
        var errs=ValidationHelper.ValidateBook(book);
        if(errs.Any()){MessageBox.Show(string.Join("\n",errs),"Validation",MessageBoxButtons.OK,MessageBoxIcon.Warning);return;}
        Result=book;DialogResult=DialogResult.OK;Close();
    }

    private void Populate(Book b){txtTitle.Text=b.Title;txtAuthor.Text=b.Author;txtISBN.Text=b.ISBN;txtYear.Text=b.PublicationYear>0?b.PublicationYear.ToString():"";txtGenre.Text=b.Genre;txtShelf.Text=b.Shelf;txtRow.Text=b.Row;txtCoverUrl.Text=b.CoverUrl??"";txtDesc.Text=b.Description??"";;chkAvail.Checked=b.IsAvailable;}
}
