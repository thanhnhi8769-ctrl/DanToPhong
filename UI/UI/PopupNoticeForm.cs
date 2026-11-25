using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace UI
{
    public sealed partial class PopupNoticeForm : Form
    {
        public enum NoticeKind
        {
            Info,
            Warning,
            Error
        }

        public enum NoticeButtons
        {
            Ok,
            YesNo
        }

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HTCAPTION = 0x2;

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        private readonly NoticeButtons _buttons;

        public PopupNoticeForm(
            string title,
            string message,
            NoticeKind kind = NoticeKind.Info,
            Image? backgroundImage = null,
            NoticeButtons buttons = NoticeButtons.Ok)
        {
            InitializeComponent();
            DoubleBuffered = true;

            _buttons = buttons;
            Text = string.IsNullOrWhiteSpace(title) ? "Notice" : title;
            lblTitle.Text = Text;
            lblMessage.Text = message ?? string.Empty;

            BackgroundImage = backgroundImage ?? Properties.Resources.bg;
            BackgroundImageLayout = ImageLayout.Stretch;

            Color accentColor = kind switch
            {
                NoticeKind.Error => Color.Crimson,
                NoticeKind.Warning => Color.FromArgb(180, 83, 9),
                _ => Color.FromArgb(43, 27, 61)
            };

            pnlHeader.BackColor = accentColor;
            btnOk.BackColor = accentColor;
            btnYes.BackColor = accentColor;

            pnlTitleBar.MouseDown += TitleBar_MouseDown;
            lblBarTitle.MouseDown += TitleBar_MouseDown;
            btnMinimize.Click += (_, _) => WindowState = FormWindowState.Minimized;
            btnMaximize.Click += (_, _) =>
            {
                WindowState = WindowState == FormWindowState.Normal
                    ? FormWindowState.Maximized
                    : FormWindowState.Normal;
                btnMaximize.Text = WindowState == FormWindowState.Maximized ? "❐" : "□";
                CanGiuaCard();
            };
            btnClose.Click += (_, _) =>
            {
                DialogResult = _buttons == NoticeButtons.YesNo ? DialogResult.No : DialogResult.Cancel;
                Close();
            };

            CauHinhCheDoNut();
            CanGiuaCard();
        }

        private void TitleBar_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || WindowState == FormWindowState.Maximized) return;
            ReleaseCapture();
            SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            CanGiuaCard();
            BringToFront();
            Activate();
        }

        private void CauHinhCheDoNut()
        {
            bool isYesNo = _buttons == NoticeButtons.YesNo;

            btnOk.Visible = !isYesNo;
            btnYes.Visible = isYesNo;
            btnNo.Visible = isYesNo;

            if (isYesNo)
            {
                AcceptButton = btnYes;
                CancelButton = btnNo;
            }
            else
            {
                AcceptButton = btnOk;
                CancelButton = btnOk;
            }
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            if (WindowState != FormWindowState.Minimized)
                CanGiuaCard();
        }

        private void CanGiuaCard()
        {
            if (nightPanel1 == null || panelShadow == null || pnlTitleBar == null) return;

            int btnY = Math.Max(4, (pnlTitleBar.Height - btnClose.Height) / 2);
            btnClose.Location = new Point(pnlTitleBar.Width - btnClose.Width - 8, btnY);
            btnMaximize.Location = new Point(btnClose.Left - btnMaximize.Width - 5, btnY);
            btnMinimize.Location = new Point(btnMaximize.Left - btnMinimize.Width - 5, btnY);
            lblBarTitle.Location = new Point(12, 0);
            lblBarTitle.Size = new Size(Math.Max(100, btnMinimize.Left - 18), pnlTitleBar.Height);

            btnClose.BringToFront();
            btnMaximize.BringToFront();
            btnMinimize.BringToFront();

            int topArea = pnlTitleBar.Height;
            int availH = Math.Max(195, ClientSize.Height - topArea);

            int x = Math.Max(12, (ClientSize.Width - nightPanel1.Width) / 2);
            int y = topArea + Math.Max(8, (availH - nightPanel1.Height) / 2);

            nightPanel1.Location = new Point(x, y);
            panelShadow.Size = nightPanel1.Size;
            panelShadow.Location = new Point(x + 4, y + 4);

            int pad = 16;
            int actionBtnTop = nightPanel1.Height - btnOk.Height - 12;
            lblMessage.Location = new Point(pad, pnlHeader.Height + 8);
            lblMessage.Size = new Size(
                Math.Max(120, nightPanel1.Width - pad * 2),
                Math.Max(36, actionBtnTop - lblMessage.Top - 8));

            if (_buttons == NoticeButtons.Ok)
            {
                btnOk.Location = new Point(nightPanel1.Width - btnOk.Width - pad, actionBtnTop);
            }
            else
            {
                int gap = 8;
                btnYes.Location = new Point(nightPanel1.Width - btnYes.Width - pad, actionBtnTop);
                btnNo.Location = new Point(btnYes.Left - btnNo.Width - gap, actionBtnTop);
            }

            panelShadow.BringToFront();
            nightPanel1.BringToFront();
            pnlTitleBar.BringToFront();

            BoGocControl(nightPanel1, 14);
            BoGocControl(panelShadow, 14);
        }

        private static void BoGocControl(Control ctrl, int radius = 14)
        {
            if (ctrl.Width <= 0 || ctrl.Height <= 0) return;

            using GraphicsPath path = new();
            int d = Math.Min(radius * 2, Math.Min(ctrl.Width, ctrl.Height));

            path.AddArc(0, 0, d, d, 180, 90);
            path.AddArc(ctrl.Width - d, 0, d, d, 270, 90);
            path.AddArc(ctrl.Width - d, ctrl.Height - d, d, d, 0, 90);
            path.AddArc(0, ctrl.Height - d, d, d, 90, 90);
            path.CloseFigure();

            ctrl.Region?.Dispose();
            ctrl.Region = new Region(path);
        }

        public static void ShowNotice(IWin32Window? owner, string title, string message, NoticeKind kind = NoticeKind.Info)
        {
            Form? ownerForm = owner as Form;
            Image? bg = ownerForm?.BackgroundImage;
            using PopupNoticeForm dlg = new(title, message, kind, bg, NoticeButtons.Ok);

            if (ownerForm != null && ownerForm.Visible && !ownerForm.IsDisposed)
                dlg.ShowDialog(ownerForm);
            else
            {
                dlg.TopMost = true;
                dlg.StartPosition = FormStartPosition.CenterScreen;
                dlg.ShowDialog();
            }
        }

        public static DialogResult ShowConfirm(IWin32Window? owner, string title, string message, NoticeKind kind = NoticeKind.Warning)
        {
            Form? ownerForm = owner as Form;
            Image? bg = ownerForm?.BackgroundImage;
            using PopupNoticeForm dlg = new(title, message, kind, bg, NoticeButtons.YesNo);

            if (ownerForm != null && ownerForm.Visible && !ownerForm.IsDisposed)
                return dlg.ShowDialog(ownerForm);

            dlg.TopMost = true;
            dlg.StartPosition = FormStartPosition.CenterScreen;
            return dlg.ShowDialog();
        }
    }
}