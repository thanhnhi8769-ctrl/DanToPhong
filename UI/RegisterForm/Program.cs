using System;
using System.Windows.Forms;
using UI.Services;

namespace UI
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            using NetworkClientService netClient = new NetworkClientService("127.0.0.1", 9000);
            Application.Run(new LoginForm(netClient));
        }
    }
}