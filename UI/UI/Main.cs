using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using DanToPhong.Protocol;
using UI.Services;
using System.Linq;

namespace UI
{
    public partial class Main : Form
    {
        private PictureBox? _picSettingAvatar;
        private string? _pendingAvatarPath;
        private Panel? _roomsPanel;
        private string _roomsSig = "";
        private int _roomsBuiltWidth;

        private string _racerId = "";
        private Label? _lblRacerIdCaption, _lblRacerIdValue;

        private string DuongDanAvatarLocal => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DanToPhong",
            "Avatars",
            $"{_username}.png");

        public void ApDungVaLuuAvatar(string sourceImagePath)
        {
            using var fs = new FileStream(sourceImagePath, FileMode.Open, FileAccess.Read);
            using var temp = Image.FromStream(fs);
            using Bitmap cloned = new Bitmap(temp);

            string? dir = Path.GetDirectoryName(DuongDanAvatarLocal);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            cloned.Save(DuongDanAvatarLocal, System.Drawing.Imaging.ImageFormat.Png);
            SetAvatar(cloned);
        }
        private async Task DayAvatarLenServerAsync(string imagePath)
        {
            if (_netClient == null) return;
            try
            {
                byte[]? data = AvatarHelper.ToJpeg128(imagePath);
                if (data == null) { SetSettingsStatus("Avatar quá nặng, hãy chọn ảnh khác.", isError: true); return; }

                ResponsePacket r = await _netClient.UploadAvatarAsync(data);
                if (!r.Success)
                    SetSettingsStatus("Không lưu được avatar lên server: " + r.Message, isError: true);
            }
            catch { }
        }

        // Gọi khi vào Main: lấy avatar từ DB (đồng bộ mọi máy). Nếu DB chưa có mà máy có file local thì đẩy lên.
        private async Task DongBoAvatarTuServerAsync()
        {
            if (_netClient == null) return;
            try
            {
                ResponsePacket r = await _netClient.GetAvatarAsync();
                if (!r.Success || IsDisposed) return;

                if (!string.IsNullOrEmpty(r.AvatarBase64))
                {
                    byte[] bytes = Convert.FromBase64String(r.AvatarBase64);
                    using var ms = new MemoryStream(bytes);
                    using var temp = Image.FromStream(ms);
                    using var bmp = new Bitmap(temp);

                    string? dir = Path.GetDirectoryName(DuongDanAvatarLocal);
                    if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                    bmp.Save(DuongDanAvatarLocal, System.Drawing.Imaging.ImageFormat.Png);

                    SetAvatar(bmp);
                }
                else if (File.Exists(DuongDanAvatarLocal))
                {
                    await DayAvatarLenServerAsync(DuongDanAvatarLocal);
                }
            }
            catch { }
        }

        private async Task TaiDanhGiaTuServerAsync()
        {
            if (_netClient == null) return;
            try
            {
                ResponsePacket r = await _netClient.GetReviewsAsync();
                if (IsDisposed || !r.Success || r.ReviewSummary == null) return;

                SetReviewSummary(r.ReviewSummary.Counts);

                if (r.ReviewSummary.HasReviewed)
                {
                    _submittedReviewers.Add(string.IsNullOrWhiteSpace(_username) ? "guest" : _username.Trim());
                    btnSubmitReview.Enabled = false;
                    btnSubmitReview.Text = "Reviewed";
                    ChonRating(r.ReviewSummary.MyStars);
                }
            }
            catch { }
        }

        private void TaiAvatarLocalNeuCo()
        {
            try
            {
                if (!File.Exists(DuongDanAvatarLocal)) return;
                using var fs = new FileStream(DuongDanAvatarLocal, FileMode.Open, FileAccess.Read);
                using var temp = Image.FromStream(fs);
                SetAvatar(new Bitmap(temp));
            }
            catch
            {
            }
        }

        private void KhoiTaoDieuKhienDoiAvatarSettings()
        {
            if (cardSettings == null || _picSettingAvatar != null) return;

            _picSettingAvatar = new PictureBox
            {
                Name = "picSettingAvatar",
                Size = new Size(76, 76),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(240, 236, 246),
                Cursor = Cursors.Hand
            };
            _picSettingAvatar.Paint += (_, e) =>
            {
                if (_picSettingAvatar.Width <= 0 || _picSettingAvatar.Height <= 0) return;

                using GraphicsPath path = new GraphicsPath();
                path.AddEllipse(0, 0, _picSettingAvatar.Width - 1, _picSettingAvatar.Height - 1);
                Region? oldRegion = _picSettingAvatar.Region;
                _picSettingAvatar.Region = new Region(path);
                oldRegion?.Dispose();

                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Image? img = _picSettingAvatar.Image ?? _customAvatar;

                if (img != null)
                {
                    e.Graphics.DrawImage(img, 0, 0, _picSettingAvatar.Width, _picSettingAvatar.Height);
                }
                else
                {
                    using SolidBrush bg = new SolidBrush(Color.FromArgb(92, 45, 145));
                    e.Graphics.FillEllipse(bg, 0, 0, _picSettingAvatar.Width - 1, _picSettingAvatar.Height - 1);

                    string initial = string.IsNullOrWhiteSpace(_username)
                        ? "P"
                        : _username.Trim()[0].ToString().ToUpperInvariant();
                    using Font f = new Font("Segoe UI", 24F, FontStyle.Bold);
                    using SolidBrush tb = new SolidBrush(Color.White);
                    SizeF sz = e.Graphics.MeasureString(initial, f);
                    e.Graphics.DrawString(
                        initial,
                        f,
                        tb,
                        (_picSettingAvatar.Width - sz.Width) / 2f,
                        (_picSettingAvatar.Height - sz.Height) / 2f);
                }

                using Pen ring = new Pen(Color.FromArgb(43, 27, 61), 2.2f);
                e.Graphics.DrawEllipse(ring, 1, 1, _picSettingAvatar.Width - 3, _picSettingAvatar.Height - 3);
            };
            _picSettingAvatar.Click += BtnChooseAvatarSettings_Click;

            cardSettings.Controls.Add(_picSettingAvatar);
        }
        private void BtnChooseAvatarSettings_Click(object? sender, EventArgs e)
        {
            using OpenFileDialog ofd = new OpenFileDialog
            {
                Title = "Select Avatar Image",
                Filter = "Image Files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp"
            };

            if (ofd.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                using var fs = new FileStream(ofd.FileName, FileMode.Open, FileAccess.Read);
                using var temp = Image.FromStream(fs);
                Image? oldImg = _picSettingAvatar?.Image;
                if (_picSettingAvatar != null)
                {
                    _picSettingAvatar.Image = new Bitmap(temp);
                    _picSettingAvatar.Invalidate();
                }
                oldImg?.Dispose();

                _pendingAvatarPath = ofd.FileName;
                if (!string.IsNullOrEmpty(lblSettingsStatus.Text))
                    lblSettingsStatus.Text = string.Empty;
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

        private void CanChinhViTriAvatarSettings(int cardWidth, int avatarTopY)
        {
            if (_picSettingAvatar == null) return;

            int avatarSize = 76;
            _picSettingAvatar.Size = new Size(avatarSize, avatarSize);
            _picSettingAvatar.Location = new Point((cardWidth - avatarSize) / 2, avatarTopY);

            _picSettingAvatar.BringToFront();
            _picSettingAvatar.Invalidate();
        }
        public sealed class RoomItem
        {
            public string RoomCode { get; init; } = "01";
            public string HostName { get; init; } = "";
            public int CurrentPlayers { get; init; }
            public int MaxPlayers { get; init; } = 4;
            public bool IsRacing { get; init; }
            public string Title => string.IsNullOrEmpty(HostName) ? $"Room {RoomCode}" : $"Room {RoomCode} · {HostName}";
            public string StatusText => $"{CurrentPlayers}/{MaxPlayers} · {(IsRacing ? "Racing" : "Waiting")}";
        }
        private readonly System.Windows.Forms.Timer _pollTimer = new() { Interval = 3000 };
        private bool _polling;

        private async Task PollServerAsync()
        {
            if (_polling || _netClient == null || _isClosing || IsDisposed || !Visible) return;
            _polling = true;
            try
            {
                if (_activeTab == MainTab.Rooms)
                {
                    ResponsePacket r = await _netClient.GetRoomsAsync();
                    if (!IsDisposed && r.Success && r.Rooms != null)
                    {
                        SetRooms(r.Rooms.Select(x => new RoomItem
                        {
                            RoomCode = x.Code,
                            HostName = x.HostName,
                            CurrentPlayers = x.CurrentPlayers,
                            MaxPlayers = x.MaxPlayers,
                            IsRacing = x.IsRacing
                        }));
                    }
                }

                ResponsePacket inv = await _netClient.GetInvitesAsync();
                if (!IsDisposed && inv.Success && inv.Invites != null)
                {
                    foreach (InviteDto i in inv.Invites)
                    {
                        if (!Visible) break;
                        DialogResult ans = PopupNoticeForm.ShowConfirm(
                            this,
                            "Room invite",
                            $"{i.FromName} invited you to room {i.RoomCode}.\n\nJoin now?",
                            PopupNoticeForm.NoticeKind.Info);
                        if (ans == DialogResult.Yes) { KhoiChayGame("room", i.RoomCode); break; }
                    }
                }
            }
            catch { }
            finally { _polling = false; }
        }
        private async Task DongBoSauGameAsync()
        {
            if (_netClient == null) return;
            try
            {
                ResponsePacket resp = await _netClient.GetInfoAsync();
                if (!IsDisposed && resp.Success && resp.User != null)
                {
                    UserInfoDto u = resp.User;
                    UpdatePlayerStats(u.Username, u.FullName, u.Coins,
                        u.Email, u.Birthday, u.NgayTao, u.LanDangNhapCuoi, u.RacerId);
                }
            }
            catch { }
        }

        private void KhoiChayGame(string mode, string roomId = "")
        {
            if (_netClient == null || string.IsNullOrEmpty(_netClient.Token))
            {
                PopupNoticeForm.ShowNotice(this, "Game",
                    "Chưa có phiên đăng nhập với server. Hãy đăng nhập lại.",
                    PopupNoticeForm.NoticeKind.Warning);
                return;
            }

            bool started = _gameLauncher.LaunchGame(
                username: _username,
                token: _netClient.Token,
                host: _netClient.Host,
                port: _netClient.Port,
                useTls: _netClient.UseTls,
                mode: mode,
                roomId: roomId,
                onGameExited: () =>
                {
                    if (!IsHandleCreated || IsDisposed) return;
                    BeginInvoke(new Action(async () =>
                    {
                        await DongBoSauGameAsync();
                        SetLauncherButtonsEnabled(true);
                        Show();
                        Activate();
                    }));
                });

            if (started)
            {
                SetLauncherButtonsEnabled(false);
                Hide();
            }
        }
        private readonly GameLauncherService _gameLauncher = new GameLauncherService();




        private void SetLauncherButtonsEnabled(bool enabled)
        {
            if (btnSinglePlayer != null) btnSinglePlayer.Enabled = enabled;
            if (btnMultiplayer != null) btnMultiplayer.Enabled = enabled;
            if (btnJoinRoom1 != null) btnJoinRoom1.Enabled = enabled;
            if (btnJoinRoom3 != null) btnJoinRoom3.Enabled = enabled;
            if (btnCreateRoom != null) btnCreateRoom.Enabled = enabled;
        }

        private void btnSinglePlayer_Click(object? sender, EventArgs e)
        {
            KhoiChayGame("single");
        }

        private void btnMultiplayer_Click(object? sender, EventArgs e)
        {
            KhoiChayGame("multiplayer");
        }

        private void JoinRoomIndex(int i)
        {
            if (i < 0 || i >= _rooms.Count) return;
            RoomItem r = _rooms[i];
            if (r.IsRacing || r.CurrentPlayers >= r.MaxPlayers) return;
            KhoiChayGame("room", r.RoomCode);
        }
        private void btnJoinRoom1_Click(object? sender, EventArgs e) => JoinRoomIndex(0);
        private void btnSpectateRoom2_Click(object? sender, EventArgs e) => JoinRoomIndex(1);
        private void btnJoinRoom3_Click(object? sender, EventArgs e) => JoinRoomIndex(2);


        public sealed class GameReviewItem
        {
            public string Username { get; init; } = "";
            public int Stars { get; init; } = 5;
            public string Comment { get; init; } = "";
            public DateTime CreatedAt { get; init; } = DateTime.Now;
        }

        public sealed class SettingsSaveEventArgs : EventArgs
        {
            public string CurrentPassword { get; init; } = string.Empty;
            public string? NewPassword { get; init; }
            public string? NewEmail { get; init; }
            public string? AvatarFilePath { get; init; }
        }

        private enum MainTab
        {
            Menu,
            Rooms,
            Racer,
            Settings,
            Review
        }

        private const int HeaderAvatarSize = 34;
        private const int RacerAvatarSize = 76;

        public event EventHandler? LogoutRequested;
        public event EventHandler? ExitRequested;
        public event EventHandler<GameReviewItem>? ReviewSubmitted;
        public event EventHandler<SettingsSaveEventArgs>? SettingsSaveRequested;

        private readonly NetworkClientService? _netClient;
        private readonly string _username;
        private string _fullName;
        private string _email;
        private DateTime? _birthday;
        private DateTime? _createdAt;
        private DateTime? _lastLogin;
        private int _coins;
        private Image? _customAvatar;
        private Image? _backgroundImage;
        private Form? _loginForm;
        private bool _isClosing = false;
        private bool _isLoggingOut = false;
        private MainTab _activeTab = MainTab.Menu;
        private readonly List<RoomItem> _rooms = new();
        private readonly int[] _starCounts = { 0, 0, 0, 0, 0 };
        private int _selectedRating = 4;

        private Rectangle _settingsAuthBox;
        private Rectangle _settingsUpdateBox;

        public Main(NetworkClientService netClient, UserInfoDto user)
            : this(
                user.Username,
                string.IsNullOrWhiteSpace(user.FullName) ? user.Username : user.FullName,
                user.Email,
                user.Birthday,
                user.NgayTao,
                user.LanDangNhapCuoi,
                user.Coins,
                netClient)
        {
        }

        public Main(string username)
            : this(username, username, "", null, DateTime.Now, DateTime.Now, 100, null)
        {
        }

        public Main(
            string username,
            string fullName,
            string? email = null,
            DateTime? birthday = null,
            DateTime? createdAt = null,
            DateTime? lastLogin = null,
            int coins = 100,
            NetworkClientService? netClient = null)
        {
            _netClient = netClient;
            _username = username;
            _fullName = fullName;
            _email = string.IsNullOrWhiteSpace(email) ? "Not set" : email;
            _birthday = birthday;
            _createdAt = createdAt;
            _lastLogin = lastLogin;
            _coins = coins;

            InitializeComponent();
            txtSettingsEmail.MaxLength = AccountInputValidation.MaxEmailLength;
            txtSettingsNewPass.MaxLength = AccountInputValidation.MaxPasswordLength;
            txtSettingsConfirmPass.MaxLength = AccountInputValidation.MaxPasswordLength;

            foreach (Button s in new[] { btnPickStar1, btnPickStar2, btnPickStar3, btnPickStar4, btnPickStar5 })
            {
                s.Text = "★";
                s.Font = new Font("Segoe UI", 15F, FontStyle.Bold);
                s.BackColor = Color.FromArgb(253, 251, 247);
                s.FlatStyle = FlatStyle.Flat;
                s.Cursor = Cursors.Hand;
            }

            DoubleBuffered = true;

            _backgroundImage = new Bitmap(Properties.Resources.bg);
            typeof(Panel).GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(pnlContentHost, true);

            KhoiTaoSidebarVaSettings();
            KhoiTaoDanhSachPhong();
            KhoiTaoRacerId();

            btnExit.BackColor = Color.FromArgb(178, 22, 30);
            btnExit.ForeColor = Color.White;
            btnExit.FlatStyle = FlatStyle.Flat;
            btnExit.FlatAppearance.BorderSize = 0;
            btnExit.FlatAppearance.MouseOverBackColor = Color.FromArgb(204, 32, 40);
            btnExit.FlatAppearance.MouseDownBackColor = Color.FromArgb(130, 14, 20);

            lblConnectionStatus.Visible = false;
            lblConnectionStatus.Text = "";

            picHeaderAvatar.Paint += VeAvatarTron;
            picRacerAvatar.Paint += VeAvatarTron;

            pnlContentHost.Paint += VeNenContentHost;
            cardRacer.Paint += VeChiTietCardRacer;
            cardRooms.Paint += VeChiTietCardRooms;
            cardSettings.Paint += VeChiTietCardSettings;
            cardReview.Paint += VeChiTietCardReview;
            Shown += Main_Shown;
            FormClosing += Main_FormClosing;

            GanSuKienDanhGia();

            if (_netClient != null)
            {
                _netClient.ConnectionStatusChanged += OnConnectionStatusChanged;
            }

            NapDuLieuMau();
            ApDungThongTinNguoiChoi();
            TaiAvatarLocalNeuCo();
            CapNhatThongKeDanhGia();
            ChonRating(_selectedRating);
            ChonTab(MainTab.Menu);
            ThuGonVaTachBoCuc();
            _pollTimer.Tick += async (_, _) => await PollServerAsync();
            _pollTimer.Start();
        }

        public void SetBackgroundImage(Image? bgImage)
        {
            _backgroundImage?.Dispose();
            _backgroundImage = bgImage != null ? new Bitmap(bgImage) : null;
            pnlContentHost.Invalidate();
        }

        private void VeNenContentHost(object? sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            if (_backgroundImage != null)
            {
                g.DrawImage(_backgroundImage, pnlContentHost.ClientRectangle);
                return;
            }

            using Pen gridPen = new Pen(Color.FromArgb(45, 43, 27, 61), 1f);
            for (int gx = 0; gx < pnlContentHost.Width; gx += 40)
                g.DrawLine(gridPen, gx, 0, gx, pnlContentHost.Height);
            for (int gy = 0; gy < pnlContentHost.Height; gy += 40)
                g.DrawLine(gridPen, 0, gy, pnlContentHost.Width, gy);
        }
        private void KhoiTaoDanhSachPhong()
        {
            if (_roomsPanel != null) return;

            // Ẩn vĩnh viễn 3 dòng cố định cũ
            foreach (Control c in new Control[]
            {
                lblRoom1Title, lblRoom1Info, btnJoinRoom1,
                lblRoom2Title, lblRoom2Info, btnSpectateRoom2,
                lblRoom3Title, lblRoom3Info, btnJoinRoom3
            })
                c.Visible = false;

            _roomsPanel = new Panel
            {
                AutoScroll = true,
                BackColor = cardRooms.BackColor
            };
            _roomsPanel.MouseEnter += (_, _) => _roomsPanel.Focus();   // để lăn chuột được
            cardRooms.Controls.Add(_roomsPanel);
        }

        // Dòng "Racer ID" trong tab Racer (bấm vào để sao chép, gửi cho bạn bè mời)
        private void KhoiTaoRacerId()
        {
            if (_lblRacerIdCaption != null) return;

            _lblRacerIdCaption = new Label
            {
                Text = "Racer ID",
                AutoSize = false,
                Font = lblEmailCaption.Font,
                ForeColor = lblEmailCaption.ForeColor,
                BackColor = lblEmailCaption.BackColor,
                TextAlign = lblEmailCaption.TextAlign
            };
            _lblRacerIdValue = new Label
            {
                Text = "--",
                AutoSize = false,
                Font = lblEmailValue.Font,
                ForeColor = lblEmailValue.ForeColor,
                BackColor = lblEmailValue.BackColor,
                TextAlign = lblEmailValue.TextAlign,
                Cursor = Cursors.Hand
            };

            _lblRacerIdValue.Click += async (_, _) =>
            {
                if (string.IsNullOrEmpty(_racerId)) return;
                try { Clipboard.SetText(_racerId); } catch { return; }
                _lblRacerIdValue!.Text = _racerId + "  (copied)";
                await Task.Delay(1200);
                if (!IsDisposed && _lblRacerIdValue != null) _lblRacerIdValue.Text = _racerId;
            };

            cardRacer.Controls.Add(_lblRacerIdCaption);
            cardRacer.Controls.Add(_lblRacerIdValue);
        }

        private void KhoiTaoSidebarVaSettings()
        {
            lblCoinsValue.Font = new Font("Segoe UI", 11F, FontStyle.Bold);

            btnMinimize.FlatAppearance.BorderSize = 1;
            btnMaximize.FlatAppearance.BorderSize = 1;
            btnExit.FlatAppearance.BorderSize = 1;

            btnLogout.FlatAppearance.BorderColor = Color.FromArgb(245, 208, 120);
            btnLogout.FlatAppearance.BorderSize = 1;

            btnSinglePlayer.FlatAppearance.BorderSize = 0;
            btnMultiplayer.FlatAppearance.BorderSize = 0;
            btnLeaderboard.FlatAppearance.BorderSize = 0;

            btnJoinRoom1.FlatAppearance.BorderSize = 0;
            btnSpectateRoom2.FlatAppearance.BorderSize = 0;
            btnJoinRoom3.FlatAppearance.BorderSize = 0;
            btnCreateRoom.FlatAppearance.BorderSize = 0;

            btnHelpMenu.FlatAppearance.BorderColor = Color.FromArgb(43, 27, 61);
            btnHelpRooms.FlatAppearance.BorderColor = Color.FromArgb(43, 27, 61);
            btnHelpRacer.FlatAppearance.BorderColor = Color.FromArgb(43, 27, 61);
            btnHelpSettings.FlatAppearance.BorderColor = Color.FromArgb(43, 27, 61);
            btnHelpReview.FlatAppearance.BorderColor = Color.FromArgb(43, 27, 61);

            foreach (Button s in new[] { btnPickStar1, btnPickStar2, btnPickStar3, btnPickStar4, btnPickStar5 })
            {
                s.FlatAppearance.BorderSize = 0;
            }
            btnSubmitReview.FlatAppearance.BorderSize = 0;

            btnTabSettings.Click += (_, _) => ChonTab(MainTab.Settings);

            foreach (Button sideBtn in new[] { btnTabMenu, btnTabRooms, btnTabRacer, btnTabSettings, btnTabReview, btnLogout })
            {
                sideBtn.TextAlign = ContentAlignment.MiddleLeft;
                sideBtn.Padding = new Padding(44, 0, 12, 0);
                sideBtn.Paint -= VeIconTrenNutSidebar;
                sideBtn.Paint += VeIconTrenNutSidebar;
            }

            lblSettingsConfirmPass.Text = "Confirm Password";
            txtSettingsNewPass.PlaceholderText = "Min 8 chars, A-z & 0-9";
            txtSettingsConfirmPass.PlaceholderText = "Re-enter password";

            CauHinhNutMatSettings(btnToggleSettingsOldPass, txtSettingsOldPass);
            CauHinhNutMatSettings(btnToggleSettingsNewPass, txtSettingsNewPass);
            CauHinhNutMatSettings(btnToggleSettingsConfirmPass, txtSettingsConfirmPass);

            btnSaveSettings.FlatAppearance.BorderColor = Color.FromArgb(43, 27, 61);
            btnSaveSettings.FlatAppearance.BorderSize = 2;
            btnSaveSettings.FlatAppearance.MouseOverBackColor = Color.FromArgb(178, 22, 30);
            btnSaveSettings.FlatAppearance.MouseDownBackColor = Color.FromArgb(126, 12, 18);
            btnSaveSettings.Click += BtnSaveSettings_Click;

            foreach (TextBox tb in new[] { txtSettingsOldPass, txtSettingsNewPass, txtSettingsConfirmPass, txtSettingsEmail })
            {
                tb.GotFocus += (_, _) => cardSettings.Invalidate();
                tb.LostFocus += (_, _) => cardSettings.Invalidate();
                tb.TextChanged += (_, _) =>
                {
                    if (!string.IsNullOrEmpty(lblSettingsStatus.Text))
                        lblSettingsStatus.Text = string.Empty;
                    cardSettings.Invalidate();
                };
            }
            KhoiTaoDieuKhienDoiAvatarSettings();
        }

        private void CauHinhNutMatSettings(Button btn, TextBox txt)
        {
            btn.Tag = txt;
            btn.Font = new Font("Segoe UI Emoji", 10F, FontStyle.Regular);
            btn.Size = new Size(btn.Width > 0 ? btn.Width : 30, 28);
            btn.Text = txt.UseSystemPasswordChar ? "👁" : "🙈";
            btn.TextAlign = ContentAlignment.MiddleCenter;
            btn.Padding = Padding.Empty;
            btn.Margin = Padding.Empty;
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(242, 239, 233);
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(228, 223, 214);

            btn.Click -= TogglePasswordVisibility_Click;
            btn.Click += TogglePasswordVisibility_Click;
        }

        private static void TogglePasswordVisibility_Click(object? sender, EventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not TextBox txt) return;
            bool show = txt.UseSystemPasswordChar;
            txt.UseSystemPasswordChar = !show;
            btn.Text = show ? "🙈" : "👁";
        }

        private void VeIconTrenNutSidebar(object? sender, PaintEventArgs e)
        {
            if (sender is not Button btn) return;

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Color iconColor = btn.ForeColor;
            using Pen pen = new Pen(iconColor, 1.8f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };

            float ix = 16f;
            float iy = (btn.Height - 16f) / 2f;

            if (btn == btnTabMenu)
            {
                VeKhungBoTronNet(e.Graphics, pen, ix, iy + 2f, 16f, 11f, 4f);
                e.Graphics.DrawLine(pen, ix + 4f, iy + 7.5f, ix + 7.5f, iy + 7.5f);
                e.Graphics.DrawLine(pen, ix + 5.75f, iy + 5.75f, ix + 5.75f, iy + 9.25f);
                using var dot = new SolidBrush(iconColor);
                e.Graphics.FillEllipse(dot, ix + 10.5f, iy + 6.5f, 2f, 2f);
            }
            else if (btn == btnTabRooms)
            {
                VeKhungBoTronNet(e.Graphics, pen, ix + 1f, iy + 1f, 6f, 6f, 1.5f);
                VeKhungBoTronNet(e.Graphics, pen, ix + 9f, iy + 1f, 6f, 6f, 1.5f);
                VeKhungBoTronNet(e.Graphics, pen, ix + 1f, iy + 9f, 6f, 6f, 1.5f);
                VeKhungBoTronNet(e.Graphics, pen, ix + 9f, iy + 9f, 6f, 6f, 1.5f);
            }
            else if (btn == btnTabRacer)
            {
                e.Graphics.DrawEllipse(pen, ix + 5f, iy + 1.5f, 6f, 6f);
                e.Graphics.DrawArc(pen, ix + 2f, iy + 8.5f, 12f, 7f, 180f, 180f);
            }
            else if (btn == btnTabSettings)
            {
                float cx = ix + 8f;
                float cy = iy + 8f;
                e.Graphics.DrawEllipse(pen, cx - 3.2f, cy - 3.2f, 6.4f, 6.4f);
                for (int k = 0; k < 6; k++)
                {
                    double rad = k * Math.PI / 3.0;
                    float x1 = cx + (float)(Math.Cos(rad) * 4.5);
                    float y1 = cy + (float)(Math.Sin(rad) * 4.5);
                    float x2 = cx + (float)(Math.Cos(rad) * 6.8);
                    float y2 = cy + (float)(Math.Sin(rad) * 6.8);
                    e.Graphics.DrawLine(pen, x1, y1, x2, y2);
                }
            }
            else if (btn == btnTabReview)
            {
                PointF[] pts = new PointF[10];
                float cx = ix + 8f;
                float cy = iy + 8.2f;
                for (int k = 0; k < 10; k++)
                {
                    double r = (k % 2 == 0) ? 7.2 : 3.2;
                    double angle = -Math.PI / 2.0 + k * Math.PI / 5.0;
                    pts[k] = new PointF(cx + (float)(Math.Cos(angle) * r), cy + (float)(Math.Sin(angle) * r));
                }
                e.Graphics.DrawPolygon(pen, pts);
            }
            else if (btn == btnLogout)
            {
                e.Graphics.DrawLine(pen, ix + 2f, iy + 2f, ix + 10f, iy + 2f);
                e.Graphics.DrawLine(pen, ix + 2f, iy + 14f, ix + 10f, iy + 14f);
                e.Graphics.DrawLine(pen, ix + 2f, iy + 2f, ix + 2f, iy + 14f);
                e.Graphics.DrawLine(pen, ix + 7f, iy + 8f, ix + 15f, iy + 8f);
                e.Graphics.DrawLine(pen, ix + 12.5f, iy + 5.5f, ix + 15f, iy + 8f);
                e.Graphics.DrawLine(pen, ix + 12.5f, iy + 10.5f, ix + 15f, iy + 8f);
            }
        }

        private static void VeKhungBoTronNet(Graphics g, Pen pen, float x, float y, float w, float h, float r)
        {
            using GraphicsPath p = new GraphicsPath();
            float d = r * 2f;
            p.AddArc(x, y, d, d, 180f, 90f);
            p.AddArc(x + w - d, y, d, d, 270f, 90f);
            p.AddArc(x + w - d, y + h - d, d, d, 0f, 90f);
            p.AddArc(x, y + h - d, d, d, 90f, 90f);
            p.CloseFigure();
            g.DrawPath(pen, p);
        }

        private void DongBoEmailSettings()
        {
            string cleanEmail = string.IsNullOrWhiteSpace(_email) || string.Equals(_email, "Not set", StringComparison.OrdinalIgnoreCase)
                ? string.Empty
                : _email.Trim();

            if (!txtSettingsEmail.Focused && string.IsNullOrWhiteSpace(txtSettingsEmail.Text))
            {
                txtSettingsEmail.Text = cleanEmail;
            }
        }

        private void BtnSaveSettings_Click(object? sender, EventArgs e)
        {
            string oldPass = txtSettingsOldPass.Text;
            string newPass = txtSettingsNewPass.Text;
            string email = txtSettingsEmail.Text.Trim();

            if (string.IsNullOrEmpty(oldPass))
            {
                SetSettingsStatus("Please enter your current password.", isError: true);
                txtSettingsOldPass.Focus();
                return;
            }

            bool hasNewPass = !string.IsNullOrEmpty(newPass);
            bool hasEmail = !string.IsNullOrEmpty(email);
            bool hasAvatar = !string.IsNullOrEmpty(_pendingAvatarPath);

            if (!hasNewPass && !hasEmail && !hasAvatar)
            {
                SetSettingsStatus("Please enter a new password, email, or choose an avatar.", isError: true);
                txtSettingsNewPass.Focus();
                return;
            }

            if (hasNewPass)
            {
                if (newPass.Length < AccountInputValidation.MinPasswordLength)
                {
                    SetSettingsStatus("New password must be at least 8 characters.", isError: true);
                    txtSettingsNewPass.Focus();
                    return;
                }
                if (newPass.Length > AccountInputValidation.MaxPasswordLength)
                {
                    SetSettingsStatus("New password must be 128 characters or fewer.", isError: true);
                    txtSettingsNewPass.Focus();
                    return;
                }
                if (!AccountInputValidation.IsValidPassword(newPass))
                {
                    SetSettingsStatus("New password must include letters and numbers.", isError: true);
                    txtSettingsNewPass.Focus();
                    return;
                }
            }

            if (hasNewPass && txtSettingsConfirmPass.Text != newPass)
            {
                SetSettingsStatus("Passwords do not match.", isError: true);
                txtSettingsConfirmPass.Focus();
                return;
            }

            if (hasEmail && !AccountInputValidation.IsValidEmail(email))
            {
                string message = email.Length > AccountInputValidation.MaxEmailLength
                    ? "Email must be 100 characters or fewer."
                    : "Enter a valid address, e.g. name@example.com.";
                SetSettingsStatus(message, isError: true);
                txtSettingsEmail.Focus();
                return;
            }

            if (hasEmail)
            {
                _email = email;
                lblEmailValue.Text = _email;
            }

            string? savedAvatarPath = null;
            if (!string.IsNullOrEmpty(_pendingAvatarPath))
            {
                ApDungVaLuuAvatar(_pendingAvatarPath);
                savedAvatarPath = DuongDanAvatarLocal;
                _ = DayAvatarLenServerAsync(savedAvatarPath);
                _pendingAvatarPath = null;
            }

            txtSettingsOldPass.Clear();
            txtSettingsNewPass.Clear();
            txtSettingsConfirmPass.Clear();
            SetSettingsStatus("Settings updated successfully.", isError: false);

            SettingsSaveRequested?.Invoke(this, new SettingsSaveEventArgs
            {
                CurrentPassword = oldPass,
                NewPassword = hasNewPass ? newPass : null,
                NewEmail = hasEmail ? email : null,
                AvatarFilePath = savedAvatarPath
            });
        }

        public void SetSettingsStatus(string message, bool isError)
        {
            lblSettingsStatus.ForeColor = isError ? Color.Crimson : Color.ForestGreen;
            lblSettingsStatus.Text = message;
            cardSettings.Invalidate();
        }

        public void AttachLoginForm(Form loginForm)
        {
            _loginForm = loginForm;
        }

        private void HienThiLaiLoginForm()
        {
            Form? target = _loginForm;
            if (target == null || target.IsDisposed)
            {
                foreach (Form f in Application.OpenForms)
                {
                    if (f is LoginForm lf && !lf.IsDisposed)
                    {
                        target = lf;
                        break;
                    }
                }
            }

            if (target != null && !target.IsDisposed)
            {
                if (!target.Visible)
                    target.Show();
                target.BringToFront();
                target.Activate();
            }
        }

        private void GanSuKienDanhGia()
        {
            Button[] starButtons = { btnPickStar1, btnPickStar2, btnPickStar3, btnPickStar4, btnPickStar5 };
            for (int i = 0; i < starButtons.Length; i++)
            {
                int starValue = i + 1;
                starButtons[i].Click += (_, _) => ChonRating(starValue);
            }

            lblReviewPlaceholder.Click += (_, _) => txtReviewComment.Focus();
            txtReviewComment.TextChanged += (_, _) =>
            {
                lblReviewPlaceholder.Visible = string.IsNullOrEmpty(txtReviewComment.Text);
            };
        }

        private void ChonRating(int stars)
        {
            _selectedRating = Math.Clamp(stars, 1, 5);
            Button[] starButtons = { btnPickStar1, btnPickStar2, btnPickStar3, btnPickStar4, btnPickStar5 };

            for (int i = 0; i < starButtons.Length; i++)
            {
                starButtons[i].ForeColor = i < _selectedRating
                    ? Color.FromArgb(235, 178, 22)
                    : Color.Silver;
            }
        }

        public void SetReviewSummary(int[] countsByStar1To5)
        {
            if (countsByStar1To5 == null || countsByStar1To5.Length != 5) return;
            for (int i = 0; i < 5; i++)
            {
                _starCounts[i] = Math.Max(0, countsByStar1To5[i]);
            }
            CapNhatThongKeDanhGia();
        }

        private void CapNhatThongKeDanhGia()
        {
            int total = 0;
            int weightedSum = 0;
            for (int i = 0; i < 5; i++)
            {
                int star = i + 1;
                total += _starCounts[i];
                weightedSum += _starCounts[i] * star;
            }

            double avg = total > 0 ? (double)weightedSum / total : 0.0;
            lblReviewAvgScore.Text = avg.ToString("0.0");
            lblReviewTotalCount.Text = total == 0 ? "No reviews yet" : $"Total reviews: {total}";

            int filledStars = (int)Math.Round(avg, MidpointRounding.AwayFromZero);
            filledStars = Math.Clamp(filledStars, 0, 5);
            lblReviewAvgStars.Text = new string('★', filledStars) + new string('☆', 5 - filledStars);

            Panel[] fills = { pnlFill1, pnlFill2, pnlFill3, pnlFill4, pnlFill5 };
            Panel[] bars = { pnlBar1, pnlBar2, pnlBar3, pnlBar4, pnlBar5 };

            for (int i = 0; i < 5; i++)
            {
                int barWidth = bars[i].Width;
                if (total <= 0 || _starCounts[i] <= 0 || barWidth <= 0)
                {
                    fills[i].Width = 0;
                }
                else
                {
                    fills[i].Width = Math.Max(6, (int)Math.Round((double)_starCounts[i] / total * barWidth));
                }
            }
        }

        private void ChonTab(MainTab tab)
        {
            _activeTab = tab;

            cardMenu.Visible = tab == MainTab.Menu;
            cardRooms.Visible = tab == MainTab.Rooms;
            cardRacer.Visible = tab == MainTab.Racer;
            cardSettings.Visible = tab == MainTab.Settings;
            cardReview.Visible = tab == MainTab.Review;

            CapNhatStyleTab(btnTabMenu, tab == MainTab.Menu);
            CapNhatStyleTab(btnTabRooms, tab == MainTab.Rooms);
            CapNhatStyleTab(btnTabRacer, tab == MainTab.Racer);
            CapNhatStyleTab(btnTabSettings, tab == MainTab.Settings);
            CapNhatStyleTab(btnTabReview, tab == MainTab.Review);
            if (tab == MainTab.Rooms) _ = PollServerAsync();

            pnlSidebar.Invalidate();
        }

        private static void CapNhatStyleTab(Button btn, bool isActive)
        {
            if (isActive)
            {
                btn.BackColor = Color.FromArgb(253, 251, 247);
                btn.ForeColor = Color.FromArgb(43, 27, 61);
                btn.FlatAppearance.BorderColor = Color.FromArgb(245, 208, 120);
                btn.FlatAppearance.BorderSize = 2;
                btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(253, 251, 247);
            }
            else
            {
                btn.BackColor = Color.FromArgb(56, 36, 78);
                btn.ForeColor = Color.FromArgb(238, 232, 245);
                btn.FlatAppearance.BorderColor = Color.FromArgb(88, 64, 116);
                btn.FlatAppearance.BorderSize = 1;
                btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(72, 48, 98);
            }
        }

        private void btnTabMenu_Click(object? sender, EventArgs e) => ChonTab(MainTab.Menu);
        private void btnTabRooms_Click(object? sender, EventArgs e) => ChonTab(MainTab.Rooms);
        private void btnTabRacer_Click(object? sender, EventArgs e) => ChonTab(MainTab.Racer);
        private void btnTabReview_Click(object? sender, EventArgs e) => ChonTab(MainTab.Review);

        private void btnHelp_Click(object? sender, EventArgs e)
        {
            (string title, string subtitle, string body) = _activeTab switch
            {
                MainTab.Rooms => (
                    "OPEN ROOMS GUIDE",
                    "Manage waiting lobbies, live spectating, and custom rooms",
                    "• Room Status: 'Waiting' means players can join before race start; 'Racing' means the match is already in progress.\n" +
                    "• Join / Watch: Click 'Join' to enter an open waiting room (up to 4 racers) or 'Watch' to spectate a live race.\n" +
                    "• Create Room: Click 'Create Room' at the bottom to host a new lobby for other racers.\n" +
                    "• Navigation: Switch back to 'Game Mode' anytime using the left sidebar."),

                MainTab.Racer => (
                    "RACER INFO GUIDE",
                    "View racer profile details, timestamps, and coin balance",
                    "• Identity: Displays your Full Name, Username, Racer ID, registered Email, and Birthday.\n" +
                    "• Racer ID: Click it to copy, then send it to friends so they can invite you from the game lobby.\n" +
                    "• Activity Log: Shows when your account was Created and your Last Login timestamp.\n" +
                    "• Coins: Tracks your current in-game currency balance shown in both the header and profile card.\n" +
                    "• Session: Use 'Logout' at the bottom-left of the sidebar to return safely to the Login screen."),

                MainTab.Settings => (
                    "SETTINGS GUIDE",
                    "Update your login password and registered email",
                    "• Current Password (Required): Enter your current password to authorize changes.\n" +
                    "• New Password (Optional): Leave blank to keep current password; otherwise at least 8 chars with letters and numbers.\n" +
                    "• Confirm Password: Must match the new password exactly.\n" +
                    "• Email (Optional): Update to a valid email address (e.g., name@domain.com).\n" +
                    "• Save: Fill at least New Password or Email, then click 'Save Changes'."),

                MainTab.Review => (
                    "GAME REVIEW GUIDE",
                    "Check rating breakdown and submit your star score & comment",
                    "• Rating Summary: Left side shows the average score (0.0–5.0), star visual, total review count, and distribution bars from 5 down to 1 star.\n" +
                    "• Pick Stars: Click any of the 5 star buttons (★) at the top-right to choose your rating from 1 to 5 stars.\n" +
                    "• Write Feedback: Enter your comment (up to 300 characters) in the text box, then click 'Submit' to record your review."),

                _ => (
                    "GAME MODE GUIDE",
                    "Controls, racing modes, and lobby navigation",
                    "• Driving Controls: W / Up = Accelerate, S / Down = Brake/Reverse, A/D or Left/Right = Steer, Space = Boost/Drift.\n" +
                    "• Single Player: Race solo or practice tracks and lines.\n" +
                    "• Multiplayer: Opens 'Open Rooms' to join waiting lobbies, watch races, or create a room.\n" +
                    "• Leaderboard: View top racer rankings and performance records.\n" +
                    "• Logout: Click 'Logout' at the bottom-left to sign out and return to the Login form.")
            };

            using GuidelinesDialog dlg = new GuidelinesDialog(title, subtitle, body);
            dlg.ShowDialog(this);
        }

        private void OnConnectionStatusChanged(bool isConnected, string statusText)
        {
        }

        private async void Main_Shown(object? sender, EventArgs e)
        {
            Rectangle workArea = Screen.FromControl(this).WorkingArea;
            int width = Math.Min(ClientSize.Width, Math.Max(640, workArea.Width - 16));
            int height = Math.Min(ClientSize.Height, Math.Max(440, workArea.Height - 16));
            if (width != ClientSize.Width || height != ClientSize.Height)
                ClientSize = new Size(width, height);
            ThuGonVaTachBoCuc();

            if (_netClient == null || _isClosing) return;

            try
            {
                ResponsePacket resp = await _netClient.GetInfoAsync();
                if (!IsDisposed && resp.Success && resp.User != null)
                {
                    UserInfoDto u = resp.User;
                    UpdatePlayerStats(
                        string.IsNullOrWhiteSpace(u.Username) ? _username : u.Username,
                        string.IsNullOrWhiteSpace(u.FullName) ? _fullName : u.FullName,
                        u.Coins,
                        u.Email,
                        u.Birthday,
                        u.NgayTao,
                        u.LanDangNhapCuoi,
                        u.RacerId);
                }
                await DongBoAvatarTuServerAsync();
                await TaiDanhGiaTuServerAsync();
            }
            catch
            {
            }
        }

        public void SetConnectionStatus(string statusText, Color color)
        {
            if (!IsDisposed && !lblConnectionStatus.IsDisposed)
            {
                lblConnectionStatus.Visible = false;
                lblConnectionStatus.Text = "";
            }
        }

        private void NapDuLieuMau()
        {
            _rooms.Clear();
            RenderDanhSachPhong();
        }

        public void SetRooms(IEnumerable<RoomItem> rooms)
        {
            _rooms.Clear();
            _rooms.AddRange(rooms);
            RenderDanhSachPhong();
        }

        public void UpdatePlayerStats(
            string username,
            string fullName,
            int coins,
            string? email = null,
            DateTime? birthday = null,
            DateTime? createdAt = null,
            DateTime? lastLogin = null,
            string? racerId = null)
        {
            _fullName = fullName;
            _coins = coins;
            if (!string.IsNullOrWhiteSpace(racerId)) _racerId = racerId;
            if (email != null) _email = string.IsNullOrWhiteSpace(email) ? "Not set" : email;
            if (birthday != null) _birthday = birthday;
            if (createdAt != null) _createdAt = createdAt;
            if (lastLogin != null) _lastLogin = lastLogin;

            string displayFullName = string.IsNullOrWhiteSpace(_fullName) ? username : _fullName;

            lblHeaderUser.Text = $"{username}  |  Coins: {_coins}";
            lblFullNameValue.Text = displayFullName;
            lblUsernameValue.Text = username;
            lblEmailValue.Text = _email;
            lblBirthdayValue.Text = _birthday?.ToString("dd/MM/yyyy") ?? "Not set";
            lblCreatedAtValue.Text = _createdAt?.ToString("dd/MM/yyyy HH:mm") ?? "--";
            lblLastLoginValue.Text = _lastLogin?.ToString("dd/MM/yyyy HH:mm") ?? "--";
            lblCoinsValue.Text = _coins.ToString();
            if (_lblRacerIdValue != null)
                _lblRacerIdValue.Text = string.IsNullOrEmpty(_racerId) ? "--" : _racerId;
            DongBoEmailSettings();
            CapNhatHeaderUserLayout();
            CapNhatAvatarMacDinh();
        }

        public void SetAvatar(Image? avatarImage)
        {
            _customAvatar?.Dispose();
            _customAvatar = avatarImage != null ? new Bitmap(avatarImage) : null;
            CapNhatAvatarMacDinh();
        }

        private void CapNhatHeaderUserLayout()
        {
            if (pnlHeaderBar == null || picHeaderAvatar == null || lblHeaderUser == null) return;

            Size textSize = TextRenderer.MeasureText(
                lblHeaderUser.Text,
                lblHeaderUser.Font,
                new Size(800, pnlHeaderBar.Height),
                TextFormatFlags.NoPadding);

            int userWidth = Math.Max(220, Math.Min(500, textSize.Width + 40));
            int rightMargin = 32;
            int gap = 16;

            lblHeaderUser.Size = new Size(userWidth, 30);
            lblHeaderUser.Location = new Point(pnlHeaderBar.Width - rightMargin - userWidth, (pnlHeaderBar.Height - lblHeaderUser.Height) / 2);

            picHeaderAvatar.Size = new Size(HeaderAvatarSize, HeaderAvatarSize);
            picHeaderAvatar.Location = new Point(lblHeaderUser.Left - gap - HeaderAvatarSize, (pnlHeaderBar.Height - HeaderAvatarSize) / 2);
            ApDungAvatarTron(picHeaderAvatar);
        }

        private void CapNhatAvatarMacDinh()
        {
            if (picHeaderAvatar == null || picRacerAvatar == null) return;

            picHeaderAvatar.Size = new Size(HeaderAvatarSize, HeaderAvatarSize);
            picRacerAvatar.Size = new Size(RacerAvatarSize, RacerAvatarSize);
            ApDungAvatarTron(picHeaderAvatar);
            ApDungAvatarTron(picRacerAvatar);
        }

        private static void ApDungAvatarTron(PictureBox pic)
        {
            if (pic.Width <= 0 || pic.Height <= 0) return;

            int size = Math.Min(pic.Width, pic.Height);
            using GraphicsPath path = new GraphicsPath();
            path.AddEllipse(0, 0, size, size);

            Region? oldRegion = pic.Region;
            pic.Region = new Region(path);
            oldRegion?.Dispose();
            pic.Invalidate();
        }

        private void VeAvatarTron(object? sender, PaintEventArgs e)
        {
            if (sender is not PictureBox pic) return;

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            int size = Math.Min(pic.Width, pic.Height);
            if (size <= 0) return;

            RectangleF circleRect = new RectangleF(0.5f, 0.5f, size - 1f, size - 1f);
            using GraphicsPath clipPath = new GraphicsPath();
            clipPath.AddEllipse(circleRect);

            g.Clear(pic.Parent?.BackColor ?? Color.FromArgb(253, 251, 247));

            StateSaver saver = new StateSaver(g);
            try
            {
                g.SetClip(clipPath);

                Image? img = _customAvatar ?? pic.Image;
                if (img != null)
                {
                    g.DrawImage(img, circleRect);
                }
                else
                {
                    using SolidBrush bgBrush = new SolidBrush(Color.FromArgb(92, 45, 145));
                    g.FillEllipse(bgBrush, circleRect);

                    string initial = string.IsNullOrWhiteSpace(_username)
                        ? "P"
                        : _username.Trim()[0].ToString().ToUpperInvariant();

                    float fontSize = Math.Max(11f, size * 0.42f);
                    using Font font = new Font("Segoe UI", fontSize, FontStyle.Bold, GraphicsUnit.Pixel);
                    using SolidBrush textBrush = new SolidBrush(Color.White);
                    using StringFormat sf = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };
                    g.DrawString(initial, font, textBrush, circleRect, sf);
                }
            }
            finally
            {
                saver.Restore();
            }

            using Pen ringPen = new Pen(Color.FromArgb(245, 208, 120), size >= 50 ? 2f : 1.4f);
            g.DrawEllipse(ringPen, circleRect);
        }

        private sealed class StateSaver
        {
            private readonly Graphics _g;
            private readonly GraphicsState _state;
            public StateSaver(Graphics g) { _g = g; _state = g.Save(); }
            public void Restore() => _g.Restore(_state);
        }

        private void ApDungThongTinNguoiChoi() =>
            UpdatePlayerStats(_username, _fullName, _coins, _email, _birthday, _createdAt, _lastLogin);

        private void RenderDanhSachPhong()
        {
            if (_roomsPanel == null) { cardRooms.Invalidate(); return; }

            int w = _roomsPanel.ClientSize.Width;
            string sig = string.Join(";", _rooms.Select(r =>
                $"{r.RoomCode}|{r.HostName}|{r.CurrentPlayers}|{r.MaxPlayers}|{r.IsRacing}"));

            // Không đổi gì thì khỏi dựng lại (tránh nhấp nháy mỗi 3 giây)
            if (sig == _roomsSig && w == _roomsBuiltWidth) { cardRooms.Invalidate(); return; }
            _roomsSig = sig;
            _roomsBuiltWidth = w;

            int oldY = -_roomsPanel.AutoScrollPosition.Y;
            _roomsPanel.SuspendLayout();

            foreach (Control c in _roomsPanel.Controls.Cast<Control>().ToList())
            {
                _roomsPanel.Controls.Remove(c);
                c.Dispose();
            }

            const int rowH = 84;
            for (int i = 0; i < _rooms.Count; i++)
            {
                RoomItem room = _rooms[i];
                int idx = i;

                var row = new Panel
                {
                    Location = new Point(0, i * rowH),
                    Size = new Size(w, rowH),
                    BackColor = _roomsPanel.BackColor
                };
                row.Paint += (_, e) =>
                {
                    using var pen = new Pen(Color.FromArgb(210, 202, 190), 1.2f) { DashStyle = DashStyle.Dash };
                    e.Graphics.DrawLine(pen, 12, row.Height - 1, row.Width - 12, row.Height - 1);
                };

                bool isFull = room.CurrentPlayers >= room.MaxPlayers;
                bool canJoin = !room.IsRacing && !isFull;

                var btn = new Button
                {
                    Size = new Size(112, 38),
                    Location = new Point(w - 112 - 16, 22),
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                    ForeColor = Color.White,
                    BackColor = canJoin ? Color.FromArgb(155, 17, 24) : Color.Gray,
                    Text = room.IsRacing ? "Racing" : isFull ? "Full" : "Join",
                    Enabled = canJoin,
                    Cursor = canJoin ? Cursors.Hand : Cursors.Default
                };
                btn.FlatAppearance.BorderSize = 0;
                btn.Click += (_, _) => JoinRoomIndex(idx);
                BoGocControl(btn, 10);

                int textW = Math.Max(160, w - 112 - 16 - 24 - 12);
                var title = new Label
                {
                    Text = room.Title,
                    AutoEllipsis = true,
                    Location = new Point(12, 12),
                    Size = new Size(textW, 30),
                    Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(43, 27, 61),
                    BackColor = Color.Transparent
                };
                var info = new Label
                {
                    Text = room.StatusText,
                    AutoEllipsis = true,
                    Location = new Point(12, 44),
                    Size = new Size(textW, 26),
                    Font = new Font("Segoe UI", 10.5F),
                    ForeColor = Color.FromArgb(110, 102, 118),
                    BackColor = Color.Transparent
                };

                row.Controls.Add(title);
                row.Controls.Add(info);
                row.Controls.Add(btn);
                _roomsPanel.Controls.Add(row);
            }

            _roomsPanel.ResumeLayout(true);
            _roomsPanel.AutoScrollPosition = new Point(0, oldY);   // giữ nguyên vị trí cuộn
            cardRooms.Invalidate();
        }


        private void VeChiTietCardRacer(object? sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using Pen dividerPen = new Pen(Color.FromArgb(224, 218, 208), 1.2f);

            int[] bottoms =
            {
                lblUsernameValue.Bottom,
                _lblRacerIdValue?.Bottom ?? lblUsernameValue.Bottom,
                lblEmailValue.Bottom,
                lblBirthdayValue.Bottom,
                lblCreatedAtValue.Bottom,
                lblLastLoginValue.Bottom
            };

            foreach (int b in bottoms)
            {
                int lineY = b + 18;
                if (lineY < lblCoinsValue.Top - 16)
                    e.Graphics.DrawLine(dividerPen, 56, lineY, cardRacer.Width - 56, lineY);
            }
        }

        private void VeChiTietCardRooms(object? sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            if (_rooms.Count == 0)
            {
                using Font f = new Font("Segoe UI", 11F);
                TextRenderer.DrawText(
                    e.Graphics,
                    "No open rooms yet.\nPress Create Room to host one,\nor use Multiplayer to join by code.",
                    f,
                    new Rectangle(52, 230, cardRooms.Width - 104, 100),
                    Color.FromArgb(110, 102, 118),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.WordBreak);
            }
            using Pen dashPen = new Pen(Color.FromArgb(210, 202, 190), 1.2f)
            {
                DashStyle = DashStyle.Dash
            };

            Label[] infos = { lblRoom1Info, lblRoom2Info, lblRoom3Info };
            Button[] btns = { btnJoinRoom1, btnSpectateRoom2, btnJoinRoom3 };

            for (int i = 0; i < infos.Length; i++)
            {
                if (!infos[i].Visible) continue;
                int lineY = infos[i].Bottom + 24;
                int rightLimit = btns[i].Visible ? btns[i].Left - 32 : cardRooms.Width - 56;
                if (rightLimit > 76)
                    e.Graphics.DrawLine(dashPen, 56, lineY, rightLimit, lineY);
            }
        }

        private void VeChiTietCardSettings(object? sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            VeKhungSettings(e.Graphics, _settingsAuthBox);
            VeKhungSettings(e.Graphics, _settingsUpdateBox);

            bool hasError = !string.IsNullOrEmpty(lblSettingsStatus.Text) && lblSettingsStatus.ForeColor == Color.Crimson;
            string currentEmail = string.IsNullOrWhiteSpace(_email) || string.Equals(_email, "Not set", StringComparison.OrdinalIgnoreCase)
                ? string.Empty
                : _email.Trim();
            string inputEmail = txtSettingsEmail.Text.Trim();
            bool emailInvalid = hasError
                && !string.IsNullOrEmpty(inputEmail)
                && !string.Equals(inputEmail, currentEmail, StringComparison.OrdinalIgnoreCase)
                && !AccountInputValidation.IsValidEmail(inputEmail);

            VeGachChanSettings(e.Graphics, txtSettingsOldPass, btnToggleSettingsOldPass, hasError && string.IsNullOrEmpty(txtSettingsOldPass.Text));
            VeGachChanSettings(e.Graphics, txtSettingsNewPass, btnToggleSettingsNewPass, hasError && !string.IsNullOrEmpty(txtSettingsNewPass.Text));
            VeGachChanSettings(e.Graphics, txtSettingsConfirmPass, btnToggleSettingsConfirmPass, hasError && !string.IsNullOrEmpty(txtSettingsConfirmPass.Text) && txtSettingsConfirmPass.Text != txtSettingsNewPass.Text);
            VeGachChanSettings(e.Graphics, txtSettingsEmail, null, emailInvalid);
        }

        private static void VeKhungSettings(Graphics g, Rectangle r)
        {
            if (r.Width <= 0 || r.Height <= 0) return;

            using GraphicsPath path = new GraphicsPath();
            int d = 18;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();

            using SolidBrush fill = new SolidBrush(Color.FromArgb(250, 247, 241));
            using Pen border = new Pen(Color.FromArgb(222, 214, 202), 1.2f);
            g.FillPath(fill, path);
            g.DrawPath(border, path);
        }

        private static void VeGachChanSettings(Graphics g, TextBox txt, Control? rightCtrl, bool isError)
        {
            Color color = isError
                ? Color.Crimson
                : txt.Focused
                    ? Color.FromArgb(43, 27, 61)
                    : Color.Silver;
            float width = isError || txt.Focused ? 2.2f : 1.5f;

            using Pen pen = new Pen(color, width);
            int y = txt.Bottom + 5;
            int right = rightCtrl != null ? rightCtrl.Right : txt.Right;
            g.DrawLine(pen, txt.Left - 1, y, right, y);
        }

        private void VeChiTietCardReview(object? sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            int leftColW = Math.Min(230, Math.Max(190, (cardReview.Width - 136) * 35 / 100));
            int splitX = 48 + leftColW + 28;
            using Pen splitPen = new Pen(Color.FromArgb(224, 218, 208), 1.2f);
            e.Graphics.DrawLine(splitPen, splitX, 114, splitX, cardReview.Height - 44);
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
            if (WindowState != FormWindowState.Minimized) ThuGonVaTachBoCuc();
        }

        private void ThuGonVaTachBoCuc()
        {
            CanGiuaBoCuc();
        }

        private void CanGiuaBoCuc()
        {
            if (pnlSidebar == null || pnlContentHost == null || cardMenu == null || cardRooms == null || cardRacer == null || cardSettings == null || cardReview == null
                || _lblRacerIdCaption == null || _lblRacerIdValue == null || WindowState == FormWindowState.Minimized)
                return;

            SuspendLayout();

            int winBtnW = 40;
            int winBtnH = 28;
            int winBtnGap = 8;
            int winBtnY = Math.Max(2, (pnlTitleBar.Height - winBtnH) / 2);

            btnExit.Size = new Size(winBtnW, winBtnH);
            btnMaximize.Size = new Size(winBtnW, winBtnH);
            btnMinimize.Size = new Size(winBtnW, winBtnH);

            btnExit.Location = new Point(ClientSize.Width - 18 - winBtnW, winBtnY);
            btnMaximize.Location = new Point(btnExit.Left - winBtnGap - winBtnW, winBtnY);
            btnMinimize.Location = new Point(btnMaximize.Left - winBtnGap - winBtnW, winBtnY);

            pnlContentHost.Invalidate();
            CapNhatHeaderUserLayout();

            int sidePadX = 20;
            int sideBtnW = Math.Max(176, pnlSidebar.Width - sidePadX * 2);
            int tabH = 44;
            int tabGap = 20;
            int tabTop = 32;
            Button[] sideTabs = { btnTabMenu, btnTabRooms, btnTabRacer, btnTabSettings, btnTabReview };
            for (int i = 0; i < sideTabs.Length; i++)
            {
                sideTabs[i].Location = new Point(sidePadX, tabTop + i * (tabH + tabGap));
                sideTabs[i].Size = new Size(sideBtnW, tabH);
            }

            btnLogout.Size = new Size(sideBtnW, 48);
            btnLogout.Location = new Point(
                sidePadX,
                Math.Max(tabTop + sideTabs.Length * (tabH + tabGap) + 36, pnlSidebar.Height - btnLogout.Height - 32));

            int cardWidth = Math.Min(760, Math.Max(500, pnlContentHost.Width - 32));
            int cardHeight = Math.Min(650, Math.Max(320, pnlContentHost.Height - 32));
            bool compactCards = cardWidth < 640 || cardHeight < 590;

            int x = Math.Max(12, (pnlContentHost.Width - cardWidth) / 2);
            int y = Math.Max(12, (pnlContentHost.Height - cardHeight) / 2);

            shadowContent.Size = new Size(cardWidth, cardHeight);
            shadowContent.Location = new Point(x + 8, y + 8);
            shadowContent.SendToBack();
            BoGocControl(shadowContent, 16);

            Control[] cards = { cardMenu, cardRooms, cardRacer, cardSettings, cardReview };
            foreach (Control c in cards)
            {
                if (c is Panel panel)
                {
                    panel.AutoScroll = compactCards;
                    panel.AutoScrollMinSize = compactCards ? new Size(640, 590) : Size.Empty;
                    if (compactCards) panel.AutoScrollPosition = Point.Empty;
                }
                c.Size = new Size(cardWidth, cardHeight);
                c.Location = new Point(x, y);
                c.BringToFront();
                BoGocControl(c, 16);
                c.Invalidate();
            }

            CanChinhLayoutSettingsTrongMain(cardWidth, cardHeight);

            int helpSize = 32;
            int helpX = cardWidth - 26 - helpSize;
            int headerX = 68;
            int headerTextW = cardWidth - headerX * 2;

            (Button Help, Label Title, Label Sub)[] headers =
            {
                (btnHelpMenu, lblMenuTitle, lblMenuSub),
                (btnHelpRooms, lblRoomsTitle, lblRoomsSub),
                (btnHelpRacer, lblRacerTitle, lblRacerSub),
                (btnHelpReview, lblReviewTitle, lblReviewSub)
            };
            foreach (var h in headers)
            {
                h.Help.Size = new Size(helpSize, helpSize);
                h.Help.Location = new Point(helpX, 112);

                h.Title.Location = new Point(headerX, 116);
                h.Title.Size = new Size(headerTextW, 46);
                h.Title.TextAlign = ContentAlignment.MiddleCenter;

                h.Sub.Location = new Point(headerX, 168);
                h.Sub.Size = new Size(headerTextW, 30);
                h.Sub.TextAlign = ContentAlignment.MiddleCenter;
            }

            // --- MENU ---
            int menuBtnH = 56;
            int menuBtnGap = 14;
            int menuBlockH = 3 * menuBtnH + 2 * menuBtnGap;
            int menuStartY = Math.Max(136, 134 + Math.Max(0, (cardHeight - 136 - menuBlockH - 56) / 2));
            int menuBtnW = Math.Min(480, cardWidth - 160);
            int menuBtnX = (cardWidth - menuBtnW) / 2;

            Button[] menuBtns = { btnSinglePlayer, btnMultiplayer, btnLeaderboard };
            for (int i = 0; i < menuBtns.Length; i++)
            {
                menuBtns[i].Size = new Size(menuBtnW, menuBtnH);
                menuBtns[i].Location = new Point(menuBtnX, menuStartY + i * (menuBtnH + menuBtnGap));
            }

            // --- ROOMS ---
            btnCreateRoom.Size = new Size(cardWidth - 104, 48);
            btnCreateRoom.Location = new Point(52, cardHeight - btnCreateRoom.Height - 24);

            Label[] roomTitles = { lblRoom1Title, lblRoom2Title, lblRoom3Title };
            Label[] roomInfos = { lblRoom1Info, lblRoom2Info, lblRoom3Info };
            Button[] roomBtns = { btnJoinRoom1, btnSpectateRoom2, btnJoinRoom3 };
            int roomStartY = 216;
            int roomAvailH = Math.Max(210, btnCreateRoom.Top - roomStartY - 16);
            int roomStepY = Math.Min(92, Math.Max(74, roomAvailH / 3));
            for (int i = 0; i < 3; i++)
            {
                int ry = roomStartY + i * roomStepY;
                roomBtns[i].Size = new Size(112, 38);
                roomBtns[i].Location = new Point(cardWidth - 52 - roomBtns[i].Width, ry + 2);

                int textMaxW = Math.Max(220, roomBtns[i].Left - 80);
                roomTitles[i].Location = new Point(52, ry - 2);
                roomTitles[i].Size = new Size(textMaxW, 28);
                roomInfos[i].Location = new Point(52, ry + 26);
                roomInfos[i].Size = new Size(textMaxW, 24);
            }
            if (_roomsPanel != null)
            {
                int top = 216;
                _roomsPanel.Location = new Point(40, top);
                _roomsPanel.Size = new Size(cardWidth - 80, Math.Max(120, btnCreateRoom.Top - top - 16));
                RenderDanhSachPhong();
            }
            // --- RACER ---
            picRacerAvatar.Size = new Size(RacerAvatarSize, RacerAvatarSize);
            picRacerAvatar.Location = new Point(52, 212);
            CapNhatAvatarMacDinh();

            int topCapX = 148;
            int topValX = 264;
            int topValW = Math.Max(190, cardWidth - topValX - 52);
            lblFullNameCaption.Location = new Point(topCapX, 216);
            lblFullNameCaption.Size = new Size(110, 28);
            lblFullNameValue.Location = new Point(topValX, 216);
            lblFullNameValue.Size = new Size(topValW, 28);

            lblUsernameCaption.Location = new Point(topCapX, 254);
            lblUsernameCaption.Size = new Size(110, 28);
            lblUsernameValue.Location = new Point(topValX, 254);
            lblUsernameValue.Size = new Size(topValW, 28);

            int lowCapX = 52;
            int lowValX = 204;
            int lowValW = Math.Max(220, cardWidth - lowValX - 52);
            int racerRowStartY = 304;
            int racerRowStepY = 40;
            (Label Cap, Label Val)[] racerRows =
            {
                (_lblRacerIdCaption!, _lblRacerIdValue!),
                (lblEmailCaption, lblEmailValue),
                (lblBirthdayCaption, lblBirthdayValue),
                (lblCreatedAtCaption, lblCreatedAtValue),
                (lblLastLoginCaption, lblLastLoginValue)
            };
            for (int i = 0; i < racerRows.Length; i++)
            {
                int ry = racerRowStartY + i * racerRowStepY;
                racerRows[i].Cap.Location = new Point(lowCapX, ry);
                racerRows[i].Cap.Size = new Size(140, 28);
                racerRows[i].Val.Location = new Point(lowValX, ry);
                racerRows[i].Val.Size = new Size(lowValW, 28);
            }

            int coinsY = Math.Min(cardHeight - 58, racerRowStartY + racerRows.Length * racerRowStepY + 12);
            lblCoinsCaption.Location = new Point(lowCapX, coinsY);
            lblCoinsCaption.Size = new Size(140, 28);
            lblCoinsValue.Location = new Point(lowValX, coinsY);
            lblCoinsValue.Size = new Size(lowValW, 28);

            // --- REVIEW ---
            int reviewTopY = 198;
            int leftColW = Math.Min(236, Math.Max(196, (cardWidth - 144) * 35 / 100));
            lblReviewAvgScore.Location = new Point(48, reviewTopY);
            lblReviewAvgScore.Size = new Size(leftColW, 76);
            lblReviewAvgScore.TextAlign = ContentAlignment.MiddleCenter;

            lblReviewAvgStars.Location = new Point(48, reviewTopY + 80);
            lblReviewAvgStars.Size = new Size(leftColW, 34);
            lblReviewAvgStars.Font = new Font("Segoe UI Emoji", 13F, FontStyle.Regular);
            lblReviewAvgStars.TextAlign = ContentAlignment.MiddleCenter;

            lblReviewTotalCount.Location = new Point(48, reviewTopY + 118);
            lblReviewTotalCount.Size = new Size(leftColW, 24);
            lblReviewTotalCount.TextAlign = ContentAlignment.MiddleCenter;

            Label[] starLabels = { lblStar5, lblStar4, lblStar3, lblStar2, lblStar1 };
            Panel[] barPanels = { pnlBar5, pnlBar4, pnlBar3, pnlBar2, pnlBar1 };
            int barW = Math.Max(120, leftColW - 38);
            int starListStartY = reviewTopY + 148;
            int starListStepY = Math.Min(34, Math.Max(27, (cardHeight - starListStartY - 24) / 5));
            for (int i = 0; i < 5; i++)
            {
                int rowY = starListStartY + i * starListStepY;
                starLabels[i].Location = new Point(48, rowY);
                starLabels[i].Size = new Size(28, 24);
                starLabels[i].TextAlign = ContentAlignment.MiddleLeft;
                barPanels[i].Location = new Point(80, rowY + 5);
                barPanels[i].Size = new Size(barW, 13);
            }
            CapNhatThongKeDanhGia();

            int rightColX = 48 + leftColW + 64;
            int rightColW = Math.Max(210, cardWidth - rightColX - 64);

            lblPickStarCaption.Location = new Point(rightColX, reviewTopY);
            lblPickStarCaption.Size = new Size(rightColW, 26);
            lblPickStarCaption.TextAlign = ContentAlignment.MiddleLeft;

            Button[] pickStars = { btnPickStar1, btnPickStar2, btnPickStar3, btnPickStar4, btnPickStar5 };
            int starBtnW = 30;
            int starBtnH = 38;
            int starBtnGap = 5;
            int pickStarY = reviewTopY + 30;
            for (int i = 0; i < pickStars.Length; i++)
            {
                pickStars[i].Size = new Size(starBtnW, starBtnH);
                pickStars[i].Location = new Point(rightColX + i * (starBtnW + starBtnGap), pickStarY);
                pickStars[i].Text = "★";
                pickStars[i].Font = new Font("Segoe UI", 12.5F, FontStyle.Bold);
                pickStars[i].TextAlign = ContentAlignment.MiddleCenter;
                pickStars[i].Padding = Padding.Empty;
                pickStars[i].Margin = Padding.Empty;
            }

            int commentCaptionY = pickStarY + starBtnH + 14;
            lblReviewCommentCaption.Location = new Point(rightColX, commentCaptionY);
            lblReviewCommentCaption.Size = new Size(rightColW, 24);
            lblReviewCommentCaption.TextAlign = ContentAlignment.MiddleLeft;

            int submitBtnH = 40;
            int commentBoxTop = commentCaptionY + 28;
            int commentBoxH = Math.Max(110, Math.Min(195, cardHeight - commentBoxTop - submitBtnH - 72));

            txtReviewComment.Location = new Point(rightColX, commentBoxTop);
            txtReviewComment.Size = new Size(rightColW, commentBoxH);
            lblReviewPlaceholder.Location = new Point(rightColX + 10, commentBoxTop + 8);
            lblReviewPlaceholder.Size = new Size(Math.Max(140, rightColW - 20), 26);

            btnSubmitReview.Size = new Size(rightColW, submitBtnH);
            btnSubmitReview.Location = new Point(rightColX, txtReviewComment.Bottom + 16);
            BoGocControl(pnlSidebar, 0);
            BoGocControl(btnLogout, 10);
            foreach (Button b in sideTabs) BoGocControl(b, 10);
            foreach (Button b in menuBtns) BoGocControl(b, 12);
            foreach (Button b in roomBtns) BoGocControl(b, 10);
            BoGocControl(btnCreateRoom, 12);
            BoGocControl(btnSubmitReview, 10);

            ResumeLayout(true);
        }

        private void CanChinhLayoutSettingsTrongMain(int cardWidth, int cardHeight)
        {
            int outerPad = 36;
            int innerPad = 24;
            int fullW = Math.Max(340, cardWidth - outerPad * 2);
            int innerW = Math.Max(280, fullW - innerPad * 2);

            lblSettingsTitle.Location = new Point(outerPad, 10);
            lblSettingsTitle.Size = new Size(Math.Max(240, cardWidth - outerPad * 2 - 52), 40);
            lblSettingsTitle.TextAlign = ContentAlignment.MiddleLeft;

            lblSettingsSub.Location = new Point(outerPad, 50);
            lblSettingsSub.Size = new Size(Math.Max(260, cardWidth - outerPad * 2 - 52), 28);
            lblSettingsSub.TextAlign = ContentAlignment.MiddleLeft;

            btnHelpSettings.Size = new Size(34, 34);
            btnHelpSettings.Location = new Point(cardWidth - 26 - btnHelpSettings.Width, 18);

            // Avatar to nằm giữa, bấm trực tiếp để chọn ảnh
            int avatarTopY = 82;
            CanChinhViTriAvatarSettings(cardWidth, avatarTopY);

            int authTop = avatarTopY + 76 + 14;
            int authH = 112;
            _settingsAuthBox = new Rectangle(outerPad, authTop, fullW, authH);

            lblSettingsAuthBadge.Size = new Size(140, 26);
            lblSettingsAuthBadge.Location = new Point(outerPad + innerPad, authTop + 8);
            lblSettingsAuthBadge.TextAlign = ContentAlignment.MiddleCenter;
            BoGocControl(lblSettingsAuthBadge, 6);

            lblSettingsOldPass.Location = new Point(outerPad + innerPad, authTop + 36);
            lblSettingsOldPass.Size = new Size(innerW, 26);
            lblSettingsOldPass.TextAlign = ContentAlignment.MiddleLeft;

            int row1InputY = authTop + 65;
            int inputH = 28;
            btnToggleSettingsOldPass.Size = new Size(38, 30);
            txtSettingsOldPass.Location = new Point(outerPad + innerPad, row1InputY);
            txtSettingsOldPass.Size = new Size(innerW - btnToggleSettingsOldPass.Width - 12, inputH);
            btnToggleSettingsOldPass.Location = new Point(
                outerPad + innerPad + innerW - btnToggleSettingsOldPass.Width,
                row1InputY + (inputH - btnToggleSettingsOldPass.Height) / 2);

            int updateTop = _settingsAuthBox.Bottom + 14;
            int updateH = 212;
            _settingsUpdateBox = new Rectangle(outerPad, updateTop, fullW, updateH);

            lblSettingsUpdateBadge.Size = new Size(156, 26);
            lblSettingsUpdateBadge.Location = new Point(outerPad + innerPad, updateTop + 8);
            lblSettingsUpdateBadge.TextAlign = ContentAlignment.MiddleCenter;
            BoGocControl(lblSettingsUpdateBadge, 6);

            int colGap = 48;
            int halfW = Math.Max(160, (innerW - colGap) / 2);
            int leftInnerX = outerPad + innerPad;
            int rightInnerX = leftInnerX + halfW + colGap;

            int row2LabelY = updateTop + 36;
            int row2InputY = updateTop + 65;

            btnToggleSettingsNewPass.Size = new Size(38, 30);
            lblSettingsNewPass.Location = new Point(leftInnerX, row2LabelY);
            lblSettingsNewPass.Size = new Size(halfW, 26);
            lblSettingsNewPass.TextAlign = ContentAlignment.MiddleLeft;
            txtSettingsNewPass.Location = new Point(leftInnerX, row2InputY);
            txtSettingsNewPass.Size = new Size(halfW - btnToggleSettingsNewPass.Width - 12, inputH);
            btnToggleSettingsNewPass.Location = new Point(
                leftInnerX + halfW - btnToggleSettingsNewPass.Width,
                row2InputY + (inputH - btnToggleSettingsNewPass.Height) / 2);

            btnToggleSettingsConfirmPass.Size = new Size(38, 30);
            lblSettingsConfirmPass.Location = new Point(rightInnerX, row2LabelY);
            lblSettingsConfirmPass.Size = new Size(halfW, 26);
            lblSettingsConfirmPass.TextAlign = ContentAlignment.MiddleLeft;
            txtSettingsConfirmPass.Location = new Point(rightInnerX, row2InputY);
            txtSettingsConfirmPass.Size = new Size(halfW - btnToggleSettingsConfirmPass.Width - 12, inputH);
            btnToggleSettingsConfirmPass.Location = new Point(
                rightInnerX + halfW - btnToggleSettingsConfirmPass.Width,
                row2InputY + (inputH - btnToggleSettingsConfirmPass.Height) / 2);

            int row3LabelY = updateTop + 112;
            int row3InputY = updateTop + 141;

            lblSettingsEmail.Location = new Point(leftInnerX, row3LabelY);
            lblSettingsEmail.Size = new Size(innerW, 26);
            lblSettingsEmail.TextAlign = ContentAlignment.MiddleLeft;
            txtSettingsEmail.Location = new Point(leftInnerX, row3InputY);
            txtSettingsEmail.Size = new Size(innerW, inputH);

            int btnW = Math.Min(220, Math.Max(180, fullW / 3));
            int btnH = 42;
            int availBelowBox = cardHeight - _settingsUpdateBox.Bottom;
            int btnY = _settingsUpdateBox.Bottom + Math.Max(16, (availBelowBox - btnH) / 2);
            btnY = Math.Min(cardHeight - btnH - 14, btnY);

            btnSaveSettings.Size = new Size(btnW, btnH);
            btnSaveSettings.Location = new Point((cardWidth - btnW) / 2, btnY);
            BoGocControl(btnSaveSettings, 10);

            lblSettingsStatus.Size = new Size(fullW, 26);
            lblSettingsStatus.Location = new Point(outerPad, btnSaveSettings.Top - 28);
            lblSettingsStatus.TextAlign = ContentAlignment.MiddleCenter;
        }
        private static void BoGocControl(Control ctrl, int radius)
        {
            if (ctrl.Width <= 0 || ctrl.Height <= 0 || radius <= 0) return;

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

        private readonly HashSet<string> _submittedReviewers = new(StringComparer.OrdinalIgnoreCase);

        private void btnLeaderboard_Click(object? sender, EventArgs e)
        {
            if (_netClient == null)
            {
                PopupNoticeForm.ShowNotice(this, "Leaderboard",
                    "Not connected to server.", PopupNoticeForm.NoticeKind.Warning);
                return;
            }
            using var dlg = new LeaderboardForm(_netClient, _username);
            dlg.ShowDialog(this);
        }

        private void JoinOrWatchRoom(int index)
        {
            if (index < 0 || index >= _rooms.Count) return;
            RoomItem room = _rooms[index];
            string action = room.IsRacing ? "Spectating" : "Joining";
            PopupNoticeForm.ShowNotice(
                this,
                room.Title,
                $"{action} {room.Title} ({room.StatusText})...",
                PopupNoticeForm.NoticeKind.Info);
        }


        private void btnCreateRoom_Click(object? sender, EventArgs e)
        {
            KhoiChayGame("create");
        }

        private async void btnSubmitReview_Click(object? sender, EventArgs e)
        {
            string reviewerKey = string.IsNullOrWhiteSpace(_username) ? "guest" : _username.Trim();
            if (_submittedReviewers.Contains(reviewerKey))
            {
                PopupNoticeForm.ShowNotice(this, "Review",
                    "Each account can only submit one review.", PopupNoticeForm.NoticeKind.Warning);
                return;
            }

            string comment = txtReviewComment.Text.Trim();
            if (comment.Length > 300)
            {
                PopupNoticeForm.ShowNotice(this, "Review",
                    "Comment must be 300 characters or fewer.", PopupNoticeForm.NoticeKind.Warning);
                return;
            }

            if (_netClient == null)
            {
                PopupNoticeForm.ShowNotice(this, "Review",
                    "Not connected to server.", PopupNoticeForm.NoticeKind.Warning);
                return;
            }

            btnSubmitReview.Enabled = false;
            ResponsePacket r = await _netClient.SubmitReviewAsync(_selectedRating, comment);
            if (IsDisposed) return;

            if (!r.Success)
            {
                if (r.Code == ErrorCodes.AlreadyReviewed)
                    await TaiDanhGiaTuServerAsync();
                else
                    btnSubmitReview.Enabled = true;

                PopupNoticeForm.ShowNotice(this, "Review",
                    string.IsNullOrWhiteSpace(r.Message) ? "Could not submit review." : r.Message,
                    PopupNoticeForm.NoticeKind.Warning);
                return;
            }

            _submittedReviewers.Add(reviewerKey);
            btnSubmitReview.Text = "Reviewed";
            if (r.ReviewSummary != null) SetReviewSummary(r.ReviewSummary.Counts);

            GameReviewItem item = new GameReviewItem
            {
                Username = _username,
                Stars = _selectedRating,
                Comment = comment,
                CreatedAt = DateTime.Now
            };

            txtReviewComment.Clear();
            lblReviewPlaceholder.Visible = true;

            PopupNoticeForm.ShowNotice(this, "Review",
                "Thank you! Your review has been submitted.", PopupNoticeForm.NoticeKind.Info);

            ReviewSubmitted?.Invoke(this, item);
        }
        private void btnLogout_Click(object? sender, EventArgs e)
        {
            if (_isLoggingOut || _isClosing) return;
            _isLoggingOut = true;
            LogoutRequested?.Invoke(this, EventArgs.Empty);
            HienThiLaiLoginForm();
            Close();
        }

        private void Main_FormClosing(object? sender, FormClosingEventArgs e)
        {
            _pollTimer.Stop();
            _pollTimer.Dispose();
            _isClosing = true;
            if (_netClient != null)
            {
                _netClient.ConnectionStatusChanged -= OnConnectionStatusChanged;
                _netClient.Disconnect();
            }

            if (!_isLoggingOut)
            {
                ExitRequested?.Invoke(this, EventArgs.Empty);
                HienThiLaiLoginForm();
            }

            _customAvatar?.Dispose();
            _customAvatar = null;
            _backgroundImage?.Dispose();
            _backgroundImage = null;
        }

        private void btnExit_Click(object? sender, EventArgs e)
        {
            Close();
        }

        private void btnMaximize_Click(object? sender, EventArgs e)
        {
            WindowState = WindowState == FormWindowState.Normal
                ? FormWindowState.Maximized
                : FormWindowState.Normal;
            BeginInvoke(new Action(ThuGonVaTachBoCuc));
        }

        private void btnMinimize_Click(object? sender, EventArgs e)
        {
            WindowState = FormWindowState.Minimized;
        }
    }
}
