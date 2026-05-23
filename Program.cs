using System;
using System.Text;
using System.Windows.Forms;

namespace ItemDataConverter
{
    internal static class Program
    {
        /// <summary>
        /// アプリケーションのメイン エントリ ポイント
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Shift_JIS (CP932) エンコーディングのサポートを有効化
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
