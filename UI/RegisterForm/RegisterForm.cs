using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using DanToPhong.Protocol;
using UI.Services;

namespace UI
{
    public partial class RegisterForm : Form
    {
        private static readonly Regex UsernameRegex = new(@"^[a-zA-Z0-9_]{3,20}$", RegexOptions.Compiled);
        private static readonly Regex HasLetterRegex = new(@"[A-Za-z]", RegexOptions.Compiled);
        private static readonly Regex HasDigitRegex = new(@"[0-9]", RegexOptions.Compiled);
        private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

        private readonly NetworkClientService _netClient;
        private bool isPasswordVisible;
        private bool isConfirmPasswordVisible;
        private bool isReturningToLogin;

        public RegisterForm(NetworkClientService netClient)
        {
            _netClient = netClient;
            InitializeComponent();
            DoubleBuffered = true;
            AcceptButton = btnRegister;

            dtpBirthday.Format = DateTimePickerFormat.Custom;
            dtpBirthday.CustomFormat = "dd/MM/yyyy";
            dtpBirthday.MaxDate = DateTime.Today;
            dtpBirthday.MinDate = new DateTime(1920, 1, 1);
            dtpBirthday.Value = new DateTime(2004, 1, 1);

            TextBox[] fields = new[] { txtUsername, txtPassword, txtConfirmPassword, txtFullName, txtEmail };
            foreach (TextBox tb in fields)
            {
                tb.GotFocus += (_, _) => nightPanel1.Invalidate();
                tb.LostFocus += (_, _) => nightPanel1.Invalidate();
            }

            nightPanel1.Paint += VeVienDuoiCacO;
            CanGiuaBoCuc();
        }

        private void VeVienDuoiCacO(object? sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            VeDuongVien(e.Graphics, txtUsername, null, lblErrUsername.Text.Length > 0);
            VeDuongVien(e.Graphics, txtPassword, btnTogglePassword, lblErrPassword.Text.Length > 0);
            VeDuongVien(e.Graphics, txtConfirmPassword, btnToggleConfirmPassword, lblErrConfirmPassword.Text.Length > 0);
            VeDuongVien(e.Graphics, txtFullName, null, lblErrFullName.Text.Length > 0);
            VeDuongVien(e.Graphics, txtEmail, null, lblErrEmail.Text.Length > 0);
        }

        private void VeDuongVien(Graphics g, TextBox txt, Control? rightControl, bool hasError)
        {
            Color borderColor = hasError
                ? Color.Crimson
                : txt.Focused
                    ? Color.FromArgb(43, 27, 61)
                    : Color.Silver;

            float thickness = hasError || txt.Focused ? 2.2f : 1.5f;

            using Pen pen = new Pen(borderColor, thickness);
            int y = txt.Bottom + 3;
            int rightEdge = rightControl != null ? rightControl.Right : txt.Right;
            g.DrawLine(pen, txt.Left - 1, y, rightEdge, y);
        }

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HTCAPTION = 0x2;

        private void pnlTitleBar_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || WindowState == FormWindowState.Maximized) return;
            ReleaseCapture();
            SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            if (WindowState != FormWindowState.Minimized) CanGiuaBoCuc();
        }

        private void CanGiuaBoCuc()
        {
            if (nightPanel1 == null || panelShadow == null || WindowState == FormWindowState.Minimized) return;

            SuspendLayout();

            int topOffset = pnlTitleBar.Height + pnlHeaderBar.Height;
            int usableHeight = Math.Max(0, ClientSize.Height - topOffset);

            int x = Math.Max(16, (ClientSize.Width - nightPanel1.Width) / 2);
            int y = topOffset + Math.Max(10, (usableHeight - nightPanel1.Height) / 2);

            nightPanel1.Location = new Point(x, y);
            panelShadow.Size = nightPanel1.Size;
            panelShadow.Location = new Point(x + 6, y + 6);

            panelShadow.SendToBack();
            nightPanel1.BringToFront();

            lblTitle.Location = new Point((nightPanel1.Width - lblTitle.Width) / 2, 10);
            lblSubtitle.Location = new Point((nightPanel1.Width - lblSubtitle.Width) / 2, lblTitle.Bottom + 2);
            btnHelp.Location = new Point(nightPanel1.Width - btnHelp.Width - 14, 12);

            BoGocControl(nightPanel1, 16);
            BoGocControl(panelShadow, 16);

            ResumeLayout(true);
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

        private static string? ValidateUsername(string u)
        {
            if (string.IsNullOrEmpty(u)) return "Please enter a username.";
            if (!UsernameRegex.IsMatch(u)) return "3-20 chars: letters, numbers, or _ only.";
            return null;
        }

        private static string? ValidatePassword(string p)
        {
            if (string.IsNullOrEmpty(p)) return "Please enter a password.";
            if (p.Length < 8) return "Password must be at least 8 characters.";
            if (!HasLetterRegex.IsMatch(p) || !HasDigitRegex.IsMatch(p))
                return "Password must include both letters and numbers.";
            return null;
        }

        private static string? ValidateConfirmPassword(string pass, string confirm)
        {
            if (string.IsNullOrEmpty(confirm)) return "Please confirm your password.";
            if (pass != confirm) return "Passwords do not match.";
            return null;
        }

        private static string? ValidateFullName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "Please enter your full name.";
            if (name.Length < 2 || name.Length > 100) return "Full name must be 2-100 characters.";
            return null;
        }

        private static string? ValidateEmailOptional(string email)
        {
            if (string.IsNullOrEmpty(email)) return null;
            if (!EmailRegex.IsMatch(email)) return "Invalid email address.";
            return null;
        }

        private void txtUsername_TextChanged(object? sender, EventArgs e)
        {
            string u = txtUsername.Text.Trim();
            lblErrUsername.Text = string.IsNullOrEmpty(u) ? "" : (ValidateUsername(u) ?? "");
            nightPanel1.Invalidate();
        }

        private void txtPassword_TextChanged(object? sender, EventArgs e)
        {
            string p = txtPassword.Text;
            lblErrPassword.Text = string.IsNullOrEmpty(p) ? "" : (ValidatePassword(p) ?? "");

            if (!string.IsNullOrEmpty(txtConfirmPassword.Text))
                lblErrConfirmPassword.Text = ValidateConfirmPassword(p, txtConfirmPassword.Text) ?? "";

            nightPanel1.Invalidate();
        }

        private void txtConfirmPassword_TextChanged(object? sender, EventArgs e)
        {
            string c = txtConfirmPassword.Text;
            lblErrConfirmPassword.Text = string.IsNullOrEmpty(c) ? "" : (ValidateConfirmPassword(txtPassword.Text, c) ?? "");
            nightPanel1.Invalidate();
        }

        private void txtFullName_TextChanged(object? sender, EventArgs e)
        {
            string fn = txtFullName.Text.Trim();
            lblErrFullName.Text = string.IsNullOrEmpty(fn) ? "" : (ValidateFullName(fn) ?? "");
            nightPanel1.Invalidate();
        }

        private void txtEmail_TextChanged(object? sender, EventArgs e)
        {
            string em = txtEmail.Text.Trim();
            lblErrEmail.Text = ValidateEmailOptional(em) ?? "";
            nightPanel1.Invalidate();
        }

        private void btnTogglePassword_Click(object? sender, EventArgs e)
        {
            isPasswordVisible = !isPasswordVisible;
            txtPassword.UseSystemPasswordChar = !isPasswordVisible;
            btnTogglePassword.Text = isPasswordVisible ? "🙈" : "👁";
        }

        private void btnToggleConfirmPassword_Click(object? sender, EventArgs e)
        {
            isConfirmPasswordVisible = !isConfirmPasswordVisible;
            txtConfirmPassword.UseSystemPasswordChar = !isConfirmPasswordVisible;
            btnToggleConfirmPassword.Text = isConfirmPasswordVisible ? "🙈" : "👁";
        }

        private void btnHelp_Click(object? sender, EventArgs e)
        {
            string rules =
                "• Username: 3-20 characters, letters, numbers, or underscore (_) only.\n" +
                "• Password: At least 8 characters, including both letters and numbers.\n" +
                "• Confirm Password: Must match Password exactly.\n" +
                "• Full Name: Required, 2-100 characters.\n" +
                "• Email (optional): Leave blank or use a valid format (name@domain.com).\n" +
                "• Birthday: Format dd/MM/yyyy, between 01/01/1920 and today.\n" +
                "• Shortcuts: Tab moves between fields, Enter submits Register.";

            using GuidelinesDialog dlg = new GuidelinesDialog(
                "REGISTER REQUIREMENTS",
                "Create a new racer account",
                rules);
            dlg.ShowDialog(this);
        }
        private async void btnRegister_Click(object? sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text;
            string confirmPassword = txtConfirmPassword.Text;
            string fullName = txtFullName.Text.Trim();
            string email = txtEmail.Text.Trim();
            DateTime birthday = dtpBirthday.Value.Date;

            bool hasError = false;

            string? e1 = ValidateUsername(username);
            lblErrUsername.Text = e1 ?? "";
            if (e1 != null) hasError = true;

            string? e2 = ValidatePassword(password);
            lblErrPassword.Text = e2 ?? "";
            if (e2 != null) hasError = true;

            string? e3 = ValidateConfirmPassword(password, confirmPassword);
            lblErrConfirmPassword.Text = e3 ?? "";
            if (e3 != null) hasError = true;

            string? e4 = ValidateFullName(fullName);
            lblErrFullName.Text = e4 ?? "";
            if (e4 != null) hasError = true;

            string? e5 = ValidateEmailOptional(email);
            lblErrEmail.Text = e5 ?? "";
            if (e5 != null) hasError = true;

            if (birthday > DateTime.Today || birthday < dtpBirthday.MinDate)
            {
                MessageBox.Show("Invalid birthday.", "Registration", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                hasError = true;
            }

            nightPanel1.Invalidate();
            if (hasError) return;

            btnRegister.Enabled = false;
            btnCancel.Enabled = false;
            try
            {
                ResponsePacket resp = await _netClient.RegisterAsync(username, password, fullName, email, birthday);
                if (!resp.Success)
                {
                    MessageBox.Show(
                        string.IsNullOrWhiteSpace(resp.Message) ? "Registration failed." : resp.Message,
                        "Registration",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                MessageBox.Show(
                    "Registration successful! You can now log in.",
                    "Registration",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                QuayVeLoginForm();
            }
            finally
            {
                if (!IsDisposed)
                {
                    btnRegister.Enabled = true;
                    btnCancel.Enabled = true;
                }
            }
        }

        private void btnCancel_Click(object? sender, EventArgs e)
        {
            QuayVeLoginForm();
        }

        private void QuayVeLoginForm()
        {
            if (isReturningToLogin) return;
            isReturningToLogin = true;
            Close();
        }

        private void btnExit_Click(object? sender, EventArgs e)
        {
            QuayVeLoginForm();
        }

        private void btnMaximize_Click(object? sender, EventArgs e)
        {
            WindowState = WindowState == FormWindowState.Normal
                ? FormWindowState.Maximized
                : FormWindowState.Normal;
            BeginInvoke(new Action(CanGiuaBoCuc));
        }

        private void btnMinimize_Click(object? sender, EventArgs e)
        {
            WindowState = FormWindowState.Minimized;
        }
    }
}