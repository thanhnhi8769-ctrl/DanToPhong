using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
        private readonly System.Windows.Forms.Timer _otpTimer = new();
        private bool isPasswordVisible;
        private bool isConfirmPasswordVisible;
        private bool isReturningToLogin;

        private string? selectedAvatarPath;
        private bool hasSentCode;
        private string? otpTargetEmail;
        private int otpCountdownSeconds;

        public RegisterForm(NetworkClientService netClient)
        {
            _netClient = netClient;
            InitializeComponent();
            picAvatar.Size = new Size(68, 68);   // vuông để avatar tròn không bị méo
            CauHinhNutDoExit();
            DoubleBuffered = true;
            AcceptButton = btnRegister;

            txtRacerId.Text = Random.Shared.Next(1, 999999).ToString("D6");
            cmbGender.SelectedIndex = 0;

            dtpBirthday.Format = DateTimePickerFormat.Custom;
            dtpBirthday.CustomFormat = "dd/MM/yyyy";
            dtpBirthday.MaxDate = DateTime.Today;
            dtpBirthday.MinDate = new DateTime(1920, 1, 1);
            dtpBirthday.Value = new DateTime(2004, 1, 1);

            _otpTimer.Interval = 1000;
            _otpTimer.Tick += OtpTimer_Tick;

            TextBox[] fields = new[]
            {
                txtUsername,
                txtPassword,
                txtConfirmPassword,
                txtFullName,
                txtEmail,
                txtVerificationCode
            };
            foreach (TextBox tb in fields)
            {
                tb.GotFocus += (_, _) => nightPanel1.Invalidate();
                tb.LostFocus += (_, _) => nightPanel1.Invalidate();
            }

            nightPanel1.Paint += VeVienDuoiCacO;
            CanGiuaBoCuc();
        }

        private static string DuongDanAvatarLocal(string username) => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DanToPhong",
            "Avatars",
            $"{username}.png");

        private void LuuAvatarLocal(string username)
        {
            if (picAvatar.Image == null) return;
            try
            {
                string path = DuongDanAvatarLocal(username);
                string? dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                using Bitmap cloned = new Bitmap(picAvatar.Image);
                cloned.Save(path, System.Drawing.Imaging.ImageFormat.Png);
            }
            catch
            {
            }
        }

        private async Task DayAvatarLenServerSauDangKyAsync(string username, string password)
        {
            if (string.IsNullOrEmpty(selectedAvatarPath)) return;
            try
            {
                byte[]? data = AvatarHelper.ToJpeg128(selectedAvatarPath);
                if (data == null) return;

                ResponsePacket login = await _netClient.LoginAsync(username, password);
                if (!login.Success) return;

                await _netClient.UploadAvatarAsync(data);
                await _netClient.LogoutAsync();
            }
            catch
            {
            }
        }

        private void picAvatar_Paint(object? sender, PaintEventArgs e)
        {
            if (picAvatar.Width <= 0 || picAvatar.Height <= 0) return;

            using GraphicsPath path = new GraphicsPath();
            path.AddEllipse(0, 0, picAvatar.Width - 1, picAvatar.Height - 1);

            Region? oldRegion = picAvatar.Region;
            picAvatar.Region = new Region(path);
            oldRegion?.Dispose();

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            if (picAvatar.Image == null)
            {
                using SolidBrush fillBrush = new SolidBrush(Color.FromArgb(240, 236, 246));
                e.Graphics.FillEllipse(fillBrush, 0, 0, picAvatar.Width - 1, picAvatar.Height - 1);

                using Font initialFont = new Font("Segoe UI", 15F, FontStyle.Bold);
                using SolidBrush textBrush = new SolidBrush(Color.FromArgb(43, 27, 61));
                string text = string.IsNullOrWhiteSpace(txtUsername.Text)
                    ? "SR"
                    : txtUsername.Text.Trim()[0].ToString().ToUpperInvariant();

                SizeF size = e.Graphics.MeasureString(text, initialFont);
                e.Graphics.DrawString(
                    text,
                    initialFont,
                    textBrush,
                    (picAvatar.Width - size.Width) / 2f,
                    (picAvatar.Height - size.Height) / 2f);
            }

            using Pen borderPen = new Pen(Color.FromArgb(43, 27, 61), 2f);
            e.Graphics.DrawEllipse(borderPen, 1, 1, picAvatar.Width - 3, picAvatar.Height - 3);
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
            btnExit.BringToFront();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            CauHinhNutDoExit();
            Rectangle workArea = Screen.FromControl(this).WorkingArea;
            int width = Math.Min(ClientSize.Width, Math.Max(800, workArea.Width - 16));
            int height = Math.Min(ClientSize.Height, Math.Max(440, workArea.Height - 16));
            if (width != ClientSize.Width || height != ClientSize.Height)
                ClientSize = new Size(width, height);
            CanGiuaBoCuc();
        }

        private void btnChooseAvatar_Click(object? sender, EventArgs e)
        {
            using OpenFileDialog ofd = new OpenFileDialog
            {
                Title = "Select Avatar Image",
                Filter = "Image Files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp"
            };

            if (ofd.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                using FileStream fs = new FileStream(ofd.FileName, FileMode.Open, FileAccess.Read);
                using Image temp = Image.FromStream(fs);
                Image? oldImg = picAvatar.Image;
                picAvatar.Image = new Bitmap(temp);
                oldImg?.Dispose();

                selectedAvatarPath = ofd.FileName;
                picAvatar.Invalidate();
            }
            catch
            {
                PopupNoticeForm.ShowNotice(
                    this,
                    "Avatar",
                    "Cannot load this image file.",
                    PopupNoticeForm.NoticeKind.Warning);
            }
        }

        private void VeVienDuoiCacO(object? sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            VeDuongVien(e.Graphics, txtRacerId, null, false);
            VeDuongVien(e.Graphics, txtUsername, null, lblErrUsername.Text.Length > 0);
            VeDuongVien(e.Graphics, txtPassword, btnTogglePassword, lblErrPassword.Text.Length > 0);
            VeDuongVien(e.Graphics, txtConfirmPassword, btnToggleConfirmPassword, lblErrConfirmPassword.Text.Length > 0);
            VeDuongVien(e.Graphics, txtFullName, null, lblErrFullName.Text.Length > 0);
            VeDuongVien(e.Graphics, txtEmail, null, lblErrEmail.Text.Length > 0);
            VeDuongVien(e.Graphics, txtVerificationCode, null, lblCodeStatus.ForeColor == Color.Crimson && lblCodeStatus.Text.Length > 0);
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

            const int designPanelWidth = 926;
            const int designPanelHeight = 659;
            nightPanel1.Width = Math.Min(designPanelWidth, Math.Max(560, ClientSize.Width - 32));
            nightPanel1.Height = Math.Min(designPanelHeight, Math.Max(420, usableHeight - 12));
            nightPanel1.AutoScroll = nightPanel1.Width < designPanelWidth || nightPanel1.Height < designPanelHeight;
            nightPanel1.AutoScrollMinSize = new Size(designPanelWidth, designPanelHeight);

            int x = Math.Max(16, (ClientSize.Width - nightPanel1.Width) / 2);
            int y = topOffset + Math.Max(4, (usableHeight - nightPanel1.Height) / 2);

            nightPanel1.Location = new Point(x, y);
            panelShadow.Size = nightPanel1.Size;
            panelShadow.Location = new Point(x + 6, y + 6);

            panelShadow.SendToBack();
            nightPanel1.BringToFront();

            int left = 48;
            int colGap = 44;
            int colWidth = (nightPanel1.Width - left * 2 - colGap) / 2;
            int rightColLeft = left + colWidth + colGap;

            lblTitle.Location = new Point((nightPanel1.Width - lblTitle.Width) / 2, 10);
            lblSubtitle.Location = new Point((nightPanel1.Width - lblSubtitle.Width) / 2, lblTitle.Bottom + 1);
            btnHelp.Location = new Point(nightPanel1.Width - btnHelp.Width - 16, 12);

            picAvatar.Location = new Point((nightPanel1.Width - picAvatar.Width) / 2, lblSubtitle.Bottom + 8);
            btnChooseAvatar.Location = new Point((nightPanel1.Width - btnChooseAvatar.Width) / 2, picAvatar.Bottom + 6);

            int row1Y = btnChooseAvatar.Bottom + 14;
            lblRacerId.Location = new Point(left, row1Y);
            txtRacerId.Location = new Point(left, lblRacerId.Bottom + 3);
            txtRacerId.Width = colWidth;

            lblUsername.Location = new Point(rightColLeft, row1Y);
            txtUsername.Location = new Point(rightColLeft, lblUsername.Bottom + 3);
            txtUsername.Width = colWidth;
            lblErrUsername.Location = new Point(rightColLeft, txtUsername.Bottom + 5);

            int row2Y = txtRacerId.Bottom + 24;
            lblPassword.Location = new Point(left, row2Y);
            txtPassword.Location = new Point(left, lblPassword.Bottom + 3);
            txtPassword.Width = colWidth - btnTogglePassword.Width - 4;
            btnTogglePassword.Location = new Point(left + colWidth - btnTogglePassword.Width, txtPassword.Top - 1);
            lblErrPassword.Location = new Point(left, txtPassword.Bottom + 5);

            lblFullName.Location = new Point(rightColLeft, row2Y);
            txtFullName.Location = new Point(rightColLeft, lblFullName.Bottom + 3);
            txtFullName.Width = colWidth;
            lblErrFullName.Location = new Point(rightColLeft, txtFullName.Bottom + 5);

            int row3Y = txtPassword.Bottom + 24;
            lblConfirmPassword.Location = new Point(left, row3Y);
            txtConfirmPassword.Location = new Point(left, lblConfirmPassword.Bottom + 3);
            txtConfirmPassword.Width = colWidth - btnToggleConfirmPassword.Width - 4;
            btnToggleConfirmPassword.Location = new Point(left + colWidth - btnToggleConfirmPassword.Width, txtConfirmPassword.Top - 1);
            lblErrConfirmPassword.Location = new Point(left, txtConfirmPassword.Bottom + 5);

            lblEmail.Location = new Point(rightColLeft, row3Y);
            txtEmail.Location = new Point(rightColLeft, lblEmail.Bottom + 3);
            txtEmail.Width = colWidth;
            lblErrEmail.Location = new Point(rightColLeft, txtEmail.Bottom + 5);

            int row4Y = txtConfirmPassword.Bottom + 24;
            int genderWidth = 132;
            int bdayGap = 12;
            int bdayWidth = colWidth - genderWidth - bdayGap;

            lblBirthday.Location = new Point(left, row4Y);
            dtpBirthday.Location = new Point(left, lblBirthday.Bottom + 3);
            dtpBirthday.Width = bdayWidth;

            lblGender.Location = new Point(left + bdayWidth + bdayGap, row4Y);
            cmbGender.Location = new Point(left + bdayWidth + bdayGap, lblGender.Bottom + 3);
            cmbGender.Width = genderWidth;

            int codeBtnWidth = 106;
            int codeGap = 10;
            lblVerificationCode.Location = new Point(rightColLeft, row4Y);
            txtVerificationCode.Location = new Point(rightColLeft, lblVerificationCode.Bottom + 5);
            txtVerificationCode.Width = colWidth - codeBtnWidth - codeGap;
            btnSendCode.Location = new Point(rightColLeft + colWidth - codeBtnWidth, lblVerificationCode.Bottom + 1);
            btnSendCode.Width = codeBtnWidth;
            lblCodeStatus.Location = new Point(rightColLeft, txtVerificationCode.Bottom + 5);

            int fullWidth = nightPanel1.Width - left * 2;
            int contentHeight = nightPanel1.AutoScroll ? Math.Max(nightPanel1.Height, nightPanel1.AutoScrollMinSize.Height) : nightPanel1.Height;
            int btnY = contentHeight - btnRegister.Height - 24;
            int cancelWidth = 132;
            int btnGap = 12;
            btnRegister.Location = new Point(left, btnY);
            btnRegister.Width = fullWidth - cancelWidth - btnGap;
            btnCancel.Location = new Point(btnRegister.Right + btnGap, btnY);
            btnCancel.Width = cancelWidth;

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

            Region? oldRegion = ctrl.Region;
            ctrl.Region = new Region(path);
            oldRegion?.Dispose();
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

        private static string? ValidateEmailRequired(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return "Please enter your email address.";
            if (!EmailRegex.IsMatch(email)) return "Invalid email address.";
            return null;
        }

        private void txtUsername_TextChanged(object? sender, EventArgs e)
        {
            string u = txtUsername.Text.Trim();
            lblErrUsername.Text = string.IsNullOrEmpty(u) ? "" : (ValidateUsername(u) ?? "");
            picAvatar.Invalidate();
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
            lblErrEmail.Text = string.IsNullOrEmpty(em) ? "" : (ValidateEmailRequired(em) ?? "");
            if (!string.Equals(em, otpTargetEmail, StringComparison.OrdinalIgnoreCase))
            {
                hasSentCode = false;
                lblCodeStatus.Text = "";
            }
            nightPanel1.Invalidate();
        }

        private void txtVerificationCode_TextChanged(object? sender, EventArgs e)
        {
            if (lblCodeStatus.ForeColor == Color.Crimson)
            {
                lblCodeStatus.Text = "";
                nightPanel1.Invalidate();
            }
        }

        private async void btnSendCode_Click(object? sender, EventArgs e)
        {
            string email = txtEmail.Text.Trim();
            string? emailErr = ValidateEmailRequired(email);
            if (emailErr != null)
            {
                lblErrEmail.ForeColor = Color.FromArgb(170, 25, 25);
                lblErrEmail.Text = emailErr;
                nightPanel1.Invalidate();
                txtEmail.Focus();
                return;
            }

            lblErrEmail.Text = "";
            btnSendCode.Enabled = false;
            btnSendCode.ForeColor = Color.White;

            try
            {
                string username = txtUsername.Text.Trim();
                string? usernameErr = ValidateUsername(username);
                if (usernameErr != null) throw new InvalidOperationException(usernameErr);
                ResponsePacket response = await _netClient.RequestRegistrationCodeAsync(username, email);
                if (!response.Success) throw new InvalidOperationException(response.Message);

                hasSentCode = true;
                otpTargetEmail = email;

                lblCodeStatus.ForeColor = Color.FromArgb(15, 115, 50);
                lblCodeStatus.Text = response.Message;

                otpCountdownSeconds = 600;
                btnSendCode.Text = $"{otpCountdownSeconds}s";
                _otpTimer.Start();
            }
            catch (Exception ex)
            {
                hasSentCode = false;
                lblCodeStatus.ForeColor = Color.FromArgb(170, 25, 25);
                lblCodeStatus.Text = "Could not send the verification email. Please try again later.";
                btnSendCode.Enabled = true;
                btnSendCode.ForeColor = Color.White;
                btnSendCode.Text = "Get Code";

                PopupNoticeForm.ShowNotice(
                    this,
                    "Mail Error",
                    $"Không gửi được email xác nhận:\n{ex.Message}",
                    PopupNoticeForm.NoticeKind.Error);
            }

            nightPanel1.Invalidate();
        }

        private void OtpTimer_Tick(object? sender, EventArgs e)
        {
            otpCountdownSeconds--;
            btnSendCode.ForeColor = Color.White;

            if (otpCountdownSeconds <= 0)
            {
                _otpTimer.Stop();

                // Mã hết hạn sau 60 giây đúng như nội dung email
                hasSentCode = false;
                lblCodeStatus.ForeColor = Color.Crimson;
                lblCodeStatus.Text = "Code expired. Please request a new one.";

                btnSendCode.Enabled = true;
                btnSendCode.Text = "Get Code";
                nightPanel1.Invalidate();
            }
            else
            {
                btnSendCode.Text = $"{otpCountdownSeconds}s";
            }
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
                "• Avatar (Optional): Click the avatar circle or 'Choose Avatar' to upload a PNG, JPG, JPEG, or BMP image. If skipped, your username initial is shown.\n" +
                "• Racer ID: Auto-generated 6-digit racer identifier (read-only).\n" +
                "• Username (Required): 3–20 characters using letters (a–z, A–Z), numbers (0–9), or underscore (_) only.\n" +
                "• Password (Required): Minimum 8 characters and must include at least 1 letter and 1 number.\n" +
                "• Confirm Password (Required): Must match the Password field exactly.\n" +
                "• Full Name (Required): 2–100 characters.\n" +
                "• Email (Required): Must be a valid email format (e.g., name@domain.com).\n" +
                "• Verification Code (Required): Click 'Get Code' to send a 6-digit code to your email. The code expires in 10 minutes. If you edit the email afterward, request a new code.\n" +
                "• Birthday & Gender (Required): Select a valid birth date (01/01/1920 – today, dd/MM/yyyy) and choose Male, Female, or Other.\n" +
                "• New Account Bonus: Every new account starts with 100 coins.\n" +
                "• Submit: Click 'Register' (or press Enter) once all fields are valid to create your account and return to Login.";

            using GuidelinesDialog dlg = new GuidelinesDialog(
                "REGISTER REQUIREMENTS",
                "Rules & instructions for creating a new racer account",
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
            string code = txtVerificationCode.Text.Trim();
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

            string? e5 = ValidateEmailRequired(email);
            lblErrEmail.Text = e5 ?? "";
            if (e5 != null) hasError = true;

            if (!hasSentCode || !string.Equals(email, otpTargetEmail, StringComparison.OrdinalIgnoreCase))
            {
                lblCodeStatus.ForeColor = Color.Crimson;
                lblCodeStatus.Text = "Please click 'Get Code' to verify your email.";
                hasError = true;
            }
            else if (code.Length != 6 || !code.All(char.IsDigit))
            {
                lblCodeStatus.ForeColor = Color.Crimson;
                lblCodeStatus.Text = "Please enter the 6-digit verification code.";
                hasError = true;
            }

            if (birthday > DateTime.Today || birthday < dtpBirthday.MinDate)
            {
                PopupNoticeForm.ShowNotice(
                    this,
                    "Registration",
                    "Invalid birthday.",
                    PopupNoticeForm.NoticeKind.Warning);
                hasError = true;
            }

            nightPanel1.Invalidate();
            if (hasError) return;

            btnRegister.Enabled = false;
            btnCancel.Enabled = false;
            try
            {
                ResponsePacket resp = await _netClient.RegisterAsync(
                    username, password, fullName, email, birthday,
                    cmbGender.SelectedItem?.ToString(),
                    txtRacerId.Text.Trim(),
                    code);

                if (!resp.Success)
                {
                    PopupNoticeForm.ShowNotice(
                        this,
                        "Registration",
                        string.IsNullOrWhiteSpace(resp.Message) ? "Registration failed." : resp.Message,
                        PopupNoticeForm.NoticeKind.Warning);
                    return;
                }

                // ID thật do server cấp (có thể khác số gợi ý nếu số đó đã có người dùng)
                string finalId = string.IsNullOrWhiteSpace(resp.User?.RacerId)
                    ? txtRacerId.Text.Trim()
                    : resp.User!.RacerId!;
                if (finalId != txtRacerId.Text.Trim()) txtRacerId.Text = finalId;
                int startCoins = resp.User?.Coins > 0 ? resp.User.Coins : 100;

                LuuAvatarLocal(username);
                await DayAvatarLenServerSauDangKyAsync(username, password);

                PopupNoticeForm.ShowNotice(
                    this,
                    "Registration",
                    $"Registration successful!\nYour Racer ID: {finalId}\nYou received {startCoins} coins. You can now log in.",
                    PopupNoticeForm.NoticeKind.Info);

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
            _otpTimer.Stop();
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
