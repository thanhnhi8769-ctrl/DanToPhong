using System;
using System.Drawing;
using System.Windows.Forms;

namespace UI
{
    partial class Main
    {
        private System.ComponentModel.IContainer? components = null;

        private Panel pnlTitleBar = null!;
        private Label lblTitleBar = null!;
        private Button btnMinimize = null!;
        private Button btnMaximize = null!;
        private Button btnExit = null!;

        private Panel pnlHeaderBar = null!;
        private Label lblHeaderBrand = null!;
        private PictureBox picHeaderAvatar = null!;
        private Label lblHeaderUser = null!;
        private Label lblConnectionStatus = null!;

        private Panel pnlSidebar = null!;
        private Button btnTabMenu = null!;
        private Button btnTabRooms = null!;
        private Button btnTabRacer = null!;
        private Button btnTabSettings = null!;
        private Button btnTabReview = null!;
        private Button btnLogout = null!;

        private Panel pnlContentHost = null!;
        private Panel shadowContent = null!;

        private Panel cardMenu = null!;
        private Label lblMenuTitle = null!;
        private Label lblMenuSub = null!;
        private Button btnHelpMenu = null!;
        private Button btnSinglePlayer = null!;
        private Button btnMultiplayer = null!;
        private Button btnLeaderboard = null!;

        private Panel cardRooms = null!;
        private Label lblRoomsTitle = null!;
        private Label lblRoomsSub = null!;
        private Button btnHelpRooms = null!;
        private Label lblRoom1Title = null!;
        private Label lblRoom1Info = null!;
        private Button btnJoinRoom1 = null!;
        private Label lblRoom2Title = null!;
        private Label lblRoom2Info = null!;
        private Button btnSpectateRoom2 = null!;
        private Label lblRoom3Title = null!;
        private Label lblRoom3Info = null!;
        private Button btnJoinRoom3 = null!;
        private Button btnCreateRoom = null!;

        private Panel cardRacer = null!;
        private Label lblRacerTitle = null!;
        private Label lblRacerSub = null!;
        private Button btnHelpRacer = null!;
        private PictureBox picRacerAvatar = null!;
        private Label lblFullNameCaption = null!;
        private Label lblFullNameValue = null!;
        private Label lblUsernameCaption = null!;
        private Label lblUsernameValue = null!;
        private Label lblEmailCaption = null!;
        private Label lblEmailValue = null!;
        private Label lblBirthdayCaption = null!;
        private Label lblBirthdayValue = null!;
        private Label lblCreatedAtCaption = null!;
        private Label lblCreatedAtValue = null!;
        private Label lblLastLoginCaption = null!;
        private Label lblLastLoginValue = null!;
        private Label lblCoinsCaption = null!;
        private Label lblCoinsValue = null!;

        private Panel cardSettings = null!;
        private Label lblSettingsTitle = null!;
        private Label lblSettingsSub = null!;
        private Button btnHelpSettings = null!;
        private Label lblSettingsAuthBadge = null!;
        private Label lblSettingsOldPass = null!;
        private TextBox txtSettingsOldPass = null!;
        private Button btnToggleSettingsOldPass = null!;
        private Label lblSettingsUpdateBadge = null!;
        private Label lblSettingsNewPass = null!;
        private TextBox txtSettingsNewPass = null!;
        private Button btnToggleSettingsNewPass = null!;
        private Label lblSettingsConfirmPass = null!;
        private TextBox txtSettingsConfirmPass = null!;
        private Button btnToggleSettingsConfirmPass = null!;
        private Label lblSettingsEmail = null!;
        private TextBox txtSettingsEmail = null!;
        private Label lblSettingsStatus = null!;
        private Button btnSaveSettings = null!;

        private Panel cardReview = null!;
        private Label lblReviewTitle = null!;
        private Label lblReviewSub = null!;
        private Button btnHelpReview = null!;
        private Label lblReviewAvgScore = null!;
        private Label lblReviewAvgStars = null!;
        private Label lblReviewTotalCount = null!;
        private Label lblStar5 = null!;
        private Panel pnlBar5 = null!;
        private Panel pnlFill5 = null!;
        private Label lblStar4 = null!;
        private Panel pnlBar4 = null!;
        private Panel pnlFill4 = null!;
        private Label lblStar3 = null!;
        private Panel pnlBar3 = null!;
        private Panel pnlFill3 = null!;
        private Label lblStar2 = null!;
        private Panel pnlBar2 = null!;
        private Panel pnlFill2 = null!;
        private Label lblStar1 = null!;
        private Panel pnlBar1 = null!;
        private Panel pnlFill1 = null!;
        private Label lblPickStarCaption = null!;
        private Button btnPickStar1 = null!;
        private Button btnPickStar2 = null!;
        private Button btnPickStar3 = null!;
        private Button btnPickStar4 = null!;
        private Button btnPickStar5 = null!;
        private Label lblReviewCommentCaption = null!;
        private TextBox txtReviewComment = null!;
        private Label lblReviewPlaceholder = null!;
        private Button btnSubmitReview = null!;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            pnlTitleBar = new Panel();
            lblTitleBar = new Label();
            btnMinimize = new Button();
            btnMaximize = new Button();
            btnExit = new Button();

            pnlHeaderBar = new Panel();
            lblHeaderBrand = new Label();
            picHeaderAvatar = new PictureBox();
            lblHeaderUser = new Label();
            lblConnectionStatus = new Label();

            pnlSidebar = new Panel();
            btnTabMenu = new Button();
            btnTabRooms = new Button();
            btnTabRacer = new Button();
            btnTabSettings = new Button();
            btnTabReview = new Button();
            btnLogout = new Button();

            pnlContentHost = new Panel();
            shadowContent = new Panel();

            cardMenu = new Panel();
            lblMenuTitle = new Label();
            lblMenuSub = new Label();
            btnHelpMenu = new Button();
            btnSinglePlayer = new Button();
            btnMultiplayer = new Button();
            btnLeaderboard = new Button();

            cardRooms = new Panel();
            lblRoomsTitle = new Label();
            lblRoomsSub = new Label();
            btnHelpRooms = new Button();
            lblRoom1Title = new Label();
            lblRoom1Info = new Label();
            btnJoinRoom1 = new Button();
            lblRoom2Title = new Label();
            lblRoom2Info = new Label();
            btnSpectateRoom2 = new Button();
            lblRoom3Title = new Label();
            lblRoom3Info = new Label();
            btnJoinRoom3 = new Button();
            btnCreateRoom = new Button();

            cardRacer = new Panel();
            lblRacerTitle = new Label();
            lblRacerSub = new Label();
            btnHelpRacer = new Button();
            picRacerAvatar = new PictureBox();
            lblFullNameCaption = new Label();
            lblFullNameValue = new Label();
            lblUsernameCaption = new Label();
            lblUsernameValue = new Label();
            lblEmailCaption = new Label();
            lblEmailValue = new Label();
            lblBirthdayCaption = new Label();
            lblBirthdayValue = new Label();
            lblCreatedAtCaption = new Label();
            lblCreatedAtValue = new Label();
            lblLastLoginCaption = new Label();
            lblLastLoginValue = new Label();
            lblCoinsCaption = new Label();
            lblCoinsValue = new Label();

            cardSettings = new Panel();
            lblSettingsTitle = new Label();
            lblSettingsSub = new Label();
            btnHelpSettings = new Button();
            lblSettingsAuthBadge = new Label();
            lblSettingsOldPass = new Label();
            txtSettingsOldPass = new TextBox();
            btnToggleSettingsOldPass = new Button();
            lblSettingsUpdateBadge = new Label();
            lblSettingsNewPass = new Label();
            txtSettingsNewPass = new TextBox();
            btnToggleSettingsNewPass = new Button();
            lblSettingsConfirmPass = new Label();
            txtSettingsConfirmPass = new TextBox();
            btnToggleSettingsConfirmPass = new Button();
            lblSettingsEmail = new Label();
            txtSettingsEmail = new TextBox();
            lblSettingsStatus = new Label();
            btnSaveSettings = new Button();

            cardReview = new Panel();
            lblReviewTitle = new Label();
            lblReviewSub = new Label();
            btnHelpReview = new Button();
            lblReviewAvgScore = new Label();
            lblReviewAvgStars = new Label();
            lblReviewTotalCount = new Label();
            lblStar5 = new Label();
            pnlBar5 = new Panel();
            pnlFill5 = new Panel();
            lblStar4 = new Label();
            pnlBar4 = new Panel();
            pnlFill4 = new Panel();
            lblStar3 = new Label();
            pnlBar3 = new Panel();
            pnlFill3 = new Panel();
            lblStar2 = new Label();
            pnlBar2 = new Panel();
            pnlFill2 = new Panel();
            lblStar1 = new Label();
            pnlBar1 = new Panel();
            pnlFill1 = new Panel();
            lblPickStarCaption = new Label();
            btnPickStar1 = new Button();
            btnPickStar2 = new Button();
            btnPickStar3 = new Button();
            btnPickStar4 = new Button();
            btnPickStar5 = new Button();
            lblReviewCommentCaption = new Label();
            txtReviewComment = new TextBox();
            lblReviewPlaceholder = new Label();
            btnSubmitReview = new Button();

            pnlTitleBar.SuspendLayout();
            pnlHeaderBar.SuspendLayout();
            pnlSidebar.SuspendLayout();
            pnlContentHost.SuspendLayout();
            cardMenu.SuspendLayout();
            cardRooms.SuspendLayout();
            cardRacer.SuspendLayout();
            cardSettings.SuspendLayout();
            cardReview.SuspendLayout();
            pnlBar5.SuspendLayout();
            pnlBar4.SuspendLayout();
            pnlBar3.SuspendLayout();
            pnlBar2.SuspendLayout();
            pnlBar1.SuspendLayout();
            SuspendLayout();

            // pnlTitleBar
            pnlTitleBar.BackColor = Color.FromArgb(244, 240, 232);
            pnlTitleBar.Controls.Add(lblTitleBar);
            pnlTitleBar.Controls.Add(btnMinimize);
            pnlTitleBar.Controls.Add(btnMaximize);
            pnlTitleBar.Controls.Add(btnExit);
            pnlTitleBar.Dock = DockStyle.Top;
            pnlTitleBar.Height = 38;
            pnlTitleBar.MouseDown += pnlTitleBar_MouseDown;

            lblTitleBar.AutoSize = false;
            lblTitleBar.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            lblTitleBar.ForeColor = Color.FromArgb(92, 24, 154);
            lblTitleBar.Location = new Point(16, 0);
            lblTitleBar.Size = new Size(260, 38);
            lblTitleBar.Text = "Scooter Racing";
            lblTitleBar.TextAlign = ContentAlignment.MiddleLeft;
            lblTitleBar.MouseDown += pnlTitleBar_MouseDown;

            btnMinimize.BackColor = Color.White;
            btnMinimize.FlatStyle = FlatStyle.Flat;
            btnMinimize.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnMinimize.ForeColor = Color.FromArgb(43, 27, 61);
            btnMinimize.Size = new Size(40, 28);
            btnMinimize.Text = "—";
            btnMinimize.UseVisualStyleBackColor = false;
            btnMinimize.Click += btnMinimize_Click;

            btnMaximize.BackColor = Color.White;
            btnMaximize.FlatStyle = FlatStyle.Flat;
            btnMaximize.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            btnMaximize.ForeColor = Color.FromArgb(43, 27, 61);
            btnMaximize.Size = new Size(40, 28);
            btnMaximize.Text = "▢";
            btnMaximize.UseVisualStyleBackColor = false;
            btnMaximize.Click += btnMaximize_Click;

            btnExit.BackColor = Color.White;
            btnExit.FlatStyle = FlatStyle.Flat;
            btnExit.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnExit.ForeColor = Color.FromArgb(43, 27, 61);
            btnExit.Size = new Size(40, 28);
            btnExit.Text = "✕";
            btnExit.UseVisualStyleBackColor = false;
            btnExit.Click += btnExit_Click;

            // pnlHeaderBar
            pnlHeaderBar.BackColor = Color.FromArgb(43, 27, 61);
            pnlHeaderBar.Controls.Add(lblHeaderBrand);
            pnlHeaderBar.Controls.Add(picHeaderAvatar);
            pnlHeaderBar.Controls.Add(lblHeaderUser);
            pnlHeaderBar.Controls.Add(lblConnectionStatus);
            pnlHeaderBar.Dock = DockStyle.Top;
            pnlHeaderBar.Height = 52;

            lblHeaderBrand.AutoSize = false;
            lblHeaderBrand.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            lblHeaderBrand.ForeColor = Color.White;
            lblHeaderBrand.Location = new Point(26, 0);
            lblHeaderBrand.Size = new Size(300, 52);
            lblHeaderBrand.Text = "SCOOTER RACING";
            lblHeaderBrand.TextAlign = ContentAlignment.MiddleLeft;

            picHeaderAvatar.BackColor = Color.FromArgb(43, 27, 61);
            picHeaderAvatar.Size = new Size(34, 34);
            picHeaderAvatar.SizeMode = PictureBoxSizeMode.Zoom;

            lblHeaderUser.AutoSize = false;
            lblHeaderUser.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
            lblHeaderUser.ForeColor = Color.FromArgb(245, 208, 120);
            lblHeaderUser.Size = new Size(240, 30);
            lblHeaderUser.Text = "player1  |  Coins: 1250";
            lblHeaderUser.TextAlign = ContentAlignment.MiddleRight;

            lblConnectionStatus.AutoSize = true;
            lblConnectionStatus.Location = new Point(340, 16);
            lblConnectionStatus.Visible = false;

            // pnlSidebar
            pnlSidebar.BackColor = Color.FromArgb(43, 27, 61);
            pnlSidebar.Controls.Add(btnTabMenu);
            pnlSidebar.Controls.Add(btnTabRooms);
            pnlSidebar.Controls.Add(btnTabRacer);
            pnlSidebar.Controls.Add(btnTabSettings);
            pnlSidebar.Controls.Add(btnTabReview);
            pnlSidebar.Controls.Add(btnLogout);
            pnlSidebar.Dock = DockStyle.Left;
            pnlSidebar.Width = 224;

            btnTabMenu.Cursor = Cursors.Hand;
            btnTabMenu.FlatStyle = FlatStyle.Flat;
            btnTabMenu.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
            btnTabMenu.Size = new Size(184, 44);
            btnTabMenu.Text = "Game";
            btnTabMenu.UseVisualStyleBackColor = false;
            btnTabMenu.Click += btnTabMenu_Click;

            btnTabRooms.Cursor = Cursors.Hand;
            btnTabRooms.FlatStyle = FlatStyle.Flat;
            btnTabRooms.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
            btnTabRooms.Size = new Size(184, 44);
            btnTabRooms.Text = "Open";
            btnTabRooms.UseVisualStyleBackColor = false;
            btnTabRooms.Click += btnTabRooms_Click;

            btnTabRacer.Cursor = Cursors.Hand;
            btnTabRacer.FlatStyle = FlatStyle.Flat;
            btnTabRacer.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
            btnTabRacer.Size = new Size(184, 44);
            btnTabRacer.Text = "Racer";
            btnTabRacer.UseVisualStyleBackColor = false;
            btnTabRacer.Click += btnTabRacer_Click;

            btnTabSettings.Cursor = Cursors.Hand;
            btnTabSettings.FlatStyle = FlatStyle.Flat;
            btnTabSettings.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
            btnTabSettings.Size = new Size(184, 44);
            btnTabSettings.Text = "Setting";
            btnTabSettings.UseVisualStyleBackColor = false;

            btnTabReview.Cursor = Cursors.Hand;
            btnTabReview.FlatStyle = FlatStyle.Flat;
            btnTabReview.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
            btnTabReview.Size = new Size(184, 44);
            btnTabReview.Text = "Review";
            btnTabReview.UseVisualStyleBackColor = false;
            btnTabReview.Click += btnTabReview_Click;

            btnLogout.BackColor = Color.FromArgb(155, 17, 24);
            btnLogout.Cursor = Cursors.Hand;
            btnLogout.FlatStyle = FlatStyle.Flat;
            btnLogout.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
            btnLogout.ForeColor = Color.White;
            btnLogout.Size = new Size(184, 48);
            btnLogout.Text = "Logout";
            btnLogout.UseVisualStyleBackColor = false;
            btnLogout.Click += btnLogout_Click;

            // pnlContentHost
            pnlContentHost.BackColor = Color.FromArgb(238, 226, 198);
            pnlContentHost.Controls.Add(shadowContent);
            pnlContentHost.Controls.Add(cardMenu);
            pnlContentHost.Controls.Add(cardRooms);
            pnlContentHost.Controls.Add(cardRacer);
            pnlContentHost.Controls.Add(cardSettings);
            pnlContentHost.Controls.Add(cardReview);
            pnlContentHost.Dock = DockStyle.Fill;

            shadowContent.BackColor = Color.FromArgb(120, 43, 27, 61);

            // cardMenu
            cardMenu.BackColor = Color.FromArgb(253, 251, 247);
            cardMenu.Controls.Add(lblMenuTitle);
            cardMenu.Controls.Add(lblMenuSub);
            cardMenu.Controls.Add(btnHelpMenu);
            cardMenu.Controls.Add(btnSinglePlayer);
            cardMenu.Controls.Add(btnMultiplayer);
            cardMenu.Controls.Add(btnLeaderboard);
            cardMenu.Location = new Point(48, 20);
            cardMenu.Size = new Size(720, 610);

            lblMenuTitle.Font = new Font("Segoe UI", 19F, FontStyle.Bold);
            lblMenuTitle.ForeColor = Color.FromArgb(43, 27, 61);
            lblMenuTitle.Text = "GAME MODE";

            lblMenuSub.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            lblMenuSub.ForeColor = Color.FromArgb(110, 102, 118);
            lblMenuSub.Text = "Choose how you want to race";

            btnHelpMenu.BackColor = Color.FromArgb(253, 251, 247);
            btnHelpMenu.Cursor = Cursors.Hand;
            btnHelpMenu.FlatStyle = FlatStyle.Flat;
            btnHelpMenu.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnHelpMenu.ForeColor = Color.FromArgb(43, 27, 61);
            btnHelpMenu.Size = new Size(32, 32);
            btnHelpMenu.Text = "?";
            btnHelpMenu.UseVisualStyleBackColor = false;
            btnHelpMenu.Click += btnHelp_Click;

            btnSinglePlayer.BackColor = Color.FromArgb(155, 17, 24);
            btnSinglePlayer.Cursor = Cursors.Hand;
            btnSinglePlayer.FlatStyle = FlatStyle.Flat;
            btnSinglePlayer.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            btnSinglePlayer.ForeColor = Color.White;
            btnSinglePlayer.Text = "Single Player";
            btnSinglePlayer.UseVisualStyleBackColor = false;
            btnSinglePlayer.Click += btnSinglePlayer_Click;

            btnMultiplayer.BackColor = Color.FromArgb(43, 27, 61);
            btnMultiplayer.Cursor = Cursors.Hand;
            btnMultiplayer.FlatStyle = FlatStyle.Flat;
            btnMultiplayer.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            btnMultiplayer.ForeColor = Color.White;
            btnMultiplayer.Text = "Multiplayer";
            btnMultiplayer.UseVisualStyleBackColor = false;
            btnMultiplayer.Click += btnMultiplayer_Click;

            btnLeaderboard.BackColor = Color.FromArgb(72, 48, 98);
            btnLeaderboard.Cursor = Cursors.Hand;
            btnLeaderboard.FlatStyle = FlatStyle.Flat;
            btnLeaderboard.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            btnLeaderboard.ForeColor = Color.White;
            btnLeaderboard.Text = "Leaderboard";
            btnLeaderboard.UseVisualStyleBackColor = false;
            btnLeaderboard.Click += btnLeaderboard_Click;

            // cardRooms
            cardRooms.BackColor = Color.FromArgb(253, 251, 247);
            cardRooms.Controls.Add(lblRoomsTitle);
            cardRooms.Controls.Add(lblRoomsSub);
            cardRooms.Controls.Add(btnHelpRooms);
            cardRooms.Controls.Add(lblRoom1Title);
            cardRooms.Controls.Add(lblRoom1Info);
            cardRooms.Controls.Add(btnJoinRoom1);
            cardRooms.Controls.Add(lblRoom2Title);
            cardRooms.Controls.Add(lblRoom2Info);
            cardRooms.Controls.Add(btnSpectateRoom2);
            cardRooms.Controls.Add(lblRoom3Title);
            cardRooms.Controls.Add(lblRoom3Info);
            cardRooms.Controls.Add(btnJoinRoom3);
            cardRooms.Controls.Add(btnCreateRoom);
            cardRooms.Location = new Point(48, 20);
            cardRooms.Size = new Size(720, 610);

            lblRoomsTitle.Font = new Font("Segoe UI", 19F, FontStyle.Bold);
            lblRoomsTitle.ForeColor = Color.FromArgb(43, 27, 61);
            lblRoomsTitle.Text = "OPEN ROOMS";

            lblRoomsSub.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            lblRoomsSub.ForeColor = Color.FromArgb(110, 102, 118);
            lblRoomsSub.Text = "Join an open lobby or spectate a live race";

            btnHelpRooms.BackColor = Color.FromArgb(253, 251, 247);
            btnHelpRooms.Cursor = Cursors.Hand;
            btnHelpRooms.FlatStyle = FlatStyle.Flat;
            btnHelpRooms.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnHelpRooms.ForeColor = Color.FromArgb(43, 27, 61);
            btnHelpRooms.Size = new Size(32, 32);
            btnHelpRooms.Text = "?";
            btnHelpRooms.UseVisualStyleBackColor = false;
            btnHelpRooms.Click += btnHelp_Click;

            lblRoom1Title.Font = new Font("Segoe UI", 11.5F, FontStyle.Bold);
            lblRoom1Title.ForeColor = Color.FromArgb(43, 27, 61);
            lblRoom1Info.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            lblRoom1Info.ForeColor = Color.FromArgb(110, 102, 118);
            btnJoinRoom1.Cursor = Cursors.Hand;
            btnJoinRoom1.FlatStyle = FlatStyle.Flat;
            btnJoinRoom1.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnJoinRoom1.UseVisualStyleBackColor = false;
            btnJoinRoom1.Click += btnJoinRoom1_Click;

            lblRoom2Title.Font = new Font("Segoe UI", 11.5F, FontStyle.Bold);
            lblRoom2Title.ForeColor = Color.FromArgb(43, 27, 61);
            lblRoom2Info.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            lblRoom2Info.ForeColor = Color.FromArgb(110, 102, 118);
            btnSpectateRoom2.Cursor = Cursors.Hand;
            btnSpectateRoom2.FlatStyle = FlatStyle.Flat;
            btnSpectateRoom2.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnSpectateRoom2.UseVisualStyleBackColor = false;
            btnSpectateRoom2.Click += btnSpectateRoom2_Click;

            lblRoom3Title.Font = new Font("Segoe UI", 11.5F, FontStyle.Bold);
            lblRoom3Title.ForeColor = Color.FromArgb(43, 27, 61);
            lblRoom3Info.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            lblRoom3Info.ForeColor = Color.FromArgb(110, 102, 118);
            btnJoinRoom3.Cursor = Cursors.Hand;
            btnJoinRoom3.FlatStyle = FlatStyle.Flat;
            btnJoinRoom3.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnJoinRoom3.UseVisualStyleBackColor = false;
            btnJoinRoom3.Click += btnJoinRoom3_Click;

            btnCreateRoom.BackColor = Color.FromArgb(155, 17, 24);
            btnCreateRoom.Cursor = Cursors.Hand;
            btnCreateRoom.FlatStyle = FlatStyle.Flat;
            btnCreateRoom.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            btnCreateRoom.ForeColor = Color.White;
            btnCreateRoom.Text = "Create Room";
            btnCreateRoom.UseVisualStyleBackColor = false;
            btnCreateRoom.Click += btnCreateRoom_Click;

            // cardRacer
            cardRacer.BackColor = Color.FromArgb(253, 251, 247);
            cardRacer.Controls.Add(lblRacerTitle);
            cardRacer.Controls.Add(lblRacerSub);
            cardRacer.Controls.Add(btnHelpRacer);
            cardRacer.Controls.Add(picRacerAvatar);
            cardRacer.Controls.Add(lblFullNameCaption);
            cardRacer.Controls.Add(lblFullNameValue);
            cardRacer.Controls.Add(lblUsernameCaption);
            cardRacer.Controls.Add(lblUsernameValue);
            cardRacer.Controls.Add(lblEmailCaption);
            cardRacer.Controls.Add(lblEmailValue);
            cardRacer.Controls.Add(lblBirthdayCaption);
            cardRacer.Controls.Add(lblBirthdayValue);
            cardRacer.Controls.Add(lblCreatedAtCaption);
            cardRacer.Controls.Add(lblCreatedAtValue);
            cardRacer.Controls.Add(lblLastLoginCaption);
            cardRacer.Controls.Add(lblLastLoginValue);
            cardRacer.Controls.Add(lblCoinsCaption);
            cardRacer.Controls.Add(lblCoinsValue);
            cardRacer.Location = new Point(48, 20);
            cardRacer.Size = new Size(720, 610);

            lblRacerTitle.Font = new Font("Segoe UI", 19F, FontStyle.Bold);
            lblRacerTitle.ForeColor = Color.FromArgb(43, 27, 61);
            lblRacerTitle.Text = "RACER INFO";

            lblRacerSub.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            lblRacerSub.ForeColor = Color.FromArgb(110, 102, 118);
            lblRacerSub.Text = "Profile details and account activity";

            btnHelpRacer.BackColor = Color.FromArgb(253, 251, 247);
            btnHelpRacer.Cursor = Cursors.Hand;
            btnHelpRacer.FlatStyle = FlatStyle.Flat;
            btnHelpRacer.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnHelpRacer.ForeColor = Color.FromArgb(43, 27, 61);
            btnHelpRacer.Size = new Size(32, 32);
            btnHelpRacer.Text = "?";
            btnHelpRacer.UseVisualStyleBackColor = false;
            btnHelpRacer.Click += btnHelp_Click;

            picRacerAvatar.BackColor = Color.FromArgb(253, 251, 247);
            picRacerAvatar.Size = new Size(76, 76);
            picRacerAvatar.SizeMode = PictureBoxSizeMode.Zoom;

            lblFullNameCaption.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblFullNameCaption.ForeColor = Color.FromArgb(110, 102, 118);
            lblFullNameCaption.Text = "Full Name";

            lblFullNameValue.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
            lblFullNameValue.ForeColor = Color.FromArgb(43, 27, 61);
            lblFullNameValue.Text = "--";

            lblUsernameCaption.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblUsernameCaption.ForeColor = Color.FromArgb(110, 102, 118);
            lblUsernameCaption.Text = "Username";

            lblUsernameValue.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
            lblUsernameValue.ForeColor = Color.FromArgb(43, 27, 61);
            lblUsernameValue.Text = "--";

            lblEmailCaption.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblEmailCaption.ForeColor = Color.FromArgb(110, 102, 118);
            lblEmailCaption.Text = "Email";

            lblEmailValue.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
            lblEmailValue.ForeColor = Color.FromArgb(43, 27, 61);
            lblEmailValue.Text = "Not set";

            lblBirthdayCaption.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblBirthdayCaption.ForeColor = Color.FromArgb(110, 102, 118);
            lblBirthdayCaption.Text = "Birthday";

            lblBirthdayValue.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
            lblBirthdayValue.ForeColor = Color.FromArgb(43, 27, 61);
            lblBirthdayValue.Text = "Not set";

            lblCreatedAtCaption.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblCreatedAtCaption.ForeColor = Color.FromArgb(110, 102, 118);
            lblCreatedAtCaption.Text = "Created At";

            lblCreatedAtValue.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
            lblCreatedAtValue.ForeColor = Color.FromArgb(43, 27, 61);
            lblCreatedAtValue.Text = "--";

            lblLastLoginCaption.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblLastLoginCaption.ForeColor = Color.FromArgb(110, 102, 118);
            lblLastLoginCaption.Text = "Last Login";

            lblLastLoginValue.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
            lblLastLoginValue.ForeColor = Color.FromArgb(43, 27, 61);
            lblLastLoginValue.Text = "--";

            lblCoinsCaption.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblCoinsCaption.ForeColor = Color.FromArgb(110, 102, 118);
            lblCoinsCaption.Text = "Coins";

            lblCoinsValue.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            lblCoinsValue.ForeColor = Color.FromArgb(155, 17, 24);
            lblCoinsValue.Text = "1250";

            // cardSettings
            cardSettings.BackColor = Color.FromArgb(253, 251, 247);
            cardSettings.Controls.Add(lblSettingsTitle);
            cardSettings.Controls.Add(lblSettingsSub);
            cardSettings.Controls.Add(btnHelpSettings);
            cardSettings.Controls.Add(lblSettingsAuthBadge);
            cardSettings.Controls.Add(lblSettingsOldPass);
            cardSettings.Controls.Add(txtSettingsOldPass);
            cardSettings.Controls.Add(btnToggleSettingsOldPass);
            cardSettings.Controls.Add(lblSettingsUpdateBadge);
            cardSettings.Controls.Add(lblSettingsNewPass);
            cardSettings.Controls.Add(txtSettingsNewPass);
            cardSettings.Controls.Add(btnToggleSettingsNewPass);
            cardSettings.Controls.Add(lblSettingsConfirmPass);
            cardSettings.Controls.Add(txtSettingsConfirmPass);
            cardSettings.Controls.Add(btnToggleSettingsConfirmPass);
            cardSettings.Controls.Add(lblSettingsEmail);
            cardSettings.Controls.Add(txtSettingsEmail);
            cardSettings.Controls.Add(lblSettingsStatus);
            cardSettings.Controls.Add(btnSaveSettings);
            cardSettings.Location = new Point(48, 20);
            cardSettings.Size = new Size(720, 610);

            lblSettingsTitle.Font = new Font("Segoe UI", 19F, FontStyle.Bold);
            lblSettingsTitle.ForeColor = Color.FromArgb(43, 27, 61);
            lblSettingsTitle.Location = new Point(38, 18);
            lblSettingsTitle.Size = new Size(600, 36);
            lblSettingsTitle.Text = "SETTINGS";

            lblSettingsSub.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            lblSettingsSub.ForeColor = Color.FromArgb(110, 102, 118);
            lblSettingsSub.Location = new Point(38, 58);
            lblSettingsSub.Size = new Size(600, 22);
            lblSettingsSub.Text = "Manage your login password and registered email";

            btnHelpSettings.BackColor = Color.FromArgb(253, 251, 247);
            btnHelpSettings.Cursor = Cursors.Hand;
            btnHelpSettings.FlatStyle = FlatStyle.Flat;
            btnHelpSettings.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnHelpSettings.ForeColor = Color.FromArgb(43, 27, 61);
            btnHelpSettings.Location = new Point(662, 22);
            btnHelpSettings.Size = new Size(32, 32);
            btnHelpSettings.Text = "?";
            btnHelpSettings.UseVisualStyleBackColor = false;
            btnHelpSettings.Click += btnHelp_Click;

            lblSettingsAuthBadge.BackColor = Color.FromArgb(236, 228, 246);
            lblSettingsAuthBadge.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            lblSettingsAuthBadge.ForeColor = Color.FromArgb(68, 36, 108);
            lblSettingsAuthBadge.Location = new Point(64, 110);
            lblSettingsAuthBadge.Size = new Size(132, 22);
            lblSettingsAuthBadge.Text = "VERIFICATION";
            lblSettingsAuthBadge.TextAlign = ContentAlignment.MiddleCenter;

            lblSettingsOldPass.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblSettingsOldPass.ForeColor = Color.FromArgb(43, 27, 61);
            lblSettingsOldPass.Location = new Point(64, 140);
            lblSettingsOldPass.Size = new Size(592, 20);
            lblSettingsOldPass.Text = "Current Password";

            txtSettingsOldPass.BackColor = Color.FromArgb(250, 247, 241);
            txtSettingsOldPass.BorderStyle = BorderStyle.None;
            txtSettingsOldPass.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            txtSettingsOldPass.ForeColor = Color.FromArgb(43, 27, 61);
            txtSettingsOldPass.Location = new Point(64, 174);
            txtSettingsOldPass.PlaceholderText = "Enter current password";
            txtSettingsOldPass.Size = new Size(554, 22);
            txtSettingsOldPass.UseSystemPasswordChar = true;

            btnToggleSettingsOldPass.BackColor = Color.FromArgb(250, 247, 241);
            btnToggleSettingsOldPass.Cursor = Cursors.Hand;
            btnToggleSettingsOldPass.FlatStyle = FlatStyle.Flat;
            btnToggleSettingsOldPass.Font = new Font("Segoe UI Emoji", 8.5F, FontStyle.Regular);
            btnToggleSettingsOldPass.ForeColor = Color.DimGray;
            btnToggleSettingsOldPass.Location = new Point(630, 175);
            btnToggleSettingsOldPass.Padding = Padding.Empty;
            btnToggleSettingsOldPass.Size = new Size(26, 20);
            btnToggleSettingsOldPass.TabStop = false;
            btnToggleSettingsOldPass.Text = "👁";
            btnToggleSettingsOldPass.TextAlign = ContentAlignment.MiddleCenter;
            btnToggleSettingsOldPass.UseVisualStyleBackColor = false;

            lblSettingsUpdateBadge.BackColor = Color.FromArgb(236, 228, 246);
            lblSettingsUpdateBadge.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            lblSettingsUpdateBadge.ForeColor = Color.FromArgb(68, 36, 108);
            lblSettingsUpdateBadge.Location = new Point(64, 264);
            lblSettingsUpdateBadge.Size = new Size(154, 22);
            lblSettingsUpdateBadge.Text = "NEW";
            lblSettingsUpdateBadge.TextAlign = ContentAlignment.MiddleCenter;

            lblSettingsNewPass.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblSettingsNewPass.ForeColor = Color.FromArgb(43, 27, 61);
            lblSettingsNewPass.Location = new Point(64, 294);
            lblSettingsNewPass.Size = new Size(268, 20);
            lblSettingsNewPass.Text = "New Password";

            txtSettingsNewPass.BackColor = Color.FromArgb(250, 247, 241);
            txtSettingsNewPass.BorderStyle = BorderStyle.None;
            txtSettingsNewPass.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            txtSettingsNewPass.ForeColor = Color.FromArgb(43, 27, 61);
            txtSettingsNewPass.Location = new Point(64, 328);
            txtSettingsNewPass.PlaceholderText = "Min 8 chars, A-z & 0-9";
            txtSettingsNewPass.Size = new Size(230, 22);
            txtSettingsNewPass.UseSystemPasswordChar = true;

            btnToggleSettingsNewPass.BackColor = Color.FromArgb(250, 247, 241);
            btnToggleSettingsNewPass.Cursor = Cursors.Hand;
            btnToggleSettingsNewPass.FlatStyle = FlatStyle.Flat;
            btnToggleSettingsNewPass.Font = new Font("Segoe UI Emoji", 8.5F, FontStyle.Regular);
            btnToggleSettingsNewPass.ForeColor = Color.DimGray;
            btnToggleSettingsNewPass.Location = new Point(306, 329);
            btnToggleSettingsNewPass.Padding = Padding.Empty;
            btnToggleSettingsNewPass.Size = new Size(26, 20);
            btnToggleSettingsNewPass.TabStop = false;
            btnToggleSettingsNewPass.Text = "👁";
            btnToggleSettingsNewPass.TextAlign = ContentAlignment.MiddleCenter;
            btnToggleSettingsNewPass.UseVisualStyleBackColor = false;

            lblSettingsConfirmPass.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblSettingsConfirmPass.ForeColor = Color.FromArgb(43, 27, 61);
            lblSettingsConfirmPass.Location = new Point(388, 294);
            lblSettingsConfirmPass.Size = new Size(268, 20);
            lblSettingsConfirmPass.Text = "Confirm Password";

            txtSettingsConfirmPass.BackColor = Color.FromArgb(250, 247, 241);
            txtSettingsConfirmPass.BorderStyle = BorderStyle.None;
            txtSettingsConfirmPass.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            txtSettingsConfirmPass.ForeColor = Color.FromArgb(43, 27, 61);
            txtSettingsConfirmPass.Location = new Point(388, 328);
            txtSettingsConfirmPass.PlaceholderText = "Re-enter password";
            txtSettingsConfirmPass.Size = new Size(230, 22);
            txtSettingsConfirmPass.UseSystemPasswordChar = true;

            btnToggleSettingsConfirmPass.BackColor = Color.FromArgb(250, 247, 241);
            btnToggleSettingsConfirmPass.Cursor = Cursors.Hand;
            btnToggleSettingsConfirmPass.FlatStyle = FlatStyle.Flat;
            btnToggleSettingsConfirmPass.Font = new Font("Segoe UI Emoji", 8.5F, FontStyle.Regular);
            btnToggleSettingsConfirmPass.ForeColor = Color.DimGray;
            btnToggleSettingsConfirmPass.Location = new Point(630, 329);
            btnToggleSettingsConfirmPass.Padding = Padding.Empty;
            btnToggleSettingsConfirmPass.Size = new Size(26, 20);
            btnToggleSettingsConfirmPass.TabStop = false;
            btnToggleSettingsConfirmPass.Text = "👁";
            btnToggleSettingsConfirmPass.TextAlign = ContentAlignment.MiddleCenter;
            btnToggleSettingsConfirmPass.UseVisualStyleBackColor = false;

            lblSettingsEmail.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblSettingsEmail.ForeColor = Color.FromArgb(43, 27, 61);
            lblSettingsEmail.Location = new Point(64, 388);
            lblSettingsEmail.Size = new Size(592, 20);
            lblSettingsEmail.Text = "Email";

            txtSettingsEmail.BackColor = Color.FromArgb(250, 247, 241);
            txtSettingsEmail.BorderStyle = BorderStyle.None;
            txtSettingsEmail.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            txtSettingsEmail.ForeColor = Color.FromArgb(43, 27, 61);
            txtSettingsEmail.Location = new Point(64, 422);
            txtSettingsEmail.PlaceholderText = "name@domain.com";
            txtSettingsEmail.Size = new Size(592, 22);
            txtSettingsEmail.UseSystemPasswordChar = false;

            lblSettingsStatus.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblSettingsStatus.ForeColor = Color.Crimson;
            lblSettingsStatus.Location = new Point(38, 502);
            lblSettingsStatus.Size = new Size(644, 22);
            lblSettingsStatus.Text = string.Empty;
            lblSettingsStatus.TextAlign = ContentAlignment.MiddleCenter;

            btnSaveSettings.BackColor = Color.FromArgb(155, 17, 24);
            btnSaveSettings.Cursor = Cursors.Hand;
            btnSaveSettings.FlatStyle = FlatStyle.Flat;
            btnSaveSettings.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
            btnSaveSettings.ForeColor = Color.White;
            btnSaveSettings.Location = new Point(260, 530);
            btnSaveSettings.Size = new Size(200, 44);
            btnSaveSettings.Text = "Save Changes";
            btnSaveSettings.UseVisualStyleBackColor = false;

            // cardReview
            cardReview.BackColor = Color.FromArgb(253, 251, 247);
            cardReview.Controls.Add(lblReviewTitle);
            cardReview.Controls.Add(lblReviewSub);
            cardReview.Controls.Add(btnHelpReview);
            cardReview.Controls.Add(lblReviewAvgScore);
            cardReview.Controls.Add(lblReviewAvgStars);
            cardReview.Controls.Add(lblReviewTotalCount);
            cardReview.Controls.Add(lblStar5);
            cardReview.Controls.Add(pnlBar5);
            cardReview.Controls.Add(lblStar4);
            cardReview.Controls.Add(pnlBar4);
            cardReview.Controls.Add(lblStar3);
            cardReview.Controls.Add(pnlBar3);
            cardReview.Controls.Add(lblStar2);
            cardReview.Controls.Add(pnlBar2);
            cardReview.Controls.Add(lblStar1);
            cardReview.Controls.Add(pnlBar1);
            cardReview.Controls.Add(lblPickStarCaption);
            cardReview.Controls.Add(btnPickStar1);
            cardReview.Controls.Add(btnPickStar2);
            cardReview.Controls.Add(btnPickStar3);
            cardReview.Controls.Add(btnPickStar4);
            cardReview.Controls.Add(btnPickStar5);
            cardReview.Controls.Add(lblReviewCommentCaption);
            cardReview.Controls.Add(lblReviewPlaceholder);
            cardReview.Controls.Add(txtReviewComment);
            cardReview.Controls.Add(btnSubmitReview);
            cardReview.Location = new Point(48, 20);
            cardReview.Size = new Size(720, 610);

            lblReviewTitle.Font = new Font("Segoe UI", 19F, FontStyle.Bold);
            lblReviewTitle.ForeColor = Color.FromArgb(43, 27, 61);
            lblReviewTitle.Text = "GAME REVIEW";

            lblReviewSub.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            lblReviewSub.ForeColor = Color.FromArgb(110, 102, 118);
            lblReviewSub.Text = "See ratings and leave your feedback";

            btnHelpReview.BackColor = Color.FromArgb(253, 251, 247);
            btnHelpReview.Cursor = Cursors.Hand;
            btnHelpReview.FlatStyle = FlatStyle.Flat;
            btnHelpReview.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnHelpReview.ForeColor = Color.FromArgb(43, 27, 61);
            btnHelpReview.Size = new Size(32, 32);
            btnHelpReview.Text = "?";
            btnHelpReview.UseVisualStyleBackColor = false;
            btnHelpReview.Click += btnHelp_Click;

            lblReviewAvgScore.Font = new Font("Segoe UI", 32F, FontStyle.Bold);
            lblReviewAvgScore.ForeColor = Color.FromArgb(43, 27, 61);
            lblReviewAvgScore.Text = "4.4";
            lblReviewAvgScore.TextAlign = ContentAlignment.MiddleCenter;

            lblReviewAvgStars.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            lblReviewAvgStars.ForeColor = Color.FromArgb(235, 178, 22);
            lblReviewAvgStars.Text = "★★★★☆";
            lblReviewAvgStars.TextAlign = ContentAlignment.MiddleCenter;

            lblReviewTotalCount.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            lblReviewTotalCount.ForeColor = Color.FromArgb(110, 102, 118);
            lblReviewTotalCount.Text = "Total reviews: 16";
            lblReviewTotalCount.TextAlign = ContentAlignment.MiddleCenter;

            lblStar5.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            lblStar5.ForeColor = Color.FromArgb(43, 27, 61);
            lblStar5.Text = "5★";
            lblStar5.TextAlign = ContentAlignment.MiddleLeft;
            pnlBar5.BackColor = Color.FromArgb(232, 226, 216);
            pnlBar5.Controls.Add(pnlFill5);
            pnlFill5.BackColor = Color.FromArgb(235, 178, 22);
            pnlFill5.Dock = DockStyle.Left;
            pnlFill5.Width = 0;

            lblStar4.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            lblStar4.ForeColor = Color.FromArgb(43, 27, 61);
            lblStar4.Text = "4★";
            lblStar4.TextAlign = ContentAlignment.MiddleLeft;
            pnlBar4.BackColor = Color.FromArgb(232, 226, 216);
            pnlBar4.Controls.Add(pnlFill4);
            pnlFill4.BackColor = Color.FromArgb(235, 178, 22);
            pnlFill4.Dock = DockStyle.Left;
            pnlFill4.Width = 0;

            lblStar3.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            lblStar3.ForeColor = Color.FromArgb(43, 27, 61);
            lblStar3.Text = "3★";
            lblStar3.TextAlign = ContentAlignment.MiddleLeft;
            pnlBar3.BackColor = Color.FromArgb(232, 226, 216);
            pnlBar3.Controls.Add(pnlFill3);
            pnlFill3.BackColor = Color.FromArgb(235, 178, 22);
            pnlFill3.Dock = DockStyle.Left;
            pnlFill3.Width = 0;

            lblStar2.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            lblStar2.ForeColor = Color.FromArgb(43, 27, 61);
            lblStar2.Text = "2★";
            lblStar2.TextAlign = ContentAlignment.MiddleLeft;
            pnlBar2.BackColor = Color.FromArgb(232, 226, 216);
            pnlBar2.Controls.Add(pnlFill2);
            pnlFill2.BackColor = Color.FromArgb(235, 178, 22);
            pnlFill2.Dock = DockStyle.Left;
            pnlFill2.Width = 0;

            lblStar1.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            lblStar1.ForeColor = Color.FromArgb(43, 27, 61);
            lblStar1.Text = "1★";
            lblStar1.TextAlign = ContentAlignment.MiddleLeft;
            pnlBar1.BackColor = Color.FromArgb(232, 226, 216);
            pnlBar1.Controls.Add(pnlFill1);
            pnlFill1.BackColor = Color.FromArgb(235, 178, 22);
            pnlFill1.Dock = DockStyle.Left;
            pnlFill1.Width = 0;

            lblPickStarCaption.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblPickStarCaption.ForeColor = Color.FromArgb(43, 27, 61);
            lblPickStarCaption.Text = "Your Rating";

            lblReviewCommentCaption.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblReviewCommentCaption.ForeColor = Color.FromArgb(43, 27, 61);
            lblReviewCommentCaption.Text = "Comment (optional)";

            txtReviewComment.BackColor = Color.FromArgb(250, 247, 241);
            txtReviewComment.BorderStyle = BorderStyle.FixedSingle;
            txtReviewComment.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            txtReviewComment.ForeColor = Color.FromArgb(43, 27, 61);
            txtReviewComment.MaxLength = 300;
            txtReviewComment.Multiline = true;

            lblReviewPlaceholder.BackColor = Color.FromArgb(250, 247, 241);
            lblReviewPlaceholder.Cursor = Cursors.IBeam;
            lblReviewPlaceholder.Font = new Font("Segoe UI", 9.5F, FontStyle.Italic);
            lblReviewPlaceholder.ForeColor = Color.FromArgb(145, 138, 152);
            lblReviewPlaceholder.Text = "Share your thoughts about controls, tracks, or rooms...";

            btnSubmitReview.BackColor = Color.FromArgb(155, 17, 24);
            btnSubmitReview.Cursor = Cursors.Hand;
            btnSubmitReview.FlatStyle = FlatStyle.Flat;
            btnSubmitReview.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
            btnSubmitReview.ForeColor = Color.White;
            btnSubmitReview.Text = "Submit";
            btnSubmitReview.UseVisualStyleBackColor = false;
            btnSubmitReview.Click += btnSubmitReview_Click;

            // Main form
            AutoScaleMode = AutoScaleMode.None;
            BackColor = Color.FromArgb(244, 240, 232);
            ClientSize = new Size(1120, 760);
            Controls.Add(pnlContentHost);
            Controls.Add(pnlSidebar);
            Controls.Add(pnlHeaderBar);
            Controls.Add(pnlTitleBar);
            FormBorderStyle = FormBorderStyle.None;
            MinimumSize = new Size(1020, 700);
            Name = "Main";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Scooter Racing";

            pnlTitleBar.ResumeLayout(false);
            pnlHeaderBar.ResumeLayout(false);
            pnlHeaderBar.PerformLayout();
            pnlSidebar.ResumeLayout(false);
            pnlContentHost.ResumeLayout(false);
            cardMenu.ResumeLayout(false);
            cardRooms.ResumeLayout(false);
            cardRacer.ResumeLayout(false);
            cardSettings.ResumeLayout(false);
            cardSettings.PerformLayout();
            cardReview.ResumeLayout(false);
            cardReview.PerformLayout();
            pnlBar5.ResumeLayout(false);
            pnlBar4.ResumeLayout(false);
            pnlBar3.ResumeLayout(false);
            pnlBar2.ResumeLayout(false);
            pnlBar1.ResumeLayout(false);
            ResumeLayout(false);
        }
    }
}