using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace UI
{
    public partial class GuidelinesDialog : Form
    {
        public GuidelinesDialog(string title, string subtitle, string rulesText)
        {
            InitializeComponent();
            DoubleBuffered = true;
            AcceptButton = btnClose;
            CancelButton = btnClose;

            lblDialogTitle.Text = title;
            lblDialogSubtitle.Text = subtitle;
            lblRulesContent.Text = rulesText;

            CanGiuaVaBoGoc();
        }

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HTCAPTION = 0x2;

        private void pnlTopBar_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            ReleaseCapture();
            SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
        }

        private void CanGiuaVaBoGoc()
        {
            lblDialogTitle.Location = new Point(Math.Max(16, (pnlCard.Width - lblDialogTitle.Width) / 2), 14);
            lblDialogSubtitle.Location = new Point(Math.Max(16, (pnlCard.Width - lblDialogSubtitle.Width) / 2), lblDialogTitle.Bottom + 2);
            btnClose.Location = new Point((pnlCard.Width - btnClose.Width) / 2, pnlCard.Height - btnClose.Height - 18);

            BoGocControl(pnlCard, 16);
            BoGocControl(pnlShadow, 16);
        }

        private static void BoGocControl(Control ctrl, int radius)
        {
            if (ctrl.Width <= 0 || ctrl.Height <= 0) return;

            using GraphicsPath path = new GraphicsPath();
            int d = Math.Min(radius * 2, Math.Min(ctrl.Width, ctrl.Height));

            path.AddArc(0, 0, d, d, 180, 90);
            path.AddArc(ctrl.Width - d, 0, d, d, 270, 90);
            path.AddArc(ctrl.Width - d, ctrl.Height - d, d, d, 0, 90);
            path.AddArc(0, ctrl.Height - d, d, d, 90, 90);
            path.CloseFigure();

            ctrl.Region?.Dispose();
            ctrl.Region = new Region(path);
        }

        private void btnClose_Click(object? sender, EventArgs e)
        {
            Close();
        }
    }
}