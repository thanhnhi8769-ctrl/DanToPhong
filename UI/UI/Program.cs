using System;
using System.Windows.Forms;
using UI.Services;

namespace UI
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            ApplicationConfiguration.Initialize();
            try
            {
                using var netClient = new NetworkClientService();
                Application.Run(new LoginForm(netClient));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Thiếu cấu hình máy chủ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
