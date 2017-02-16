using Medo.Security.Cryptography;
using System;
using System.Text;
using System.Windows.Forms;

namespace OneTimePasswordSample {
    static class Program {

        [STAThread]
        static void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
