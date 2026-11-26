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
    public partial class ForgotPasswordForm : Form
    {
        private static readonly Regex UsernameRegex = new(@"^[a-zA-Z0-9_]{3,20}$", RegexOptions.Compiled);
        private static readonly Regex HasLetterRegex = new(@"[A-Za-z]", RegexOptions.Compiled);
        private static readonly Regex HasDigitRegex = new(@"[0-9]", RegexOptions.Compiled);
        private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
        private static readonly Regex CodeRegex = new(@"^[0-9]{6}$", RegexOptions.Compiled);

        private readonly NetworkClientService _netClient;
        private readonly System.Windows.Forms.Timer _resendTimer = new();
        private int _resendCooldown;
        private string? _otpUsername;
        private string? _otpEmail;
        private DateTime? _codeExpiresAt;
        private bool isNewPasswordVisible;
        private bool isConfirmPasswordVisible;
        private bool isReturningToLogin;

        public ForgotPasswordForm(NetworkClientService netClient, string? initialUsername = null)
        {
            _netClient = netClient;
            InitializeComponent();
            DoubleBuffered = true;
            AcceptButton = btnResetPassword;

            txtNewPassword.PlaceholderText = "Min 8 chars, letters and numbers";
            txtVerificationCode.MaxLength = 6;

            CauHinhNutIconVaCode();
            CauHinhNutDoExit();

            if (!string.IsNullOrWhiteSpace(initialUsername))
                txtUsername.Text = initialUsername;

            _resendTimer.Interval = 1000;
            _resendTimer.Tick += ResendTimer_Tick;

            TextBox[] fields = new[] { txtUsername, txtEmail, txtNewPassword, txtConfirmPassword, txtVerificationCode };
            foreach (TextBox tb in fields)
            {
                tb.GotFocus += (_, _) => nightPanel1.Invalidate();
                tb.LostFocus += (_, _) => nightPanel1.Invalidate();
            }

            nightPanel1.Paint += VeVienDuoiCacO;
            FormClosing += ForgotPasswordForm_FormClosing;

            CapNhatAvatarMacDinh();
            CanGiuaBoCuc();
        }

        private void CauHinhNutDoExit()
        {
            if (btnExit == null) return;

            btnExit.FlatStyle = FlatStyle.Flat;
            btnExit.FlatAppearance.BorderSize = 0;
            btnExit.UseVisualStyleBackColor = false;
            btnExit.BackColor = Color.FromArgb(155, 17, 24);
            btnExit.ForeColor = Color.White;
            btnExit.FlatAppearance.MouseOverBackColor = Color.FromArgb(185, 24, 32);
            btnExit.FlatAppearance.MouseDownBackColor = Color.FromArgb(120, 12, 18);
        }

        private void CauHinhNutIconVaCode()
        {
            Color cardBg = Color.FromArgb(253, 251, 247);

            foreach (System.Windows.Forms.Button eyeBtn in new[] { btnToggleNewPassword, btnToggleConfirmPassword })
            {
                eyeBtn.Text = "👁";
                eyeBtn.BackColor = cardBg;
                eyeBtn.ForeColor = Color.Black;
                eyeBtn.Font = new Font("Segoe UI Emoji", 7F, FontStyle.Regular, GraphicsUnit.Point);
                eyeBtn.FlatStyle = FlatStyle.Flat;
                eyeBtn.FlatAppearance.BorderSize = 0;
                eyeBtn.FlatAppearance.MouseOverBackColor = Color.FromArgb(242, 239, 233);
                eyeBtn.FlatAppearance.MouseDownBackColor = Color.FromArgb(230, 226, 218);
                eyeBtn.Padding = new Padding(0, 1, 0, 1);
                eyeBtn.Margin = Padding.Empty;
                eyeBtn.TextAlign = ContentAlignment.MiddleCenter;
            }

            btnSendCode.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            btnSendCode.Padding = Padding.Empty;
            btnSendCode.Margin = Padding.Empty;
            btnSendCode.TextAlign = ContentAlignment.MiddleCenter;

            btnResetPassword.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
            btnResetPassword.Padding = Padding.Empty;
            btnResetPassword.TextAlign = ContentAlignment.MiddleCenter;

            btnCancel.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
            btnCancel.Padding = Padding.Empty;
            btnCancel.TextAlign = ContentAlignment.MiddleCenter;
        }

        private void ForgotPasswordForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            _resendTimer.Stop();
            _resendTimer.Dispose();
            picAvatar.Image?.Dispose();
        }

        private void ResendTimer_Tick(object? sender, EventArgs e)
        {
            if (_resendCooldown > 1)
            {
                _resendCooldown--;
                btnSendCode.Text = $"{_resendCooldown}s";
            }
            else
            {
                _resendCooldown = 0;
                _resendTimer.Stop();
                btnSendCode.Enabled = true;
                btnSendCode.Text = "Get Code";
            }
        }

        private void VeVienDuoiCacO(object? sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            VeDuongVien(e.Graphics, txtUsername, null, lblErrUsername.Text.Length > 0);
            VeDuongVien(e.Graphics, txtEmail, null, lblErrEmail.Text.Length > 0);
            VeDuongVien(e.Graphics, txtNewPassword, btnToggleNewPassword, lblErrNewPassword.Text.Length > 0);
            VeDuongVien(e.Graphics, txtConfirmPassword, btnToggleConfirmPassword, lblErrConfirmPassword.Text.Length > 0);
            // Thông báo gửi mã thành công (màu xanh) không được tính là lỗi
            VeDuongVien(e.Graphics, txtVerificationCode, null,
                lblErrVerificationCode.Text.Length > 0 && lblErrVerificationCode.ForeColor == Color.Crimson);
        }

        private static void VeDuongVien(Graphics g, TextBox txt, Control? rightControl, bool hasError)
        {
            Color borderColor = hasError
                ? Color.Crimson
                : txt.Focused
                    ? Color.FromArgb(43, 27, 61)
                    : Color.Silver;

            float thickness = hasError || txt.Focused ? 2.2f : 1.5f;

            using Pen pen = new Pen(borderColor, thickness);
            int y = txt.Bottom + 4;
            int rightEdge = rightControl != null ? rightControl.Right : txt.Right;
            g.DrawLine(pen, txt.Left - 1, y, rightEdge, y);
        }

        private void CapNhatAvatarMacDinh()
        {
            if (picAvatar == null || picAvatar.Width <= 0 || picAvatar.Height <= 0) return;

            BoGocTron(picAvatar);

            string name = txtUsername.Text.Trim();
            string initials = LayChuCaiVietTat(string.IsNullOrWhiteSpace(name) ? "R" : name);

            picAvatar.Image?.Dispose();
            float fontSize = Math.Max(14f, picAvatar.Height * 0.34f);
            picAvatar.Image = TaoAvatarChuCai(initials, picAvatar.Width, picAvatar.Height, fontSize);
        }

        private static string LayChuCaiVietTat(string name)
        {
            string trimmed = name.Trim();
            if (string.IsNullOrEmpty(trimmed)) return "R";

            string[] parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
                return parts[0][0].ToString().ToUpperInvariant();

            return $"{char.ToUpperInvariant(parts[0][0])}{char.ToUpperInvariant(parts[^1][0])}";
        }

        private static Bitmap TaoAvatarChuCai(string text, int width, int height, float fontSize)
        {
            Bitmap bmp = new Bitmap(Math.Max(16, width), Math.Max(16, height));
            using Graphics g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            using SolidBrush bgBrush = new SolidBrush(Color.FromArgb(92, 45, 145));
            g.FillRectangle(bgBrush, 0, 0, bmp.Width, bmp.Height);

            using Font font = new Font("Segoe UI", fontSize, FontStyle.Bold, GraphicsUnit.Point);
            using SolidBrush textBrush = new SolidBrush(Color.FromArgb(245, 208, 120));
            using StringFormat sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };

            g.DrawString(text, font, textBrush, new RectangleF(0, 0, bmp.Width, bmp.Height), sf);
            return bmp;
        }

        private static void BoGocTron(Control ctrl, int radius = 999)
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

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            Rectangle workArea = Screen.FromControl(this).WorkingArea;
            int width = Math.Min(ClientSize.Width, Math.Max(400, workArea.Width - 16));
            int height = Math.Min(ClientSize.Height, Math.Max(400, workArea.Height - 16));
            if (width != ClientSize.Width || height != ClientSize.Height)
                ClientSize = new Size(width, height);
            CanGiuaBoCuc();
        }

        private void CanGiuaBoCuc()
        {
            if (nightPanel1 == null || panelShadow == null || WindowState == FormWindowState.Minimized) return;

            SuspendLayout();

            int topOffset = pnlTitleBar.Height + pnlHeaderBar.Height;
            int usableHeight = Math.Max(0, ClientSize.Height - topOffset);
            bool twoCols = ClientSize.Width >= 780 && usableHeight >= 540;

            int cardW = twoCols
                ? Math.Min(860, Math.Max(700, ClientSize.Width - 100))
                : Math.Min(580, Math.Max(420, ClientSize.Width - 48));
            int cardH = twoCols
                ? Math.Min(600, Math.Max(460, usableHeight - 12))
                : Math.Min(650, usableHeight - 10);
            nightPanel1.Size = new Size(cardW, cardH);
            nightPanel1.AutoScroll = cardW < 580 || cardH < 640;
            nightPanel1.AutoScrollMinSize = nightPanel1.AutoScroll ? new Size(580, 640) : Size.Empty;

            int leftMargin = 44;
            int contentWidth = nightPanel1.Width - leftMargin * 2;
            int headerW = Math.Max(240, nightPanel1.Width - 120);

            lblTitle.Size = new Size(headerW, 30);
            lblTitle.Location = new Point((nightPanel1.Width - lblTitle.Width) / 2, 10);

            lblSubtitle.Size = new Size(headerW, 19);
            lblSubtitle.Location = new Point((nightPanel1.Width - lblSubtitle.Width) / 2, lblTitle.Bottom + 3);

            btnHelp.Size = new Size(34, 34);
            btnHelp.Location = new Point(nightPanel1.Width - btnHelp.Width - 18, 12);

            int avatarSize = twoCols ? 54 : 48;
            picAvatar.Size = new Size(avatarSize, avatarSize);
            picAvatar.Location = new Point((nightPanel1.Width - avatarSize) / 2, lblSubtitle.Bottom + 10);
            CapNhatAvatarMacDinh();

            int labelH = 19;
            int inputH = 22;
            int errH = 16;
            int labelGap = 5;
            int errGap = 7;
            int rowGap = 15;

            int eyeW = 30;
            int eyeH = 26;
            int eyeGap = 6;

            if (twoCols)
            {
                int colGap = 40;
                int colW = (contentWidth - colGap) / 2;
                int rightColX = leftMargin + colW + colGap;
                int passInputW = Math.Max(140, colW - eyeW - eyeGap);

                int curY = picAvatar.Bottom + 14;

                lblUsername.Location = new Point(leftMargin, curY);
                lblUsername.Size = new Size(colW, labelH);
                lblEmail.Location = new Point(rightColX, curY);
                lblEmail.Size = new Size(colW, labelH);

                int inputY = curY + labelH + labelGap;
                txtUsername.Location = new Point(leftMargin, inputY);
                txtUsername.Size = new Size(colW, inputH);
                txtEmail.Location = new Point(rightColX, inputY);
                txtEmail.Size = new Size(colW, inputH);

                int errY = inputY + inputH + errGap;
                lblErrUsername.Location = new Point(leftMargin, errY);
                lblErrUsername.Size = new Size(colW, errH);
                lblErrEmail.Location = new Point(rightColX, errY);
                lblErrEmail.Size = new Size(colW, errH);

                curY = errY + errH + rowGap;
                lblNewPassword.Location = new Point(leftMargin, curY);
                lblNewPassword.Size = new Size(colW, labelH);
                lblConfirmPassword.Location = new Point(rightColX, curY);
                lblConfirmPassword.Size = new Size(colW, labelH);

                inputY = curY + labelH + labelGap;
                txtNewPassword.Location = new Point(leftMargin, inputY);
                txtNewPassword.Size = new Size(passInputW, inputH);
                btnToggleNewPassword.Size = new Size(eyeW, eyeH);
                btnToggleNewPassword.Location = new Point(leftMargin + colW - eyeW, inputY - 2);

                txtConfirmPassword.Location = new Point(rightColX, inputY);
                txtConfirmPassword.Size = new Size(passInputW, inputH);
                btnToggleConfirmPassword.Size = new Size(eyeW, eyeH);
                btnToggleConfirmPassword.Location = new Point(rightColX + colW - eyeW, inputY - 2);

                errY = inputY + inputH + errGap;
                lblErrNewPassword.Location = new Point(leftMargin, errY);
                lblErrNewPassword.Size = new Size(colW, errH);
                lblErrConfirmPassword.Location = new Point(rightColX, errY);
                lblErrConfirmPassword.Size = new Size(colW, errH);

                curY = errY + errH + rowGap;
                int sendBtnW = 126;
                int sendBtnH = 34;
                int sendGap = 12;
                int codeInputW = Math.Max(160, colW - sendBtnW - sendGap);

                lblVerificationCode.Location = new Point(leftMargin, curY);
                lblVerificationCode.Size = new Size(colW, labelH);

                inputY = curY + labelH + labelGap;
                txtVerificationCode.Location = new Point(leftMargin, inputY);
                txtVerificationCode.Size = new Size(codeInputW, inputH);
                btnSendCode.Size = new Size(sendBtnW, sendBtnH);
                btnSendCode.Location = new Point(leftMargin + colW - sendBtnW, inputY - 6);

                errY = Math.Max(inputY + inputH, btnSendCode.Bottom) + errGap;
                lblErrVerificationCode.Location = new Point(leftMargin, errY);
                lblErrVerificationCode.Size = new Size(contentWidth, errH);

                int btnH = 40;
                int actionGap = 18;
                int resetW = (int)(contentWidth * 0.65);
                int cancelW = contentWidth - resetW - actionGap;
                int btnTop = errY + errH + 16;

                btnResetPassword.Size = new Size(resetW, btnH);
                btnResetPassword.Location = new Point(leftMargin, btnTop);

                btnCancel.Size = new Size(cancelW, btnH);
                btnCancel.Location = new Point(leftMargin + resetW + actionGap, btnTop);

                nightPanel1.Height = Math.Min(usableHeight - 10, btnCancel.Bottom + 18);
            }
            else
            {
                int curY = picAvatar.Bottom + 10;
                int passInputW = Math.Max(150, contentWidth - eyeW - eyeGap);

                lblUsername.Location = new Point(leftMargin, curY);
                lblUsername.Size = new Size(contentWidth, labelH);
                curY = lblUsername.Bottom + 4;
                txtUsername.Location = new Point(leftMargin, curY);
                txtUsername.Size = new Size(contentWidth, inputH);
                curY = txtUsername.Bottom + 5;
                lblErrUsername.Location = new Point(leftMargin, curY);
                lblErrUsername.Size = new Size(contentWidth, errH);
                curY = lblErrUsername.Bottom + 9;

                lblEmail.Location = new Point(leftMargin, curY);
                lblEmail.Size = new Size(contentWidth, labelH);
                curY = lblEmail.Bottom + 4;
                txtEmail.Location = new Point(leftMargin, curY);
                txtEmail.Size = new Size(contentWidth, inputH);
                curY = txtEmail.Bottom + 5;
                lblErrEmail.Location = new Point(leftMargin, curY);
                lblErrEmail.Size = new Size(contentWidth, errH);
                curY = lblErrEmail.Bottom + 9;

                lblNewPassword.Location = new Point(leftMargin, curY);
                lblNewPassword.Size = new Size(contentWidth, labelH);
                curY = lblNewPassword.Bottom + 4;
                txtNewPassword.Location = new Point(leftMargin, curY);
                txtNewPassword.Size = new Size(passInputW, inputH);
                btnToggleNewPassword.Size = new Size(eyeW, eyeH);
                btnToggleNewPassword.Location = new Point(leftMargin + contentWidth - eyeW, curY - 1);
                curY = Math.Max(txtNewPassword.Bottom, btnToggleNewPassword.Bottom) + 5;
                lblErrNewPassword.Location = new Point(leftMargin, curY);
                lblErrNewPassword.Size = new Size(contentWidth, errH);
                curY = lblErrNewPassword.Bottom + 9;

                lblConfirmPassword.Location = new Point(leftMargin, curY);
                lblConfirmPassword.Size = new Size(contentWidth, labelH);
                curY = lblConfirmPassword.Bottom + 4;
                txtConfirmPassword.Location = new Point(leftMargin, curY);
                txtConfirmPassword.Size = new Size(passInputW, inputH);
                btnToggleConfirmPassword.Size = new Size(eyeW, eyeH);
                btnToggleConfirmPassword.Location = new Point(leftMargin + contentWidth - eyeW, curY - 1);
                curY = Math.Max(txtConfirmPassword.Bottom, btnToggleConfirmPassword.Bottom) + 5;
                lblErrConfirmPassword.Location = new Point(leftMargin, curY);
                lblErrConfirmPassword.Size = new Size(contentWidth, errH);
                curY = lblErrConfirmPassword.Bottom + 9;

                int sendBtnW = 122;
                int sendBtnH = 32;
                int sendGap = 10;
                int codeInputW = Math.Max(140, contentWidth - sendBtnW - sendGap);
                lblVerificationCode.Location = new Point(leftMargin, curY);
                lblVerificationCode.Size = new Size(contentWidth, labelH);
                curY = lblVerificationCode.Bottom + 4;
                txtVerificationCode.Location = new Point(leftMargin, curY);
                txtVerificationCode.Size = new Size(codeInputW, inputH);
                btnSendCode.Size = new Size(sendBtnW, sendBtnH);
                btnSendCode.Location = new Point(leftMargin + contentWidth - sendBtnW, curY - 5);
                curY = Math.Max(txtVerificationCode.Bottom, btnSendCode.Bottom) + 5;
                lblErrVerificationCode.Location = new Point(leftMargin, curY);
                lblErrVerificationCode.Size = new Size(contentWidth, errH);

                int compactBtnH = 34;
                int compactBtnGap = 8;
                int btnTop = lblErrVerificationCode.Bottom + 12;

                btnResetPassword.Size = new Size(contentWidth, compactBtnH);
                btnResetPassword.Location = new Point(leftMargin, btnTop);

                btnCancel.Size = new Size(contentWidth, compactBtnH);
                btnCancel.Location = new Point(leftMargin, btnResetPassword.Bottom + compactBtnGap);

                nightPanel1.Height = Math.Min(usableHeight - 10, btnCancel.Bottom + 16);
            }

            btnToggleNewPassword.BringToFront();
            btnToggleConfirmPassword.BringToFront();

            int x = Math.Max(12, (ClientSize.Width - nightPanel1.Width) / 2);
            int y = topOffset + Math.Max(4, (usableHeight - nightPanel1.Height) / 2);

            nightPanel1.Location = new Point(x, y);
            panelShadow.Size = nightPanel1.Size;
            panelShadow.Location = new Point(x + 5, y + 5);

            panelShadow.SendToBack();
            nightPanel1.BringToFront();

            BoGocControl(nightPanel1, 16);
            BoGocControl(panelShadow, 16);
            nightPanel1.Invalidate();

            ResumeLayout(true);
        }

        private static void BoGocControl(Control ctrl, int radius = 999)
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
            if (string.IsNullOrEmpty(u)) return "Please enter your username.";
            if (!UsernameRegex.IsMatch(u)) return "3-20 chars: letters, numbers, or _ only.";
            return null;
        }

        private static string? ValidateEmailRequired(string em)
        {
            if (string.IsNullOrEmpty(em)) return "Please enter your registered email.";
            if (!EmailRegex.IsMatch(em)) return "Invalid email address.";
            return null;
        }

        private string? ValidateVerificationCode(string code)
        {
            if (string.IsNullOrEmpty(code)) return "Please enter the verification code.";
            if (!CodeRegex.IsMatch(code)) return "Verification code must be 6 digits.";
            return null;
        }

        private static string? ValidatePassword(string p)
        {
            if (string.IsNullOrEmpty(p)) return "Please enter a new password.";
            if (p.Length < 8) return "Password must be at least 8 characters.";
            if (!HasLetterRegex.IsMatch(p) || !HasDigitRegex.IsMatch(p))
                return "Password must include both letters and numbers.";
            return null;
        }

        private static string? ValidateConfirmPassword(string pass, string confirm)
        {
            if (string.IsNullOrEmpty(confirm)) return "Please confirm your new password.";
            if (pass != confirm) return "Passwords do not match.";
            return null;
        }

        private void txtUsername_TextChanged(object? sender, EventArgs e)
        {
            string u = txtUsername.Text.Trim();
            lblErrUsername.Text = string.IsNullOrEmpty(u) ? "" : (ValidateUsername(u) ?? "");
            CapNhatAvatarMacDinh();
            nightPanel1.Invalidate();
        }

        private void txtEmail_TextChanged(object? sender, EventArgs e)
        {
            string em = txtEmail.Text.Trim();
            lblErrEmail.Text = string.IsNullOrEmpty(em) ? "" : (ValidateEmailRequired(em) ?? "");
            nightPanel1.Invalidate();
        }

        private void txtVerificationCode_TextChanged(object? sender, EventArgs e)
        {
            string c = txtVerificationCode.Text.Trim();
            lblErrVerificationCode.ForeColor = Color.Crimson;
            lblErrVerificationCode.Text = string.IsNullOrEmpty(c) ? "" : (ValidateVerificationCode(c) ?? "");
            nightPanel1.Invalidate();
        }

        private void txtNewPassword_TextChanged(object? sender, EventArgs e)
        {
            string p = txtNewPassword.Text;
            lblErrNewPassword.Text = string.IsNullOrEmpty(p) ? "" : (ValidatePassword(p) ?? "");

            if (!string.IsNullOrEmpty(txtConfirmPassword.Text))
                lblErrConfirmPassword.Text = ValidateConfirmPassword(p, txtConfirmPassword.Text) ?? "";

            nightPanel1.Invalidate();
        }

        private void txtConfirmPassword_TextChanged(object? sender, EventArgs e)
        {
            string c = txtConfirmPassword.Text;
            lblErrConfirmPassword.Text = string.IsNullOrEmpty(c) ? "" : (ValidateConfirmPassword(txtNewPassword.Text, c) ?? "");
            nightPanel1.Invalidate();
        }

        private async void btnSendCode_Click(object? sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string email = txtEmail.Text.Trim();

            string? e1 = ValidateUsername(username);
            string? e2 = ValidateEmailRequired(email);
            lblErrUsername.Text = e1 ?? "";
            lblErrEmail.Text = e2 ?? "";
            lblErrVerificationCode.Text = "";
            nightPanel1.Invalidate();

            if (e1 != null || e2 != null) return;

            _otpUsername = null;
            _otpEmail = null;
            _codeExpiresAt = null;
            btnSendCode.Enabled = false;
            string previousText = btnSendCode.Text;
            btnSendCode.Text = "Sending...";

            try
            {
                ResponsePacket response = await _netClient.RequestResetCodeAsync(username, email);
                if (IsDisposed) return;
                if (!response.Success)
                    throw new InvalidOperationException(string.IsNullOrWhiteSpace(response.Message)
                        ? "Could not request a verification code." : response.Message);

                _otpUsername = username;
                _otpEmail = email;
                _codeExpiresAt = DateTime.UtcNow.AddMinutes(10);

                lblErrVerificationCode.ForeColor = Color.FromArgb(22, 101, 52);
                lblErrVerificationCode.Text = response.Message;
                nightPanel1.Invalidate();
                txtVerificationCode.Focus();

                _resendCooldown = 60;
                btnSendCode.Text = $"{_resendCooldown}s";
                _resendTimer.Start();
            }
            catch (Exception ex)
            {
                if (IsDisposed) return;
                btnSendCode.Enabled = true;
                btnSendCode.Text = string.IsNullOrWhiteSpace(previousText) ? "Get Code" : previousText;

                lblErrVerificationCode.ForeColor = Color.Crimson;
                lblErrVerificationCode.Text = $"Send failed: {ex.Message}";
                nightPanel1.Invalidate();

                PopupNoticeForm.ShowNotice(
                    this,
                    "Mail Error",
                    $"Không gửi được email xác nhận:\n{ex.Message}",
                    PopupNoticeForm.NoticeKind.Error);
            }
        }

        private void btnToggleNewPassword_Click(object? sender, EventArgs e)
        {
            isNewPasswordVisible = !isNewPasswordVisible;
            txtNewPassword.UseSystemPasswordChar = !isNewPasswordVisible;
            btnToggleNewPassword.Text = isNewPasswordVisible ? "🙈" : "👁";
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
                "• Registered Email: Required, valid email format matching your account.\n" +
                "• Verification Code: Click 'Get Code' to receive a 6-digit code and enter it here.\n" +
                "• New Password: At least 8 characters, including both letters and numbers.\n" +
                "• Confirm Password: Must match New Password exactly.\n" +
                "• Shortcuts: Tab moves between fields, Enter submits Reset Password.";

            using GuidelinesDialog dlg = new GuidelinesDialog(
                "RECOVERY REQUIREMENTS",
                "Verify your account and set a new password",
                rules);
            dlg.ShowDialog(this);
        }

        private async void btnResetPassword_Click(object? sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string email = txtEmail.Text.Trim();
            string code = txtVerificationCode.Text.Trim();
            string newPassword = txtNewPassword.Text;
            string confirmPassword = txtConfirmPassword.Text;

            bool hasError = false;

            string? e1 = ValidateUsername(username);
            lblErrUsername.Text = e1 ?? "";
            if (e1 != null) hasError = true;

            string? e2 = ValidateEmailRequired(email);
            lblErrEmail.Text = e2 ?? "";
            if (e2 != null) hasError = true;

            lblErrVerificationCode.ForeColor = Color.Crimson;
            string? eCode = ValidateVerificationCode(code);

            // UI-only guard; the server performs authoritative OTP validation.
            if (eCode == null &&
                (!string.Equals(username, _otpUsername, StringComparison.Ordinal) ||
                 !string.Equals(email, _otpEmail, StringComparison.OrdinalIgnoreCase)))
                eCode = "Username or email changed after the code was sent. Please request a new code.";

            if (eCode == null && (!_codeExpiresAt.HasValue || DateTime.UtcNow > _codeExpiresAt.Value))
                eCode = "Verification code has expired. Please request a new code.";

            lblErrVerificationCode.Text = eCode ?? "";
            if (eCode != null) hasError = true;

            string? e3 = ValidatePassword(newPassword);
            lblErrNewPassword.Text = e3 ?? "";
            if (e3 != null) hasError = true;

            string? e4 = ValidateConfirmPassword(newPassword, confirmPassword);
            lblErrConfirmPassword.Text = e4 ?? "";
            if (e4 != null) hasError = true;

            nightPanel1.Invalidate();
            if (hasError) return;

            btnResetPassword.Enabled = false;
            btnCancel.Enabled = false;
            try
            {
                ResponsePacket resp = await _netClient.ResetPasswordAsync(username, email, newPassword, code);
                if (IsDisposed) return;
                if (!resp.Success)
                {
                    PopupNoticeForm.ShowNotice(
                        this,
                        "Reset Password",
                        string.IsNullOrWhiteSpace(resp.Message) ? "Password reset failed." : resp.Message,
                        PopupNoticeForm.NoticeKind.Warning);
                    return;
                }

                // Mã dùng một lần
                _codeExpiresAt = null;

                PopupNoticeForm.ShowNotice(
                    this,
                    "Reset Password",
                    "Password updated successfully! You can now log in.",
                    PopupNoticeForm.NoticeKind.Info);

                QuayVeLoginForm();
            }
            finally
            {
                if (!IsDisposed)
                {
                    btnResetPassword.Enabled = true;
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
            _resendTimer.Stop();
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
