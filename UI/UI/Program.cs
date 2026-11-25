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
            var netClient = new NetworkClientService();
            Application.Run(new LoginForm(netClient));
        }
    }
}