using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using DanToPhong.Protocol;
using UI.Services;

namespace UI
{
    public partial class LoginForm : Form
    {
        private sealed class RememberedLogin
        {
            public bool RememberMe { get; set; }
            public string Username { get; set; } = string.Empty;
        }

        private static readonly string RememberFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DanToPhong",
            "remember_login.json");

        private static readonly Regex UsernameRegex = new(@"^[a-zA-Z0-9_]{3,20}$", RegexOptions.Compiled);

        private readonly NetworkClientService _netClient;
        private bool isPasswordVisible;
        private string? lastLoginError;
        private bool isNavigating;

        public LoginForm(NetworkClientService netClient)
        {
            _netClient = netClient;
            InitializeComponent();
            DoubleBuffered = true;
            AcceptButton = btnLogin;

            txtUsername.GotFocus += (_, _) => nightPanel1.Invalidate();
            txtUsername.LostFocus += (_, _) => nightPanel1.Invalidate();
            txtPassword.GotFocus += (_, _) => nightPanel1.Invalidate();
            txtPassword.LostFocus += (_, _) => nightPanel1.Invalidate();
            nightPanel1.Paint += VeVienDuoiTextBox;

            TaiThongTinGhiNho();
            CanGiuaBoCuc();
        }

        private void TaiThongTinGhiNho()
        {
            try
            {
                if (!File.Exists(RememberFilePath)) return;
                string json = File.ReadAllText(RememberFilePath);
                RememberedLogin? data = JsonSerializer.Deserialize<RememberedLogin>(json);
                if (data != null && data.RememberMe && !string.IsNullOrWhiteSpace(data.Username))
                {
                    chkRememberMe.Checked = true;
                    txtUsername.Text = data.Username;
                }
            }
            catch
            {
            }
        }

        private void LuuThongTinGhiNho(string username)
        {
            try
            {
                string? dir = Path.GetDirectoryName(RememberFilePath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                RememberedLogin data = new RememberedLogin
                {
                    RememberMe = chkRememberMe.Checked,
                    Username = chkRememberMe.Checked ? username : string.Empty
                };
                File.WriteAllText(RememberFilePath, JsonSerializer.Serialize(data));
            }
            catch
            {
            }
        }

        private void VeVienDuoiTextBox(object? sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            VeDuongVien(e.Graphics, txtUsername, null, lblErrUsername.Text.Length > 0);
            VeDuongVien(e.Graphics, txtPassword, btnTogglePassword, lblErrPassword.Text.Length > 0);
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
            int y = topOffset + Math.Max(16, (usableHeight - nightPanel1.Height) / 2);

            nightPanel1.Location = new Point(x, y);
            panelShadow.Size = nightPanel1.Size;
            panelShadow.Location = new Point(x + 6, y + 6);

            panelShadow.SendToBack();
            nightPanel1.BringToFront();

            lblTitle.Location = new Point((nightPanel1.Width - lblTitle.Width) / 2, 16);
            lblSubtitle.Location = new Point((nightPanel1.Width - lblSubtitle.Width) / 2, lblTitle.Bottom + 2);
            btnHelp.Location = new Point(nightPanel1.Width - btnHelp.Width - 14, 14);

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

        private static string? ValidateUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return "Please enter a username.";
            if (!UsernameRegex.IsMatch(username))
                return "Username must be 3-20 chars (letters, numbers, _).";
            return null;
        }

        private void txtUsername_TextChanged(object? sender, EventArgs e)
        {
            lastLoginError = null;
            string u = txtUsername.Text.Trim();
            lblErrUsername.Text = string.IsNullOrEmpty(u) ? "" : (ValidateUsername(u) ?? "");
            nightPanel1.Invalidate();
        }

        private void txtPassword_TextChanged(object? sender, EventArgs e)
        {
            lastLoginError = null;
            if (!string.IsNullOrEmpty(txtPassword.Text))
                lblErrPassword.Text = "";
            nightPanel1.Invalidate();
        }

        private void chkRememberMe_CheckedChanged(object? sender, EventArgs e)
        {
            if (!chkRememberMe.Checked)
                LuuThongTinGhiNho(string.Empty);
        }

        private void btnTogglePassword_Click(object? sender, EventArgs e)
        {
            isPasswordVisible = !isPasswordVisible;
            txtPassword.UseSystemPasswordChar = !isPasswordVisible;
            btnTogglePassword.Text = isPasswordVisible ? "🙈" : "👁";
        }

        private void btnHelp_Click(object? sender, EventArgs e)
        {
            string rules =
                "• Username: 3-20 characters, letters (a-z, A-Z), numbers (0-9), or underscore (_) only.\n" +
                "• Password: Required.\n" +
                "• Remember me: Saves your username locally for faster sign-in.\n" +
                "• Shortcuts: Tab moves between fields, Enter submits Login.";

            using GuidelinesDialog dlg = new GuidelinesDialog(
                "LOGIN REQUIREMENTS",
                "Sign in with your racer credentials",
                rules);
            dlg.ShowDialog(this);
        }

        private void MoFormConVaTroVe(Form childForm, bool clearPasswordOnReturn, bool syncSizeFromLogin)
        {
            if (isNavigating) return;
            isNavigating = true;

            childForm.StartPosition = FormStartPosition.Manual;
            if (syncSizeFromLogin)
                childForm.Size = Size;
            childForm.Location = Location;
            childForm.WindowState = WindowState;

            childForm.FormClosed += (_, _) =>
            {
                if (syncSizeFromLogin)
                    Size = childForm.Size;
                Location = childForm.Location;
                WindowState = childForm.WindowState;

                if (clearPasswordOnReturn)
                {
                    txtPassword.Clear();
                    lblErrPassword.Text = "";
                }

                if (!chkRememberMe.Checked)
                {
                    txtUsername.Clear();
                    lblErrUsername.Text = "";
                }

                isNavigating = false;
                Show();
                CanGiuaBoCuc();
                nightPanel1.Invalidate();
            };

            Hide();
            childForm.Show();
        }

        private async void btnLogin_Click(object? sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text;

            bool hasError = false;

            string? userErr = ValidateUsername(username);
            if (userErr != null)
            {
                lblErrUsername.Text = userErr;
                hasError = true;
            }
            else
            {
                lblErrUsername.Text = "";
            }

            if (string.IsNullOrEmpty(password))
            {
                lblErrPassword.Text = "Please enter a password.";
                hasError = true;
            }
            else
            {
                lblErrPassword.Text = "";
            }

            nightPanel1.Invalidate();
            if (hasError) return;

            if (!string.IsNullOrEmpty(lastLoginError))
            {
                lblErrPassword.Text = lastLoginError;
                nightPanel1.Invalidate();
                return;
            }

            btnLogin.Enabled = false;
            try
            {
                ResponsePacket resp = await _netClient.LoginAsync(username, password);
                if (!resp.Success)
                {
                    lastLoginError = resp.Code == ErrorCodes.AuthFailed
                        ? "Invalid username or password."
                        : string.IsNullOrWhiteSpace(resp.Message)
                            ? "Unable to sign in right now. Please try again."
                            : resp.Message;

                    lblErrPassword.Text = lastLoginError;
                    nightPanel1.Invalidate();
                    return;
                }

                lastLoginError = null;
                LuuThongTinGhiNho(username);

                UserInfoDto currentUser = resp.User ?? new UserInfoDto
                {
                    Username = username,
                    FullName = username
                };

                MainForm mainForm = new MainForm(_netClient, currentUser);
                MoFormConVaTroVe(mainForm, clearPasswordOnReturn: true, syncSizeFromLogin: true);
            }
            finally
            {
                if (!IsDisposed)
                    btnLogin.Enabled = true;
            }
        }

        private void btnRegister_Click(object? sender, EventArgs e)
        {
            RegisterForm regForm = new RegisterForm(_netClient);
            MoFormConVaTroVe(regForm, clearPasswordOnReturn: false, syncSizeFromLogin: false);
        }

        private void btnForgotPassword_Click(object? sender, EventArgs e)
        {
            ForgotPasswordForm forgotForm = new ForgotPasswordForm(_netClient, txtUsername.Text.Trim());
            MoFormConVaTroVe(forgotForm, clearPasswordOnReturn: false, syncSizeFromLogin: true);
        }

        private void btnExit_Click(object? sender, EventArgs e)
        {
            if (isNavigating) return;
            _netClient.Disconnect();
            Application.Exit();
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