using LibraryApp.Forms;
Application.EnableVisualStyles();
Application.SetCompatibleTextRenderingDefault(false);
Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
Application.ThreadException += (_, e) => MessageBox.Show($"Error:\n{e.Exception.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
Application.Run(new MainForm());
