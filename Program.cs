using System;
using System.IO;
using System.Windows.Forms;
using FreeKantar.UI;

namespace FreeKantar
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // First things first, setup a file-based logger in case the UI fails
            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "startup_error.txt");

            try
            {
                // Clear old log
                if (File.Exists(logPath)) File.Delete(logPath);

                ApplicationConfiguration.Initialize();
                RunApp();
            }
            catch (Exception ex)
            {
                string msg = $"CRITICAL STARTUP ERROR:\n{ex.Message}\n{ex.StackTrace}";
                if (ex.InnerException != null) msg += $"\nInner: {ex.InnerException.Message}";
                
                File.WriteAllText(logPath, msg);
                MessageBox.Show(msg, "Kritik Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Method isolation to prevent JIT failure in Main
        static void RunApp()
        {
            try
            {
                // In v2 bundles (bundle_e_sqlcipher), Batteries_V2.Init() is sufficient
                // to initialize the correct provider included in the bundle.
                SQLitePCL.Batteries_V2.Init();
                
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_error.txt");
                File.WriteAllText(logPath, ex.ToString());
                throw; // Re-throw to be caught by Main
            }
        }
    }
}