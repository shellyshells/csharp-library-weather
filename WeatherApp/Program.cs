using WeatherApp.Forms;
using System;
using System.IO;
using System.Windows.Forms;

namespace WeatherApp
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            LoadDotEnv();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.ThreadException += (_, args) =>
                MessageBox.Show($"An unexpected error occurred:\n{args.Exception.Message}", "WeatherPro — Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Application.Run(new MainForm());
        }

        private static void LoadDotEnv()
        {
            string envPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env");
            if (!File.Exists(envPath))
            {
                envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
                if (!File.Exists(envPath)) return;
            }
            foreach (string line in File.ReadAllLines(envPath))
            {
                string trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#')) continue;
                int sep = trimmed.IndexOf('=');
                if (sep <= 0) continue;
                string key   = trimmed[..sep].Trim();
                string value = trimmed[(sep + 1)..].Trim().Trim('"');
                if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
                    Environment.SetEnvironmentVariable(key, value);
            }
        }
    }
}
