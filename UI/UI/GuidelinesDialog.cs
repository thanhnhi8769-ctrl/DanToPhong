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

            CauHinhCacNutDieuKhien();
            CanGiuaVaBoGoc();
        }

        private void CauHinhCacNutDieuKhien()
        {
            // Nút thu nhỏ (-)
            btnTopMinimize.Text = "-";
            btnTopMinimize.BackColor = Color.FromArgb(43, 27, 61);
            btnTopMinimize.ForeColor = Color.White;
            btnTopMinimize.FlatAppearance.MouseOverBackColor = Color.FromArgb(65, 42, 92);
            btnTopMinimize.FlatAppearance.MouseDownBackColor = Color.FromArgb(30, 18, 44);

            // Nút phóng to / khôi phục (□ / ❐)
            btnTopMaximize.Text = WindowState == FormWindowState.Maximized ? "❐" : "□";
            btnTopMaximize.BackColor = Color.FromArgb(43, 27, 61);
            btnTopMaximize.ForeColor = Color.White;
            btnTopMaximize.FlatAppearance.MouseOverBackColor = Color.FromArgb(65, 42, 92);
            btnTopMaximize.FlatAppearance.MouseDownBackColor = Color.FromArgb(30, 18, 44);

            // Nút đóng (X) màu đỏ
            btnTopClose.Text = "X";
            btnTopClose.BackColor = Color.FromArgb(155, 17, 24);
            btnTopClose.ForeColor = Color.White;
            btnTopClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(185, 24, 32);
            btnTopClose.FlatAppearance.MouseDownBackColor = Color.FromArgb(120, 12, 18);
        }

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HTCAPTION = 0x2;

        private void pnlTopBar_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || WindowState == FormWindowState.Maximized) return;
            ReleaseCapture();
            SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
        }

        private void lblBarBrand_MouseDown(object? sender, MouseEventArgs e)
        {
            pnlTopBar_MouseDown(sender, e);
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            if (WindowState != FormWindowState.Minimized)
                CanGiuaVaBoGoc();
        }

        private void CanGiuaVaBoGoc()
        {
            if (pnlTopBar == null || pnlCard == null || pnlShadow == null) return;

            int btnY = Math.Max(4, (pnlTopBar.Height - btnTopClose.Height) / 2);
            btnTopClose.Location = new Point(pnlTopBar.Width - btnTopClose.Width - 10, btnY);
            btnTopMaximize.Location = new Point(btnTopClose.Left - btnTopMaximize.Width - 6, btnY);
            btnTopMinimize.Location = new Point(btnTopMaximize.Left - btnTopMinimize.Width - 6, btnY);
            lblBarBrand.Location = new Point(16, (pnlTopBar.Height - lblBarBrand.Height) / 2);

            btnTopClose.BringToFront();
            btnTopMaximize.BringToFront();
            btnTopMinimize.BringToFront();

            int availW = Math.Max(320, ClientSize.Width - 44);
            int availH = Math.Max(280, ClientSize.Height - pnlTopBar.Height - 28);

            int cardW = Math.Min(620, availW);
            int cardH = Math.Min(460, availH);
            pnlCard.Size = new Size(cardW, cardH);

            int cardX = Math.Max(12, (ClientSize.Width - pnlCard.Width) / 2);
            int cardY = pnlTopBar.Height + Math.Max(8, (ClientSize.Height - pnlTopBar.Height - pnlCard.Height) / 2);

            pnlCard.Location = new Point(cardX, cardY);
            pnlShadow.Size = pnlCard.Size;
            pnlShadow.Location = new Point(cardX + 6, cardY + 6);

            lblDialogTitle.Location = new Point(Math.Max(16, (pnlCard.Width - lblDialogTitle.Width) / 2), 14);
            lblDialogSubtitle.Location = new Point(Math.Max(16, (pnlCard.Width - lblDialogSubtitle.Width) / 2), lblDialogTitle.Bottom + 2);

            pnlRulesScroll.Location = new Point(22, lblDialogSubtitle.Bottom + 10);
            btnClose.Location = new Point((pnlCard.Width - btnClose.Width) / 2, pnlCard.Height - btnClose.Height - 18);
            pnlRulesScroll.Size = new Size(pnlCard.Width - 44, Math.Max(80, btnClose.Top - pnlRulesScroll.Top - 12));

            lblRulesContent.MaximumSize = new Size(Math.Max(120, pnlRulesScroll.ClientSize.Width - 8), 0);
            lblRulesContent.Location = new Point(2, 2);

            pnlShadow.SendToBack();
            pnlCard.BringToFront();
            pnlTopBar.BringToFront();

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

        private void btnTopMinimize_Click(object? sender, EventArgs e)
        {
            WindowState = FormWindowState.Minimized;
        }

        private void btnTopMaximize_Click(object? sender, EventArgs e)
        {
            WindowState = WindowState == FormWindowState.Normal
                ? FormWindowState.Maximized
                : FormWindowState.Normal;
            btnTopMaximize.Text = WindowState == FormWindowState.Maximized ? "❐" : "□";
            BeginInvoke(new Action(CanGiuaVaBoGoc));
        }

        private void btnClose_Click(object? sender, EventArgs e)
        {
            Close();
        }
    }
}