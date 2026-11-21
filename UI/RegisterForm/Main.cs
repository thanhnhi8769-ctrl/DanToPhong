using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using DanToPhong.Protocol;
using UI.Services;

namespace UI
{
    public partial class MainForm : Form
    {
        public sealed class RoomItem
        {
            public string RoomCode { get; init; } = "01";
            public int CurrentPlayers { get; init; }
            public int MaxPlayers { get; init; } = 4;
            public bool IsRacing { get; init; }
            public string Title => $"Room {RoomCode}";
            public string StatusText => $"{CurrentPlayers}/{MaxPlayers} · {(IsRacing ? "Racing" : "Waiting")}";
        }

        private readonly NetworkClientService? _netClient;
        private readonly string _username;
        private string _fullName;
        private string _email;
        private DateTime? _birthday;
        private DateTime? _createdAt;
        private DateTime? _lastLogin;
        private int _coins;
        private bool _isLoggingOut = false;
        private readonly List<RoomItem> _rooms = new();

        public MainForm(NetworkClientService netClient, UserInfoDto user)
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

        public MainForm(string username)
            : this(username, username, "", null, DateTime.Now, DateTime.Now, 1250, null)
        {
        }

        public MainForm(
            string username,
            string fullName,
            string? email = null,
            DateTime? birthday = null,
            DateTime? createdAt = null,
            DateTime? lastLogin = null,
            int coins = 1250,
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
            DoubleBuffered = true;

            cardRacer.Paint += VeChiTietCardRacer;
            cardRooms.Paint += VeChiTietCardRooms;
            Shown += MainForm_Shown;
            FormClosing += MainForm_FormClosing;

            if (_netClient != null)
            {
                _netClient.ConnectionStatusChanged += OnConnectionStatusChanged;
            }

            NapDuLieuMau();
            ApDungThongTinNguoiChoi();
            SetConnectionStatus(_netClient == null || _netClient.IsConnected ? "Connected" : "Disconnected",
                _netClient == null || _netClient.IsConnected ? Color.LightGreen : Color.FromArgb(245, 208, 120));
            CanGiuaBoCuc();
        }

        private void OnConnectionStatusChanged(bool isConnected, string statusText)
        {
            if (IsDisposed) return;
            SetConnectionStatus(statusText, isConnected ? Color.LightGreen : Color.FromArgb(245, 208, 120));
        }

        private async void MainForm_Shown(object? sender, EventArgs e)
        {
            if (_netClient == null || _isLoggingOut) return;

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
                        u.LanDangNhapCuoi);
                }
            }
            catch
            {
                // Không lộ tên ngoại lệ ra UI
            }
        }

        public void SetConnectionStatus(string statusText, Color color)
        {
            if (IsDisposed || lblConnectionStatus.IsDisposed) return;
            if (lblConnectionStatus.InvokeRequired)
            {
                lblConnectionStatus.Invoke(() => SetConnectionStatus(statusText, color));
                return;
            }
            lblConnectionStatus.Text = $"Server: {statusText}";
            lblConnectionStatus.ForeColor = color;
        }

        private void NapDuLieuMau()
        {
            _rooms.Clear();
            _rooms.AddRange(new[]
            {
                new RoomItem { RoomCode = "01", CurrentPlayers = 2, MaxPlayers = 4, IsRacing = false },
                new RoomItem { RoomCode = "02", CurrentPlayers = 4, MaxPlayers = 4, IsRacing = true },
                new RoomItem { RoomCode = "03", CurrentPlayers = 1, MaxPlayers = 4, IsRacing = false }
            });
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
            DateTime? lastLogin = null)
        {
            _fullName = fullName;
            _coins = coins;
            if (email != null) _email = string.IsNullOrWhiteSpace(email) ? "Not set" : email;
            if (birthday != null) _birthday = birthday;
            if (createdAt != null) _createdAt = createdAt;
            if (lastLogin != null) _lastLogin = lastLogin;

            lblHeaderUser.Text = $"{username}  |  Coins: {_coins}";
            lblFullNameValue.Text = _fullName;
            lblUsernameValue.Text = username;
            lblEmailValue.Text = _email;
            lblBirthdayValue.Text = _birthday?.ToString("dd/MM/yyyy") ?? "Not set";
            lblCreatedAtValue.Text = _createdAt?.ToString("dd/MM/yyyy HH:mm") ?? "--";
            lblLastLoginValue.Text = _lastLogin?.ToString("dd/MM/yyyy HH:mm") ?? "--";
            lblCoinsValue.Text = _coins.ToString();
        }

        private void ApDungThongTinNguoiChoi() =>
            UpdatePlayerStats(_username, _fullName, _coins, _email, _birthday, _createdAt, _lastLogin);

        private void RenderDanhSachPhong()
        {
            var rows = new[]
            {
                (Title: lblRoom1Title, Info: lblRoom1Info, ActionBtn: btnJoinRoom1),
                (Title: lblRoom2Title, Info: lblRoom2Info, ActionBtn: btnSpectateRoom2),
                (Title: lblRoom3Title, Info: lblRoom3Info, ActionBtn: btnJoinRoom3)
            };

            for (int i = 0; i < rows.Length; i++)
            {
                bool hasRoom = i < _rooms.Count;
                rows[i].Title.Visible = hasRoom;
                rows[i].Info.Visible = hasRoom;
                rows[i].ActionBtn.Visible = hasRoom;

                if (!hasRoom) continue;

                RoomItem room = _rooms[i];
                rows[i].Title.Text = room.Title;
                rows[i].Info.Text = room.StatusText;

                if (room.IsRacing)
                {
                    rows[i].ActionBtn.Text = "Watch";
                    rows[i].ActionBtn.BackColor = Color.White;
                    rows[i].ActionBtn.ForeColor = Color.FromArgb(43, 27, 61);
                    rows[i].ActionBtn.Enabled = true;
                }
                else
                {
                    bool isFull = room.CurrentPlayers >= room.MaxPlayers;
                    rows[i].ActionBtn.Text = isFull ? "Full" : "Join";
                    rows[i].ActionBtn.BackColor = isFull ? Color.Gray : Color.FromArgb(155, 17, 24);
                    rows[i].ActionBtn.ForeColor = Color.White;
                    rows[i].ActionBtn.Enabled = !isFull;
                }
            }

            cardRooms.Invalidate();
        }

        private void VeChiTietCardRacer(object? sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using Pen dividerPen = new Pen(Color.FromArgb(222, 215, 204), 1.5f);

            int[] bottoms = { lblFullNameValue.Bottom, lblUsernameValue.Bottom, lblEmailValue.Bottom, lblBirthdayValue.Bottom };
            foreach (int b in bottoms)
            {
                e.Graphics.DrawLine(dividerPen, 22, b + 6, cardRacer.Width - 22, b + 6);
            }
        }

        private void VeChiTietCardRooms(object? sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using Pen dashPen = new Pen(Color.FromArgb(205, 196, 184), 1.5f)
            {
                DashStyle = DashStyle.Dash
            };

            Label[] infos = { lblRoom1Info, lblRoom2Info, lblRoom3Info };
            for (int i = 0; i < infos.Length; i++)
            {
                if (!infos[i].Visible) continue;
                int lineY = infos[i].Bottom + 11;
                e.Graphics.DrawLine(dashPen, 22, lineY, cardRooms.Width - 22, lineY);
            }
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
            if (cardRacer == null || cardMenu == null || cardRooms == null || WindowState == FormWindowState.Minimized)
                return;

            SuspendLayout();

            int topOffset = pnlTitleBar.Height + pnlHeaderBar.Height;
            int usableHeight = Math.Max(0, ClientSize.Height - topOffset);

            int cardHeight = Math.Min(540, Math.Max(510, usableHeight - 40));
            int cardsTotalWidth = cardRacer.Width + cardMenu.Width + cardRooms.Width;
            int gap = Math.Max(20, Math.Min(48, (ClientSize.Width - cardsTotalWidth) / 5));
            int totalWidth = cardsTotalWidth + gap * 2;

            int startX = Math.Max(16, (ClientSize.Width - totalWidth) / 2);
            int y = topOffset + Math.Max(14, (usableHeight - cardHeight) / 2);

            DatViTriVaBong(cardRacer, shadowRacer, startX, y, cardRacer.Width, cardHeight);
            DatViTriVaBong(cardMenu, shadowMenu, startX + cardRacer.Width + gap, y, cardMenu.Width, cardHeight);
            DatViTriVaBong(cardRooms, shadowRooms, startX + cardRacer.Width + gap + cardMenu.Width + gap, y, cardRooms.Width, cardHeight);

            CanGiuaTieuDeCard(cardRacer, lblRacerTitle, lblRacerSub);
            CanGiuaTieuDeCard(cardMenu, lblMenuTitle, lblMenuSub);
            CanGiuaTieuDeCard(cardRooms, lblRoomsTitle, null);

            btnCreateRoom.Location = new Point(22, cardHeight - btnCreateRoom.Height - 24);

            ResumeLayout(true);
        }

        private static void DatViTriVaBong(Control card, Control shadow, int x, int y, int width, int height)
        {
            card.Size = new Size(width, height);
            card.Location = new Point(x, y);
            shadow.Size = new Size(width, height);
            shadow.Location = new Point(x + 6, y + 6);
            shadow.SendToBack();
            card.BringToFront();

            BoGocControl(card, 16);
            BoGocControl(shadow, 16);
        }

        private static void CanGiuaTieuDeCard(Control card, Label title, Label? subtitle)
        {
            title.Location = new Point(Math.Max(0, (card.Width - title.Width) / 2), 12);
            if (subtitle != null)
            {
                subtitle.Location = new Point(Math.Max(0, (card.Width - subtitle.Width) / 2), title.Bottom + 1);
            }
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

        private void DongKetNoiTcp()
        {
            if (_netClient != null)
            {
                _netClient.ConnectionStatusChanged -= OnConnectionStatusChanged;
            }
            SetConnectionStatus("Disconnected", Color.FromArgb(245, 208, 120));
        }

        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            DongKetNoiTcp();
        }

        private void btnSinglePlayer_Click(object? sender, EventArgs e)
        {
            MessageBox.Show("Single Player mode is not implemented yet.", "Info",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnMultiplayer_Click(object? sender, EventArgs e)
        {
            MessageBox.Show("Multiplayer lobby is not implemented yet.", "Info",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnLeaderboard_Click(object? sender, EventArgs e)
        {
            MessageBox.Show("Leaderboard is not implemented yet.", "Info",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private async void btnLogout_Click(object? sender, EventArgs e)
        {
            if (_isLoggingOut) return;
            _isLoggingOut = true;
            btnLogout.Enabled = false;

            if (_netClient != null)
            {
                try
                {
                    await _netClient.LogoutAsync();
                }
                catch
                {
                }
            }

            DongKetNoiTcp();
            if (!IsDisposed)
                Close();
        }

        private void XuLyChonPhong(int index, bool isWatch)
        {
            if (index < 0 || index >= _rooms.Count) return;
            RoomItem room = _rooms[index];
            string action = isWatch ? "Spectating" : "Joining";
            MessageBox.Show($"{action} {room.Title} ({room.StatusText})...", room.Title,
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnJoinRoom1_Click(object? sender, EventArgs e) => XuLyChonPhong(0, isWatch: false);
        private void btnSpectateRoom2_Click(object? sender, EventArgs e) => XuLyChonPhong(1, isWatch: true);
        private void btnJoinRoom3_Click(object? sender, EventArgs e) => XuLyChonPhong(2, isWatch: false);

        private void btnCreateRoom_Click(object? sender, EventArgs e)
        {
            MessageBox.Show("Create Room dialog is not implemented yet.", "Info",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnExit_Click(object? sender, EventArgs e)
        {
            if (_isLoggingOut) return;
            _isLoggingOut = true;
            DongKetNoiTcp();
            Close();
        }

        private void btnMaximize_Click(object? sender, EventArgs e)
        {
            WindowState = WindowState == FormWindowState.Normal
                ? FormWindowState.Maximized
                : FormWindowState.Normal;
            BeginInvoke(new Action(CanGiuaBoCuc));
        }

        private void btnMinimize_Click(object? sender, EventArgs e) => WindowState = FormWindowState.Minimized;
    }
}