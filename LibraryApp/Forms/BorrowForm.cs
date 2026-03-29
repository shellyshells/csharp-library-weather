using LibraryApp.Helpers;

namespace LibraryApp.Forms;

public sealed class BorrowForm : Form
{
    public string BorrowerName{get;private set;}="";
    public string BorrowerEmail{get;private set;}="";
    public DateTime DueDate{get;private set;}=DateTime.Now.AddDays(14);

    public BorrowForm(string bookTitle)
    {
        Text="Borrow Book";Size=new Size(660,520);MinimumSize=new Size(620,500);
        FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;
        StartPosition=FormStartPosition.CenterParent;Font=new Font("Segoe UI",10F);
        BackColor=ThemeManager.Bg;ForeColor=ThemeManager.Text;

        var body=new Panel{Dock=DockStyle.Fill,Padding=new Padding(24,18,24,12),AutoScroll=true};
        var form=new TableLayoutPanel{Dock=DockStyle.Top,AutoSize=true,ColumnCount=1,RowCount=7};
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100F));
        var lblBook=new Label{Text=bookTitle,Dock=DockStyle.Top,Height=40,AutoEllipsis=true,Font=new Font("Segoe UI Semibold",12F),ForeColor=ThemeManager.Accent,TextAlign=ContentAlignment.MiddleLeft,Margin=new Padding(0,0,0,10)};
        var lblName=new Label{Text="Borrower Name",AutoSize=true,Font=new Font("Segoe UI Semibold",9.5F),ForeColor=ThemeManager.TextMuted,Margin=new Padding(0,4,0,4)};
        var txtN=new TextBox{Dock=DockStyle.Top,Height=36,BorderStyle=BorderStyle.FixedSingle,Font=new Font("Segoe UI",10.5F),Margin=new Padding(0,0,0,10)};
        var lblEmail=new Label{Text="Email",AutoSize=true,Font=new Font("Segoe UI Semibold",9.5F),ForeColor=ThemeManager.TextMuted,Margin=new Padding(0,4,0,4)};
        var txtE=new TextBox{Dock=DockStyle.Top,Height=36,BorderStyle=BorderStyle.FixedSingle,Font=new Font("Segoe UI",10.5F),Margin=new Padding(0,0,0,10)};
        var lblDue=new Label{Text="Due Date",AutoSize=true,Font=new Font("Segoe UI Semibold",9.5F),ForeColor=ThemeManager.TextMuted,Margin=new Padding(0,4,0,4)};
        var dtp=new DateTimePicker{Dock=DockStyle.Top,Height=36,MinDate=DateTime.Today.AddDays(1),Value=DateTime.Today.AddDays(14),Format=DateTimePickerFormat.Custom,CustomFormat="dddd dd MMM yyyy",Font=new Font("Segoe UI",10.5F),Margin=new Padding(0,0,0,14)};
        form.Controls.Add(lblBook);form.Controls.Add(lblName);form.Controls.Add(txtN);form.Controls.Add(lblEmail);form.Controls.Add(txtE);form.Controls.Add(lblDue);form.Controls.Add(dtp);
        body.Controls.Add(form);
        var btnOk=new Button{Text="Confirm",Width=140,Height=38,FlatStyle=FlatStyle.Flat,BackColor=ThemeManager.Accent,ForeColor=Color.White,Font=new Font("Segoe UI Semibold",10F)};
        btnOk.FlatAppearance.BorderSize=0;
        btnOk.Click+=(_,_)=>{if(string.IsNullOrWhiteSpace(txtN.Text)){MessageBox.Show("Name required.");txtN.Focus();return;}if(string.IsNullOrWhiteSpace(txtE.Text)||!txtE.Text.Contains('@')){MessageBox.Show("Valid email required.");txtE.Focus();return;}BorrowerName=txtN.Text.Trim();BorrowerEmail=txtE.Text.Trim();DueDate=dtp.Value;DialogResult=DialogResult.OK;Close();};
        var btnNo=new Button{Text="Cancel",Width=110,Height=38,FlatStyle=FlatStyle.Flat,BackColor=ThemeManager.Surface,ForeColor=ThemeManager.Text,DialogResult=DialogResult.Cancel};
        btnNo.FlatAppearance.BorderColor=ThemeManager.Border;
        var bottom=new Panel{Dock=DockStyle.Bottom,Height=64,Padding=new Padding(24,12,24,12)};
        var buttons=new FlowLayoutPanel{Dock=DockStyle.Right,AutoSize=true,WrapContents=false,FlowDirection=FlowDirection.LeftToRight};
        buttons.Controls.Add(btnOk);buttons.Controls.Add(btnNo);bottom.Controls.Add(buttons);
        Controls.Add(body);Controls.Add(bottom);AcceptButton=btnOk;CancelButton=btnNo;
    }
}
