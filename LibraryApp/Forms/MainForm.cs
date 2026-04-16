using LibraryApp.Database;
using LibraryApp.Helpers;
using LibraryApp.Models;

namespace LibraryApp.Forms;

public sealed class MainForm : Form
{
    private DatabaseHelper _db = null!;
    private const string DefaultConn = "server=localhost;user=root;database=library_db;port=3306;password=";
    private List<Book> _currentBooks = new();

    private Panel pnlBooks=null!,pnlBorrow=null!,pnlStats=null!,pnlSettings=null!;
    private readonly List<Panel> _panels=new();
    private Button navBooks=null!,navBorrow=null!,navStats=null!,navSettings=null!;
    private readonly List<Button> _navButtons=new();

    private DataGridView dgvBooks=null!;
    private TextBox txtTitle=null!,txtAuthor=null!,txtGenre=null!,txtISBN=null!;
    private ComboBox cmbStatus=null!;

    private DataGridView dgvActive=null!,dgvHistory=null!;
    private TabControl tabBorrow=null!;

    private int _statTotal,_statAvailable,_statBorrowed,_statOverdue;
    private Panel chartPanel=null!;
    private List<(string Genre,int Count)> _chartData=new();

    private TextBox txtServer=null!,txtPort=null!,txtDbName=null!,txtUser=null!,txtPass=null!;
    private Label lblConnResult=null!;
    private Button btnTheme=null!;

    private ToolStripStatusLabel lblStatus=null!,lblDbIndicator=null!;

    public MainForm()
    {
        _db=new DatabaseHelper(LoadConfig()??DefaultConn);
        Build();
        ThemeManager.Apply(this);
        if(File.Exists(CfgPath))RefreshBooks();
        else SetStatus("Configure your database in Settings to get started.");
        UpdateDbIndicator();
    }

    private void Build()
    {
        Text="LibraryManager Pro"; Size=new Size(1140,740); MinimumSize=new Size(960,640);
        StartPosition=FormStartPosition.CenterScreen; Font=new Font("Segoe UI",10F); BackColor=ThemeManager.Bg;

        var side=new Panel{Dock=DockStyle.Left,Width=212,BackColor=ThemeManager.Sidebar,Tag="side"};
        var logoPanel=new Panel{Dock=DockStyle.Top,Height=78,BackColor=Color.Transparent,Tag="side",Padding=new Padding(12,10,10,10)};
        var logoTitle=new Label{Dock=DockStyle.Fill,Text="Library Manager",Font=new Font("Segoe UI Semibold",11F),ForeColor=Color.White,TextAlign=ContentAlignment.MiddleLeft,Tag="side",AutoEllipsis=false};
        logoPanel.Controls.Add(logoTitle);
        var sep=new Panel{Dock=DockStyle.Top,Height=1,BackColor=Color.FromArgb(65,55,100),Tag="side"};

        navBooks=NavBtn("\U0001F4DA   Books");
        navBorrow=NavBtn("\U0001F4CB   Borrow");
        navStats=NavBtn("\U0001F4CA   Statistics");
        navSettings=NavBtn("\u2699\uFE0F   Settings");
        _navButtons.AddRange(new[]{navBooks,navBorrow,navStats,navSettings});

        var dbLbl=new Label{Dock=DockStyle.Bottom,Height=30,ForeColor=Color.FromArgb(120,120,155),Font=new Font("Segoe UI",8.5F),TextAlign=ContentAlignment.MiddleCenter,Tag="side",Text="\u25CF Database"};
        side.Controls.Add(dbLbl);
        side.Controls.Add(navSettings);side.Controls.Add(navStats);side.Controls.Add(navBorrow);side.Controls.Add(navBooks);
        side.Controls.Add(sep);side.Controls.Add(logoPanel);

        var content=new Panel{Dock=DockStyle.Fill,Padding=new Padding(16,12,16,8)};
        BuildBooks();BuildBorrow();BuildStats();BuildSettings();
        _panels.AddRange(new[]{pnlBooks,pnlBorrow,pnlStats,pnlSettings});
        foreach(var p in _panels){p.Dock=DockStyle.Fill;content.Controls.Add(p);}

        var bar=new StatusStrip{SizingGrip=false};
        lblStatus=new ToolStripStatusLabel("Ready"){Spring=true,TextAlign=ContentAlignment.MiddleLeft};
        lblDbIndicator=new ToolStripStatusLabel("DB: checking...");
        bar.Items.AddRange(new ToolStripItem[]{lblStatus,lblDbIndicator});

        Controls.AddRange(new Control[]{content,side,bar});

        navBooks.Click+=(_,_)=>{Activate(pnlBooks,navBooks);RefreshBooks();};
        navBorrow.Click+=(_,_)=>{Activate(pnlBorrow,navBorrow);RefreshBorrow();};
        navStats.Click+=(_,_)=>{Activate(pnlStats,navStats);RefreshStats();};
        navSettings.Click+=(_,_)=>Activate(pnlSettings,navSettings);
        Activate(pnlBooks,navBooks);
    }

    private void BuildBooks()
    {
        pnlBooks=new Panel{BackColor=ThemeManager.Bg};
        var header=PageHeader("Books");
        var search=new Panel{Dock=DockStyle.Top,Height=72,BackColor=ThemeManager.Surface,Tag="card",Padding=new Padding(10)};
        txtTitle=SBox("Title...");txtAuthor=SBox("Author...");txtGenre=SBox("Genre...");txtISBN=SBox("ISBN...");
        cmbStatus=new ComboBox{Width=100,Height=32,DropDownStyle=ComboBoxStyle.DropDownList,Font=new Font("Segoe UI",9F),Margin=new Padding(0,0,10,0)};
        cmbStatus.Items.AddRange(new object[]{"All","Available","Borrowed"});cmbStatus.SelectedIndex=0;
        var rightGroup=new FlowLayoutPanel{AutoSize=false,Width=0,Height=40,WrapContents=false,FlowDirection=FlowDirection.LeftToRight,Margin=new Padding(0),Padding=new Padding(0)};
        var bSearch=ActBtn("Search",ThemeManager.Accent,Color.White,"accent");bSearch.Width=108;bSearch.Height=40;bSearch.Margin=new Padding(0,0,10,0);bSearch.TextAlign=ContentAlignment.MiddleCenter;bSearch.Font=new Font("Segoe UI Semibold",9F);
        var bClear=ActBtn("Clear",ThemeManager.Surface,ThemeManager.Text);bClear.Width=90;bClear.Height=40;bClear.Margin=new Padding(0);bClear.TextAlign=ContentAlignment.MiddleCenter;bClear.Font=new Font("Segoe UI Semibold",9F);
        bSearch.Click+=(_,_)=>RefreshBooks();
        bClear.Click+=(_,_)=>{txtTitle.Text=txtAuthor.Text=txtGenre.Text=txtISBN.Text=string.Empty;cmbStatus.SelectedIndex=0;RefreshBooks();};
        foreach(var tb in new[]{txtTitle,txtAuthor,txtGenre,txtISBN})
            tb.KeyDown+=(_,e)=>{if(e.KeyCode==Keys.Enter){RefreshBooks();e.SuppressKeyPress=true;}};
        rightGroup.Controls.AddRange(new Control[]{cmbStatus,bSearch,bClear});
        search.Controls.AddRange(new Control[]{txtTitle,txtAuthor,txtGenre,txtISBN,rightGroup});
        search.Resize+=(_,_)=>{int x=10;int groupW=cmbStatus.Width+bSearch.Width+bClear.Width+20;int w=Math.Max(120,(search.ClientSize.Width-20-groupW-24)/4);foreach(var tb in new[]{txtTitle,txtAuthor,txtGenre,txtISBN}){tb.Left=x;tb.Top=10;tb.Width=w;x+=w+8;}rightGroup.Width=groupW;rightGroup.Left=search.ClientSize.Width-groupW-10;rightGroup.Top=10;};

        dgvBooks=MkGrid();
        dgvBooks.Columns.AddRange(MkCol("Id","ID",60,false),MkCol("Title","Title",210),MkCol("Author","Author",170),MkCol("ISBN","ISBN",120),MkCol("Genre","Genre",110),MkCol("AvailabilityLabel","Status",95));

        var actionBar=new Panel{Dock=DockStyle.Top,Height=64,Padding=new Padding(0,10,0,8)};
        var flow=new FlowLayoutPanel{Dock=DockStyle.Left,AutoSize=true,WrapContents=false,FlowDirection=FlowDirection.LeftToRight};
        var bAdd=ActBtn("+ Add",ThemeManager.Accent,Color.White,"accent");bAdd.Width=100;bAdd.Height=42;bAdd.Margin=new Padding(0,0,8,0);bAdd.Font=new Font("Segoe UI Semibold",9.5F);
        var bEdit=ActBtn("Edit",ThemeManager.Surface,ThemeManager.Text);bEdit.Width=80;bEdit.Height=42;bEdit.Margin=new Padding(0,0,8,0);bEdit.Font=new Font("Segoe UI Semibold",9.5F);
        var bDel=ActBtn("Delete",ThemeManager.Danger,Color.White,"danger");bDel.Width=108;bDel.Height=42;bDel.Margin=new Padding(0,0,8,0);bDel.Font=new Font("Segoe UI Semibold",9.5F);
        var bExp=ActBtn("Export CSV",ThemeManager.Surface,ThemeManager.Text);bExp.Width=110;bExp.Height=42;bExp.Font=new Font("Segoe UI Semibold",9.5F);
        bAdd.Click+=BtnAdd_Click;bEdit.Click+=BtnEdit_Click;bDel.Click+=BtnDelete_Click;bExp.Click+=BtnExport_Click;
        flow.Controls.AddRange(new Control[]{bAdd,bEdit,bDel,bExp});actionBar.Controls.Add(flow);

        pnlBooks.Controls.Add(dgvBooks);pnlBooks.Controls.Add(actionBar);pnlBooks.Controls.Add(search);pnlBooks.Controls.Add(header);
    }

    private void BuildBorrow()
    {
        pnlBorrow=new Panel{BackColor=ThemeManager.Bg};
        var header=PageHeader("Borrowing");
        tabBorrow=new TabControl{Dock=DockStyle.Fill,Font=new Font("Segoe UI",10F)};
        var activeTab=new TabPage("Active"){BackColor=ThemeManager.Bg};
        var historyTab=new TabPage("History"){BackColor=ThemeManager.Bg};
        dgvActive=MkGrid();dgvHistory=MkGrid();
        activeTab.Controls.Add(dgvActive);historyTab.Controls.Add(dgvHistory);
        tabBorrow.TabPages.AddRange(new[]{activeTab,historyTab});
        var actions=new Panel{Dock=DockStyle.Top,Height=70,Padding=new Padding(0,12,0,10)};
        var actionsFlow=new FlowLayoutPanel{Dock=DockStyle.Left,AutoSize=true,WrapContents=false,FlowDirection=FlowDirection.LeftToRight,Margin=new Padding(0),Padding=new Padding(0)};
        var bBorrow=ActBtn("Borrow",ThemeManager.Accent,Color.White,"accent");bBorrow.Width=140;bBorrow.Height=42;bBorrow.Margin=new Padding(0,0,10,0);bBorrow.Font=new Font("Segoe UI Semibold",10F);bBorrow.TextAlign=ContentAlignment.MiddleCenter;bBorrow.Padding=new Padding(0,0,0,5);
        var bReturn=ActBtn("Return",ThemeManager.Success,Color.White,"success");bReturn.Width=140;bReturn.Height=42;bReturn.Margin=new Padding(0,0,10,0);bReturn.Font=new Font("Segoe UI Semibold",10F);bReturn.TextAlign=ContentAlignment.MiddleCenter;bReturn.Padding=new Padding(0,0,0,5);
        var bRefresh=ActBtn("Refresh",ThemeManager.Surface,ThemeManager.Text);bRefresh.Width=140;bRefresh.Height=44;bRefresh.Margin=new Padding(0);bRefresh.Font=new Font("Segoe UI Semibold",9.5F);bRefresh.TextAlign=ContentAlignment.MiddleCenter;bRefresh.Padding=new Padding(0,0,0,4);
        bBorrow.Click+=BtnBorrowBook_Click;bReturn.Click+=BtnReturnBook_Click;bRefresh.Click+=(_,_)=>RefreshBorrow();
        actionsFlow.Controls.AddRange(new Control[]{bBorrow,bReturn,bRefresh});
        actions.Controls.Add(actionsFlow);
        pnlBorrow.Controls.Add(tabBorrow);pnlBorrow.Controls.Add(actions);pnlBorrow.Controls.Add(header);
    }

    private void BuildStats()
    {
        pnlStats=new Panel{BackColor=ThemeManager.Bg};
        var header=PageHeader("Statistics");
        chartPanel=new Panel{Dock=DockStyle.Fill,BackColor=ThemeManager.Surface,Tag="card"};
        chartPanel.Paint+=ChartPaint;
        chartPanel.Resize+=(_,_)=>chartPanel.Invalidate();
        chartPanel.Padding=new Padding(8);
        pnlStats.Controls.Add(chartPanel);pnlStats.Controls.Add(header);
    }

    private void BuildSettings()
    {
        pnlSettings=new Panel{BackColor=ThemeManager.Bg};
        var header=PageHeader("Settings");
        var body=new Panel{Dock=DockStyle.Fill,Padding=new Padding(16),AutoScroll=true};
        var card=new Panel{Dock=DockStyle.Top,Height=350,BackColor=ThemeManager.Surface,Tag="card",Padding=new Padding(16)};
        var title=new Label{Text="Database Connection",Dock=DockStyle.Top,Height=36,Font=new Font("Segoe UI Semibold",11F),TextAlign=ContentAlignment.MiddleLeft,Tag="heading"};
        var form=new TableLayoutPanel{Dock=DockStyle.Top,AutoSize=true,ColumnCount=2,RowCount=7,Padding=new Padding(0,10,0,0)};
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,68F));form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,32F));
        static Label FL(string t)=>new(){Text=t,AutoSize=true,Font=new Font("Segoe UI Semibold",9F),Tag="muted",Margin=new Padding(0,8,0,4)};
        txtServer=new TextBox{Dock=DockStyle.Fill,Text="localhost",Margin=new Padding(0,0,12,10)};
        txtPort=new TextBox{Dock=DockStyle.Fill,Text="3306",Margin=new Padding(0,0,0,10)};
        txtDbName=new TextBox{Dock=DockStyle.Fill,Text="library_db",Margin=new Padding(0,0,12,10)};
        txtUser=new TextBox{Dock=DockStyle.Fill,Text="root",Margin=new Padding(0,0,0,10)};
        txtPass=new TextBox{Dock=DockStyle.Fill,PasswordChar='*',Margin=new Padding(0,0,12,10)};
        form.Controls.Add(FL("Server"),0,0);form.Controls.Add(FL("Port"),1,0);
        form.Controls.Add(txtServer,0,1);form.Controls.Add(txtPort,1,1);
        form.Controls.Add(FL("Database"),0,2);form.Controls.Add(FL("User"),1,2);
        form.Controls.Add(txtDbName,0,3);form.Controls.Add(txtUser,1,3);
        form.Controls.Add(FL("Password"),0,4);form.SetColumnSpan(form.GetControlFromPosition(0,4)!,2);
        form.Controls.Add(txtPass,0,5);form.SetColumnSpan(txtPass,2);
        var actions=new FlowLayoutPanel{Dock=DockStyle.Top,AutoSize=true,FlowDirection=FlowDirection.LeftToRight,WrapContents=false,Padding=new Padding(0),Margin=new Padding(0,6,0,0)};
        var bTest=ActBtn("Test Connection",ThemeManager.Accent,Color.White,"accent");bTest.Width=150;bTest.Height=34;bTest.Margin=new Padding(0,0,10,0);
        btnTheme=ActBtn(ThemeManager.IsDark?"Switch to Light":"Switch to Dark",ThemeManager.Surface,ThemeManager.Text);btnTheme.Width=150;btnTheme.Height=34;btnTheme.Margin=new Padding(0);
        lblConnResult=new Label{Dock=DockStyle.Top,Height=24,Margin=new Padding(0,10,0,0),ForeColor=ThemeManager.TextMuted,Tag="muted"};
        bTest.Click+=BtnConnect_Click;
        btnTheme.Click+=(_,_)=>{ThemeManager.Toggle();ThemeManager.Apply(this);btnTheme.Text=ThemeManager.IsDark?"Switch to Light":"Switch to Dark";chartPanel?.Invalidate();};
        AcceptButton=bTest;actions.Controls.Add(bTest);actions.Controls.Add(btnTheme);
        card.Controls.Add(lblConnResult);card.Controls.Add(actions);card.Controls.Add(form);card.Controls.Add(title);
        body.Controls.Add(card);pnlSettings.Controls.Add(body);pnlSettings.Controls.Add(header);
    }

    private void RefreshBooks()
    {
        try
        {
            bool? available=cmbStatus.SelectedIndex switch{1=>true,2=>false,_=>null};
            _currentBooks=_db.GetBooks(txtTitle.Text,txtAuthor.Text,txtGenre.Text,txtISBN.Text,available);
            dgvBooks.Rows.Clear();
            foreach(var b in _currentBooks){int i=dgvBooks.Rows.Add(b.Id,b.Title,b.Author,b.ISBN,b.Genre,b.AvailabilityLabel);dgvBooks.Rows[i].Cells["AvailabilityLabel"].Style.ForeColor=b.IsAvailable?ThemeManager.Success:ThemeManager.Warning;}
            SetStatus($"{_currentBooks.Count} book(s) found.");
        }
        catch(Exception ex){DbErr(ex);}
    }

    private void RefreshBorrow()
    {
        try{FillBorrowGrid(dgvActive,_db.GetBorrowRecords(true));FillBorrowGrid(dgvHistory,_db.GetBorrowRecords(false));}
        catch(Exception ex){DbErr(ex);}
    }

    private static void FillBorrowGrid(DataGridView dgv,List<BorrowRecord> records)
    {
        dgv.Columns.Clear();dgv.Rows.Clear();
        dgv.Columns.AddRange(MkCol("Id","ID",50,false),MkCol("BookTitle","Book",210),MkCol("BorrowerName","Borrower",150),MkCol("BorrowDate","Borrowed",90),MkCol("DueDate","Due",90),MkCol("StatusLabel","Status",90));
        foreach(var r in records)dgv.Rows.Add(r.Id,r.BookTitle,r.BorrowerName,r.BorrowDate.ToString("yyyy-MM-dd"),r.DueDate.ToString("yyyy-MM-dd"),r.StatusLabel);
        ThemeManager.StyleGrid(dgv);
    }

    private void RefreshStats()
    {
        try{var s=_db.GetStats();_statTotal=Convert.ToInt32(s["total"]);_statAvailable=Convert.ToInt32(s["available"]);_statBorrowed=Convert.ToInt32(s["borrowed"]);_statOverdue=Convert.ToInt32(s["overdue"]);_chartData=_db.GetBooksByGenre();chartPanel.Invalidate();}
        catch(Exception ex){DbErr(ex);}
    }

    private void ChartPaint(object? sender,PaintEventArgs e)
    {
        var g=e.Graphics;g.Clear(chartPanel.BackColor);g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var fTitle=new Font("Segoe UI Semibold",12F);using var fSummary=new Font("Segoe UI",9.5F);using var fAxis=new Font("Segoe UI",8.5F);
        using var bText=new SolidBrush(ThemeManager.Text);using var bMuted=new SolidBrush(ThemeManager.TextMuted);
        const int padLeft=54,padRight=22,padTop=108,padBottom=84;
        int plotW=Math.Max(120,chartPanel.ClientSize.Width-padLeft-padRight);int plotH=Math.Max(120,chartPanel.ClientSize.Height-padTop-padBottom);
        int left=padLeft,top=padTop,bottom=top+plotH;
        g.DrawString("Books by Genre",fTitle,bText,left,12);
        g.DrawString($"Total: {_statTotal}   Available: {_statAvailable}   Borrowed: {_statBorrowed}   Overdue: {_statOverdue}",fSummary,bMuted,left,50);
        if(_chartData.Count==0){g.DrawString("No data yet. Add books or connect your database.",fSummary,bMuted,left,top+24);return;}
        int max=Math.Max(1,_chartData.Max(d=>d.Count));int step=max<=5?1:(int)Math.Ceiling(max/5.0);int yMax=(int)Math.Ceiling(max/(double)step)*step;
        using var pGrid=new System.Drawing.Pen(Color.FromArgb(38,ThemeManager.Text)){DashStyle=System.Drawing.Drawing2D.DashStyle.Dash};
        using var pAxis=new System.Drawing.Pen(ThemeManager.Border,1.4f);
        for(int y=0;y<=yMax;y+=step){int yy=bottom-(int)(y/(double)yMax*plotH);g.DrawLine(pGrid,left,yy,left+plotW,yy);g.DrawString(y.ToString(),fAxis,bText,left-18,yy-8);}
        g.DrawLine(pAxis,left,top,left,bottom);g.DrawLine(pAxis,left,bottom,left+plotW,bottom);
        int n=_chartData.Count;float gap,barW,startX;
        if(n<=8){gap=Math.Clamp(plotW*0.03f,18f,34f);barW=(plotW-(n+1)*gap)/n;barW=Math.Clamp(barW,30f,140f);float clusterW=n*barW+(n+1)*gap;startX=left+(plotW-clusterW)/2f;}
        else{float slot=plotW/(float)n;barW=Math.Clamp(slot*0.66f,12f,72f);gap=slot-barW;startX=left;}
        bool angledLabels=n>8;int labelStride=Math.Max(1,(int)Math.Ceiling(n/12.0));
        Color[] pal={ThemeManager.Accent,ThemeManager.Success,ThemeManager.Warning,ThemeManager.Danger,ThemeManager.Info,Color.FromArgb(17,138,178),Color.FromArgb(239,71,111),Color.FromArgb(6,214,160)};
        for(int i=0;i<n;i++){var(genre,count)=_chartData[i];float x=startX+gap+i*(barW+gap);int h=(int)(count/(double)yMax*plotH);int y=bottom-h;using var br=new SolidBrush(pal[i%pal.Length]);g.FillRectangle(br,x,y,barW,h);if(i%labelStride==0){var lbl=genre.Length>12?genre[..12]+"...":genre;if(angledLabels){var state=g.Save();g.TranslateTransform(x+barW/2f+3,bottom+10);g.RotateTransform(-32);g.DrawString(lbl,fAxis,bText,0,0);g.Restore(state);}else{var lsz=g.MeasureString(lbl,fAxis);g.DrawString(lbl,fAxis,bText,x+barW/2f-lsz.Width/2f,bottom+8);}}}
    }

    private void BtnAdd_Click(object? s,EventArgs e){using var f=new AddEditBookForm();if(f.ShowDialog(this)==DialogResult.OK&&f.Result!=null){try{_db.AddBook(f.Result);RefreshBooks();}catch(Exception ex){DbErr(ex);}}}
    private void BtnEdit_Click(object? s,EventArgs e){var b=Sel();if(b==null)return;using var f=new AddEditBookForm(b);if(f.ShowDialog(this)==DialogResult.OK&&f.Result!=null){try{_db.UpdateBook(f.Result);RefreshBooks();}catch(Exception ex){DbErr(ex);}}}
    private void BtnDelete_Click(object? s,EventArgs e){var b=Sel();if(b==null)return;if(MessageBox.Show($"Delete \"{b.Title}\" by {b.Author}?","Confirm",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)==DialogResult.Yes){try{_db.DeleteBook(b.Id);RefreshBooks();}catch(Exception ex){DbErr(ex);}}}
    private async void BtnExport_Click(object? s,EventArgs e)
    {
        try
        {
            if(_currentBooks==null||_currentBooks.Count==0)
            {
                MessageBox.Show("There are no books to export.","Export CSV",MessageBoxButtons.OK,MessageBoxIcon.Information);
                return;
            }

            using var d=new SaveFileDialog
            {
                Filter="CSV|*.csv",
                FileName=$"library_{DateTime.Now:yyyyMMdd}.csv",
                AddExtension=true,
                DefaultExt="csv",
                OverwritePrompt=true,
                CheckPathExists=true
            };

            if(d.ShowDialog(this)!=DialogResult.OK) return;

            // Snapshot current data so export is stable even if grid refreshes.
            var booksSnapshot=_currentBooks.Select(b=>new Book
            {
                Id=b.Id,
                Title=b.Title,
                Author=b.Author,
                ISBN=b.ISBN,
                PublicationYear=b.PublicationYear,
                Genre=b.Genre,
                Shelf=b.Shelf,
                Row=b.Row,
                IsAvailable=b.IsAvailable,
                CoverUrl=b.CoverUrl,
                Description=b.Description,
                CreatedAt=b.CreatedAt
            }).ToList();

            string targetFile=d.FileName;
            SetStatus("Exporting CSV...");
            UseWaitCursor=true;

            await Task.Run(() =>
            {
                string csv=DatabaseHelper.ToCsv(booksSnapshot);
                File.WriteAllText(targetFile,csv);
            });

            SetStatus($"Exported {booksSnapshot.Count} books to {Path.GetFileName(targetFile)}.");
            MessageBox.Show("Export completed successfully.","Export CSV",MessageBoxButtons.OK,MessageBoxIcon.Information);
        }
        catch(Exception ex)
        {
            MessageBox.Show($"Export failed:\n{ex.Message}","Export CSV",MessageBoxButtons.OK,MessageBoxIcon.Error);
            SetStatus("Export failed.");
        }
        finally
        {
            UseWaitCursor=false;
        }
    }

    private void BtnBorrowBook_Click(object? s,EventArgs e)
    {
        var avail=_currentBooks.Where(b=>b.IsAvailable).ToList();
        if(!avail.Any()){MessageBox.Show("No available books. Load books first.");return;}
        using var pick=new Form{Text="Select a Book",Size=new Size(500,380),StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,BackColor=ThemeManager.Bg,ForeColor=ThemeManager.Text};
        var lst=new ListBox{Dock=DockStyle.Fill,BackColor=ThemeManager.Surface,ForeColor=ThemeManager.Text,Font=new Font("Segoe UI",10.5F),ItemHeight=26};
        foreach(var b in avail)lst.Items.Add($"{b.Title}  \u2014  {b.Author}");
        var ok=ActBtn("Select",ThemeManager.Accent,Color.White,"accent");ok.Dock=DockStyle.Bottom;ok.Height=40;
        ok.Click+=(_,_)=>{if(lst.SelectedIndex>=0){pick.Tag=avail[lst.SelectedIndex];pick.DialogResult=DialogResult.OK;}};
        pick.Controls.AddRange(new Control[]{lst,ok});
        if(pick.ShowDialog(this)!=DialogResult.OK)return;
        var sel=(Book)pick.Tag!;
        using var frm=new BorrowForm(sel.Title);
        if(frm.ShowDialog(this)==DialogResult.OK){try{_db.BorrowBook(sel.Id,frm.BorrowerName,frm.BorrowerEmail,frm.DueDate);RefreshBorrow();RefreshBooks();}catch(Exception ex){DbErr(ex);}}
    }

    private void BtnReturnBook_Click(object? s,EventArgs e)
    {
        var dgv=tabBorrow.SelectedIndex==0?dgvActive:dgvHistory;
        if(dgv.SelectedRows.Count==0)return;
        int rid=(int)dgv.SelectedRows[0].Cells["Id"].Value;
        try{var rec=_db.GetBorrowRecords().FirstOrDefault(r=>r.Id==rid);if(rec==null||rec.IsReturned){MessageBox.Show("Already returned.");return;}_db.ReturnBook(rid,rec.BookId);RefreshBorrow();RefreshBooks();}
        catch(Exception ex){DbErr(ex);}
    }

    private void BtnConnect_Click(object? s,EventArgs e)
    {
        string cs=$"server={txtServer.Text.Trim()};port={txtPort.Text.Trim()};database={txtDbName.Text.Trim()};user={txtUser.Text.Trim()};password={txtPass.Text.Trim()}";
        _db.UpdateConnectionString(cs);SaveConfig(cs);
        bool ok=_db.TestConnection();
        lblConnResult.ForeColor=ok?ThemeManager.Success:ThemeManager.Danger;
        lblConnResult.Text=ok?"\u2714  Connected successfully":"\u2716  Connection failed";
        UpdateDbIndicator();if(ok)RefreshBooks();
    }

    private Book? Sel(){if(dgvBooks.SelectedRows.Count==0){MessageBox.Show("Select a book first.");return null;}return _currentBooks.FirstOrDefault(b=>b.Id==(int)dgvBooks.SelectedRows[0].Cells["Id"].Value);}

    private void Activate(Panel p,Button n)
    {
        foreach(var pp in _panels)pp.Visible=false;p.Visible=true;
        foreach(var b in _navButtons){b.BackColor=ThemeManager.Sidebar;b.Font=new Font("Segoe UI",10F);b.ForeColor=Color.FromArgb(170,170,200);}
        n.BackColor=ThemeManager.SideActive;n.Font=new Font("Segoe UI",10F);n.ForeColor=Color.White;
    }

    private void SetStatus(string s)=>lblStatus.Text=s;
    private void DbErr(Exception ex)=>MessageBox.Show($"Database error:\n{ex.Message}\n\nCheck Settings.","Error",MessageBoxButtons.OK,MessageBoxIcon.Error);
    private void UpdateDbIndicator(){bool ok=_db.TestConnection();lblDbIndicator.Text=ok?"DB: Connected \u2714":"DB: Not connected \u2716";lblDbIndicator.ForeColor=ok?ThemeManager.Success:ThemeManager.Danger;}

    private static readonly string CfgPath=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"db_config.txt");
    private static string? LoadConfig()=>File.Exists(CfgPath)?File.ReadAllText(CfgPath).Trim():null;
    private static void SaveConfig(string cs)=>File.WriteAllText(CfgPath,cs);

    private static Label PageHeader(string text)=>new(){Text=text,Dock=DockStyle.Top,Height=58,Font=new Font("Segoe UI Semibold",15F),Padding=new Padding(2,0,0,0),TextAlign=ContentAlignment.MiddleLeft,Tag="heading"};
    private static Button NavBtn(string text){var b=new Button{Text=text,Dock=DockStyle.Top,Height=42,FlatStyle=FlatStyle.Flat,TextAlign=ContentAlignment.MiddleLeft,Padding=new Padding(20,0,0,0),Font=new Font("Segoe UI",10F),ForeColor=Color.FromArgb(170,170,200),Tag="side"};b.FlatAppearance.BorderSize=0;b.FlatAppearance.MouseOverBackColor=ThemeManager.SideHover;return b;}
    private static Button ActBtn(string text,Color bg,Color fg,string? tag=null){var b=new Button{Text=text,Height=32,FlatStyle=FlatStyle.Flat,BackColor=bg,ForeColor=fg,Font=new Font("Segoe UI",9.5F),Tag=tag,Cursor=Cursors.Hand};b.FlatAppearance.BorderColor=ThemeManager.Border;if(tag=="accent"||tag=="danger"||tag=="success")b.FlatAppearance.BorderSize=0;return b;}
    private static TextBox SBox(string ph)=>new(){Top=10,Height=28,PlaceholderText=ph,Font=new Font("Segoe UI",9.5F),BorderStyle=BorderStyle.FixedSingle};
    private static DataGridView MkGrid(){var g=new DataGridView{Dock=DockStyle.Fill,AllowUserToAddRows=false,AllowUserToDeleteRows=false,ReadOnly=true,SelectionMode=DataGridViewSelectionMode.FullRowSelect,MultiSelect=false,RowHeadersVisible=false,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,Font=new Font("Segoe UI",9.5F)};ThemeManager.StyleGrid(g);return g;}
    private static DataGridViewTextBoxColumn MkCol(string n,string h,int w,bool v=true)=>new(){Name=n,HeaderText=h,FillWeight=w,Visible=v};
}
