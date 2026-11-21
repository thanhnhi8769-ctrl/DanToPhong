namespace UI
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            pnlTitleBar = new Panel();
            titellbl = new Label();
            btnMinimize = new Button();
            btnMaximize = new Button();
            btnExit = new Button();
            pnlHeaderBar = new Panel();
            lblHeaderTitle = new Label();
            lblConnectionStatus = new Label();
            lblHeaderUser = new Label();
            cardRacer = new ReaLTaiizor.Controls.NightPanel();
            lblRacerTitle = new Label();
            lblRacerSub = new Label();
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
            shadowRacer = new Panel();
            cardMenu = new ReaLTaiizor.Controls.NightPanel();
            lblMenuTitle = new Label();
            lblMenuSub = new Label();
            btnSinglePlayer = new Button();
            btnMultiplayer = new Button();
            btnLeaderboard = new Button();
            btnLogout = new Button();
            shadowMenu = new Panel();
            cardRooms = new ReaLTaiizor.Controls.NightPanel();
            lblRoomsTitle = new Label();
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
            shadowRooms = new Panel();
            pnlTitleBar.SuspendLayout();
            pnlHeaderBar.SuspendLayout();
            cardRacer.SuspendLayout();
            cardMenu.SuspendLayout();
            cardRooms.SuspendLayout();
            SuspendLayout();
            // 
            // pnlTitleBar
            // 
            pnlTitleBar.BackColor = Color.FromArgb(245, 243, 247);
            pnlTitleBar.Controls.Add(titellbl);
            pnlTitleBar.Controls.Add(btnMinimize);
            pnlTitleBar.Controls.Add(btnMaximize);
            pnlTitleBar.Controls.Add(btnExit);
            pnlTitleBar.Dock = DockStyle.Top;
            pnlTitleBar.Name = "pnlTitleBar";
            pnlTitleBar.Size = new Size(1220, 44);
            pnlTitleBar.TabIndex = 0;
            pnlTitleBar.MouseDown += pnlTitleBar_MouseDown;
            // 
            // titellbl
            // 
            titellbl.AutoSize = true;
            titellbl.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point, 0);
            titellbl.ForeColor = Color.Indigo;
            titellbl.Location = new Point(24, 11);
            titellbl.Name = "titellbl";
            titellbl.Size = new Size(165, 23);
            titellbl.TabIndex = 0;
            titellbl.Text = "Scooter Racing";
            // 
            // btnMinimize
            // 
            btnMinimize.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnMinimize.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnMinimize.ForeColor = Color.Black;
            btnMinimize.Location = new Point(1056, 7);
            btnMinimize.Name = "btnMinimize";
            btnMinimize.Size = new Size(47, 30);
            btnMinimize.TabIndex = 1;
            btnMinimize.Text = "–";
            btnMinimize.UseVisualStyleBackColor = true;
            btnMinimize.Click += btnMinimize_Click;
            // 
            // btnMaximize
            // 
            btnMaximize.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnMaximize.Font = new Font("Segoe UI Black", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnMaximize.ForeColor = Color.Black;
            btnMaximize.Location = new Point(1109, 7);
            btnMaximize.Name = "btnMaximize";
            btnMaximize.Size = new Size(47, 30);
            btnMaximize.TabIndex = 2;
            btnMaximize.Text = "❐";
            btnMaximize.UseVisualStyleBackColor = true;
            btnMaximize.Click += btnMaximize_Click;
            // 
            // btnExit
            // 
            btnExit.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnExit.Font = new Font("Segoe UI Black", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnExit.ForeColor = Color.Black;
            btnExit.Location = new Point(1162, 7);
            btnExit.Name = "btnExit";
            btnExit.Size = new Size(47, 30);
            btnExit.TabIndex = 3;
            btnExit.Text = "X";
            btnExit.UseVisualStyleBackColor = true;
            btnExit.Click += btnExit_Click;
            // 
            // pnlHeaderBar
            // 
            pnlHeaderBar.BackColor = Color.FromArgb(43, 27, 61);
            pnlHeaderBar.Controls.Add(lblHeaderTitle);
            pnlHeaderBar.Controls.Add(lblConnectionStatus);
            pnlHeaderBar.Controls.Add(lblHeaderUser);
            pnlHeaderBar.Dock = DockStyle.Top;
            pnlHeaderBar.Name = "pnlHeaderBar";
            pnlHeaderBar.Size = new Size(1220, 52);
            pnlHeaderBar.TabIndex = 1;
            // 
            // lblHeaderTitle
            // 
            lblHeaderTitle.AutoSize = true;
            lblHeaderTitle.Font = new Font("Segoe UI", 13F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblHeaderTitle.ForeColor = Color.White;
            lblHeaderTitle.Location = new Point(24, 12);
            lblHeaderTitle.Name = "lblHeaderTitle";
            lblHeaderTitle.Size = new Size(209, 30);
            lblHeaderTitle.TabIndex = 0;
            lblHeaderTitle.Text = "SCOOTER RACING";
            // 
            // lblConnectionStatus
            // 
            lblConnectionStatus.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblConnectionStatus.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblConnectionStatus.ForeColor = Color.LightGreen;
            lblConnectionStatus.Location = new Point(630, 16);
            lblConnectionStatus.Name = "lblConnectionStatus";
            lblConnectionStatus.Size = new Size(200, 22);
            lblConnectionStatus.TabIndex = 2;
            lblConnectionStatus.Text = "Server: Connected";
            lblConnectionStatus.TextAlign = ContentAlignment.MiddleRight;
            // 
            // lblHeaderUser
            // 
            lblHeaderUser.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblHeaderUser.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblHeaderUser.ForeColor = Color.White;
            lblHeaderUser.Location = new Point(836, 14);
            lblHeaderUser.Name = "lblHeaderUser";
            lblHeaderUser.Size = new Size(360, 26);
            lblHeaderUser.TabIndex = 1;
            lblHeaderUser.Text = "player1  |  Coins: 1250";
            lblHeaderUser.TextAlign = ContentAlignment.MiddleRight;
            // 
            // cardRacer
            // 
            cardRacer.Controls.Add(lblRacerTitle);
            cardRacer.Controls.Add(lblRacerSub);
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
            cardRacer.ForeColor = Color.FromArgb(250, 250, 250);
            cardRacer.LeftSideColor = Color.FromArgb(253, 251, 247);
            cardRacer.Location = new Point(66, 118);
            cardRacer.Name = "cardRacer";
            cardRacer.RightSideColor = Color.FromArgb(253, 251, 247);
            cardRacer.Side = ReaLTaiizor.Controls.NightPanel.PanelSide.Left;
            cardRacer.Size = new Size(310, 520);
            cardRacer.TabIndex = 3;
            // 
            // lblRacerTitle
            // 
            lblRacerTitle.AutoSize = true;
            lblRacerTitle.Font = new Font("Segoe UI", 14.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblRacerTitle.ForeColor = Color.Black;
            lblRacerTitle.Location = new Point(110, 12);
            lblRacerTitle.Name = "lblRacerTitle";
            lblRacerTitle.Size = new Size(90, 33);
            lblRacerTitle.TabIndex = 0;
            lblRacerTitle.Text = "RACER";
            // 
            // lblRacerSub
            // 
            lblRacerSub.AutoSize = true;
            lblRacerSub.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblRacerSub.ForeColor = Color.DimGray;
            lblRacerSub.Location = new Point(122, 46);
            lblRacerSub.Name = "lblRacerSub";
            lblRacerSub.Size = new Size(65, 20);
            lblRacerSub.TabIndex = 1;
            lblRacerSub.Text = "Account";
            // 
            // lblFullNameCaption
            // 
            lblFullNameCaption.AutoSize = true;
            lblFullNameCaption.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblFullNameCaption.ForeColor = Color.Black;
            lblFullNameCaption.Location = new Point(22, 78);
            lblFullNameCaption.Name = "lblFullNameCaption";
            lblFullNameCaption.Size = new Size(84, 20);
            lblFullNameCaption.TabIndex = 2;
            lblFullNameCaption.Text = "Full Name";
            // 
            // lblFullNameValue
            // 
            lblFullNameValue.AutoSize = true;
            lblFullNameValue.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblFullNameValue.ForeColor = Color.FromArgb(43, 27, 61);
            lblFullNameValue.Location = new Point(22, 100);
            lblFullNameValue.Name = "lblFullNameValue";
            lblFullNameValue.Size = new Size(112, 21);
            lblFullNameValue.TabIndex = 3;
            lblFullNameValue.Text = "Player One";
            // 
            // lblUsernameCaption
            // 
            lblUsernameCaption.AutoSize = true;
            lblUsernameCaption.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblUsernameCaption.ForeColor = Color.Black;
            lblUsernameCaption.Location = new Point(22, 138);
            lblUsernameCaption.Name = "lblUsernameCaption";
            lblUsernameCaption.Size = new Size(84, 20);
            lblUsernameCaption.TabIndex = 4;
            lblUsernameCaption.Text = "Username";
            // 
            // lblUsernameValue
            // 
            lblUsernameValue.AutoSize = true;
            lblUsernameValue.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblUsernameValue.ForeColor = Color.FromArgb(43, 27, 61);
            lblUsernameValue.Location = new Point(22, 160);
            lblUsernameValue.Name = "lblUsernameValue";
            lblUsernameValue.Size = new Size(94, 21);
            lblUsernameValue.TabIndex = 5;
            lblUsernameValue.Text = "player1";
            // 
            // lblEmailCaption
            // 
            lblEmailCaption.AutoSize = true;
            lblEmailCaption.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblEmailCaption.ForeColor = Color.Black;
            lblEmailCaption.Location = new Point(22, 198);
            lblEmailCaption.Name = "lblEmailCaption";
            lblEmailCaption.Size = new Size(50, 20);
            lblEmailCaption.TabIndex = 6;
            lblEmailCaption.Text = "Email";
            // 
            // lblEmailValue
            // 
            lblEmailValue.AutoSize = true;
            lblEmailValue.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblEmailValue.ForeColor = Color.FromArgb(43, 27, 61);
            lblEmailValue.Location = new Point(22, 220);
            lblEmailValue.Name = "lblEmailValue";
            lblEmailValue.Size = new Size(76, 21);
            lblEmailValue.TabIndex = 7;
            lblEmailValue.Text = "Not set";
            // 
            // lblBirthdayCaption
            // 
            lblBirthdayCaption.AutoSize = true;
            lblBirthdayCaption.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblBirthdayCaption.ForeColor = Color.Black;
            lblBirthdayCaption.Location = new Point(22, 258);
            lblBirthdayCaption.Name = "lblBirthdayCaption";
            lblBirthdayCaption.Size = new Size(72, 20);
            lblBirthdayCaption.TabIndex = 8;
            lblBirthdayCaption.Text = "Birthday";
            // 
            // lblBirthdayValue
            // 
            lblBirthdayValue.AutoSize = true;
            lblBirthdayValue.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblBirthdayValue.ForeColor = Color.FromArgb(43, 27, 61);
            lblBirthdayValue.Location = new Point(22, 280);
            lblBirthdayValue.Name = "lblBirthdayValue";
            lblBirthdayValue.Size = new Size(76, 21);
            lblBirthdayValue.TabIndex = 9;
            lblBirthdayValue.Text = "Not set";
            // 
            // lblCreatedAtCaption
            // 
            lblCreatedAtCaption.AutoSize = true;
            lblCreatedAtCaption.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblCreatedAtCaption.ForeColor = Color.DimGray;
            lblCreatedAtCaption.Location = new Point(22, 320);
            lblCreatedAtCaption.Name = "lblCreatedAtCaption";
            lblCreatedAtCaption.Size = new Size(76, 19);
            lblCreatedAtCaption.TabIndex = 10;
            lblCreatedAtCaption.Text = "Created:";
            // 
            // lblCreatedAtValue
            // 
            lblCreatedAtValue.AutoSize = true;
            lblCreatedAtValue.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblCreatedAtValue.ForeColor = Color.DimGray;
            lblCreatedAtValue.Location = new Point(156, 320);
            lblCreatedAtValue.Name = "lblCreatedAtValue";
            lblCreatedAtValue.Size = new Size(24, 19);
            lblCreatedAtValue.TabIndex = 11;
            lblCreatedAtValue.Text = "--";
            // 
            // lblLastLoginCaption
            // 
            lblLastLoginCaption.AutoSize = true;
            lblLastLoginCaption.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblLastLoginCaption.ForeColor = Color.DimGray;
            lblLastLoginCaption.Location = new Point(22, 348);
            lblLastLoginCaption.Name = "lblLastLoginCaption";
            lblLastLoginCaption.Size = new Size(92, 19);
            lblLastLoginCaption.TabIndex = 12;
            lblLastLoginCaption.Text = "Last login:";
            // 
            // lblLastLoginValue
            // 
            lblLastLoginValue.AutoSize = true;
            lblLastLoginValue.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblLastLoginValue.ForeColor = Color.DimGray;
            lblLastLoginValue.Location = new Point(156, 348);
            lblLastLoginValue.Name = "lblLastLoginValue";
            lblLastLoginValue.Size = new Size(24, 19);
            lblLastLoginValue.TabIndex = 13;
            lblLastLoginValue.Text = "--";
            // 
            // lblCoinsCaption
            // 
            lblCoinsCaption.AutoSize = true;
            lblCoinsCaption.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblCoinsCaption.ForeColor = Color.Black;
            lblCoinsCaption.Location = new Point(22, 392);
            lblCoinsCaption.Name = "lblCoinsCaption";
            lblCoinsCaption.Size = new Size(52, 21);
            lblCoinsCaption.TabIndex = 14;
            lblCoinsCaption.Text = "Coins";
            // 
            // lblCoinsValue
            // 
            lblCoinsValue.AutoSize = true;
            lblCoinsValue.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblCoinsValue.ForeColor = Color.FromArgb(155, 17, 24);
            lblCoinsValue.Location = new Point(22, 418);
            lblCoinsValue.Name = "lblCoinsValue";
            lblCoinsValue.Size = new Size(56, 27);
            lblCoinsValue.TabIndex = 15;
            lblCoinsValue.Text = "1250";
            // 
            // shadowRacer
            // 
            shadowRacer.BackColor = Color.FromArgb(43, 27, 61);
            shadowRacer.Location = new Point(72, 124);
            shadowRacer.Name = "shadowRacer";
            shadowRacer.Size = new Size(310, 520);
            shadowRacer.TabIndex = 4;
            // 
            // cardMenu
            // 
            cardMenu.Controls.Add(lblMenuTitle);
            cardMenu.Controls.Add(lblMenuSub);
            cardMenu.Controls.Add(btnSinglePlayer);
            cardMenu.Controls.Add(btnMultiplayer);
            cardMenu.Controls.Add(btnLeaderboard);
            cardMenu.Controls.Add(btnLogout);
            cardMenu.ForeColor = Color.FromArgb(250, 250, 250);
            cardMenu.LeftSideColor = Color.FromArgb(253, 251, 247);
            cardMenu.Location = new Point(430, 118);
            cardMenu.Name = "cardMenu";
            cardMenu.RightSideColor = Color.FromArgb(253, 251, 247);
            cardMenu.Side = ReaLTaiizor.Controls.NightPanel.PanelSide.Left;
            cardMenu.Size = new Size(340, 520);
            cardMenu.TabIndex = 5;
            // 
            // lblMenuTitle
            // 
            lblMenuTitle.AutoSize = true;
            lblMenuTitle.Font = new Font("Segoe UI", 14.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblMenuTitle.ForeColor = Color.Black;
            lblMenuTitle.Location = new Point(132, 12);
            lblMenuTitle.Name = "lblMenuTitle";
            lblMenuTitle.Size = new Size(74, 33);
            lblMenuTitle.TabIndex = 0;
            lblMenuTitle.Text = "MENU";
            // 
            // lblMenuSub
            // 
            lblMenuSub.AutoSize = true;
            lblMenuSub.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblMenuSub.ForeColor = Color.DimGray;
            lblMenuSub.Location = new Point(114, 46);
            lblMenuSub.Name = "lblMenuSub";
            lblMenuSub.Size = new Size(110, 20);
            lblMenuSub.TabIndex = 1;
            lblMenuSub.Text = "Select game mode";
            // 
            // btnSinglePlayer
            // 
            btnSinglePlayer.BackColor = Color.FromArgb(155, 17, 24);
            btnSinglePlayer.FlatAppearance.BorderColor = Color.FromArgb(43, 27, 61);
            btnSinglePlayer.FlatAppearance.BorderSize = 2;
            btnSinglePlayer.FlatStyle = FlatStyle.Flat;
            btnSinglePlayer.Font = new Font("Segoe UI", 11F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnSinglePlayer.ForeColor = Color.White;
            btnSinglePlayer.Location = new Point(28, 102);
            btnSinglePlayer.Name = "btnSinglePlayer";
            btnSinglePlayer.Size = new Size(284, 56);
            btnSinglePlayer.TabIndex = 2;
            btnSinglePlayer.Text = "Single Player";
            btnSinglePlayer.UseVisualStyleBackColor = false;
            btnSinglePlayer.Click += btnSinglePlayer_Click;
            // 
            // btnMultiplayer
            // 
            btnMultiplayer.BackColor = Color.FromArgb(166, 82, 0);
            btnMultiplayer.FlatAppearance.BorderColor = Color.FromArgb(43, 27, 61);
            btnMultiplayer.FlatAppearance.BorderSize = 2;
            btnMultiplayer.FlatStyle = FlatStyle.Flat;
            btnMultiplayer.Font = new Font("Segoe UI", 11F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnMultiplayer.ForeColor = Color.White;
            btnMultiplayer.Location = new Point(28, 178);
            btnMultiplayer.Name = "btnMultiplayer";
            btnMultiplayer.Size = new Size(284, 56);
            btnMultiplayer.TabIndex = 3;
            btnMultiplayer.Text = "Multiplayer";
            btnMultiplayer.UseVisualStyleBackColor = false;
            btnMultiplayer.Click += btnMultiplayer_Click;
            // 
            // btnLeaderboard
            // 
            btnLeaderboard.BackColor = Color.White;
            btnLeaderboard.FlatAppearance.BorderColor = Color.FromArgb(43, 27, 61);
            btnLeaderboard.FlatAppearance.BorderSize = 2;
            btnLeaderboard.FlatStyle = FlatStyle.Flat;
            btnLeaderboard.Font = new Font("Segoe UI", 11F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnLeaderboard.ForeColor = Color.Black;
            btnLeaderboard.Location = new Point(28, 254);
            btnLeaderboard.Name = "btnLeaderboard";
            btnLeaderboard.Size = new Size(284, 56);
            btnLeaderboard.TabIndex = 4;
            btnLeaderboard.Text = "Leaderboard";
            btnLeaderboard.UseVisualStyleBackColor = false;
            btnLeaderboard.Click += btnLeaderboard_Click;
            // 
            // btnLogout
            // 
            btnLogout.BackColor = Color.White;
            btnLogout.FlatAppearance.BorderColor = Color.FromArgb(43, 27, 61);
            btnLogout.FlatAppearance.BorderSize = 2;
            btnLogout.FlatStyle = FlatStyle.Flat;
            btnLogout.Font = new Font("Segoe UI", 11F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnLogout.ForeColor = Color.Black;
            btnLogout.Location = new Point(28, 330);
            btnLogout.Name = "btnLogout";
            btnLogout.Size = new Size(284, 56);
            btnLogout.TabIndex = 5;
            btnLogout.Text = "Logout";
            btnLogout.UseVisualStyleBackColor = false;
            btnLogout.Click += btnLogout_Click;
            // 
            // shadowMenu
            // 
            shadowMenu.BackColor = Color.FromArgb(43, 27, 61);
            shadowMenu.Location = new Point(436, 124);
            shadowMenu.Name = "shadowMenu";
            shadowMenu.Size = new Size(340, 520);
            shadowMenu.TabIndex = 6;
            // 
            // cardRooms
            // 
            cardRooms.Controls.Add(lblRoomsTitle);
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
            cardRooms.ForeColor = Color.FromArgb(250, 250, 250);
            cardRooms.LeftSideColor = Color.FromArgb(253, 251, 247);
            cardRooms.Location = new Point(824, 118);
            cardRooms.Name = "cardRooms";
            cardRooms.RightSideColor = Color.FromArgb(253, 251, 247);
            cardRooms.Side = ReaLTaiizor.Controls.NightPanel.PanelSide.Left;
            cardRooms.Size = new Size(326, 520);
            cardRooms.TabIndex = 7;
            // 
            // lblRoomsTitle
            // 
            lblRoomsTitle.AutoSize = true;
            lblRoomsTitle.Font = new Font("Segoe UI", 14.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblRoomsTitle.ForeColor = Color.Black;
            lblRoomsTitle.Location = new Point(86, 12);
            lblRoomsTitle.Name = "lblRoomsTitle";
            lblRoomsTitle.Size = new Size(154, 33);
            lblRoomsTitle.TabIndex = 0;
            lblRoomsTitle.Text = "OPEN ROOMS";
            // 
            // lblRoom1Title
            // 
            lblRoom1Title.AutoSize = true;
            lblRoom1Title.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblRoom1Title.ForeColor = Color.Black;
            lblRoom1Title.Location = new Point(22, 74);
            lblRoom1Title.Name = "lblRoom1Title";
            lblRoom1Title.Size = new Size(88, 23);
            lblRoom1Title.TabIndex = 1;
            lblRoom1Title.Text = "Room 01";
            // 
            // lblRoom1Info
            // 
            lblRoom1Info.AutoSize = true;
            lblRoom1Info.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblRoom1Info.ForeColor = Color.DimGray;
            lblRoom1Info.Location = new Point(22, 99);
            lblRoom1Info.Name = "lblRoom1Info";
            lblRoom1Info.Size = new Size(116, 20);
            lblRoom1Info.TabIndex = 2;
            lblRoom1Info.Text = "2/4 · Waiting";
            // 
            // btnJoinRoom1
            // 
            btnJoinRoom1.BackColor = Color.FromArgb(155, 17, 24);
            btnJoinRoom1.FlatAppearance.BorderColor = Color.FromArgb(43, 27, 61);
            btnJoinRoom1.FlatAppearance.BorderSize = 2;
            btnJoinRoom1.FlatStyle = FlatStyle.Flat;
            btnJoinRoom1.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnJoinRoom1.ForeColor = Color.White;
            btnJoinRoom1.Location = new Point(222, 76);
            btnJoinRoom1.Name = "btnJoinRoom1";
            btnJoinRoom1.Size = new Size(82, 38);
            btnJoinRoom1.TabIndex = 3;
            btnJoinRoom1.Text = "Join";
            btnJoinRoom1.UseVisualStyleBackColor = false;
            btnJoinRoom1.Click += btnJoinRoom1_Click;
            // 
            // lblRoom2Title
            // 
            lblRoom2Title.AutoSize = true;
            lblRoom2Title.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblRoom2Title.ForeColor = Color.Black;
            lblRoom2Title.Location = new Point(22, 146);
            lblRoom2Title.Name = "lblRoom2Title";
            lblRoom2Title.Size = new Size(88, 23);
            lblRoom2Title.TabIndex = 4;
            lblRoom2Title.Text = "Room 02";
            // 
            // lblRoom2Info
            // 
            lblRoom2Info.AutoSize = true;
            lblRoom2Info.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblRoom2Info.ForeColor = Color.DimGray;
            lblRoom2Info.Location = new Point(22, 171);
            lblRoom2Info.Name = "lblRoom2Info";
            lblRoom2Info.Size = new Size(105, 20);
            lblRoom2Info.TabIndex = 5;
            lblRoom2Info.Text = "4/4 · Racing";
            // 
            // btnSpectateRoom2
            // 
            btnSpectateRoom2.BackColor = Color.White;
            btnSpectateRoom2.FlatAppearance.BorderColor = Color.FromArgb(43, 27, 61);
            btnSpectateRoom2.FlatAppearance.BorderSize = 2;
            btnSpectateRoom2.FlatStyle = FlatStyle.Flat;
            btnSpectateRoom2.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnSpectateRoom2.ForeColor = Color.FromArgb(43, 27, 61);
            btnSpectateRoom2.Location = new Point(222, 148);
            btnSpectateRoom2.Name = "btnSpectateRoom2";
            btnSpectateRoom2.Size = new Size(82, 38);
            btnSpectateRoom2.TabIndex = 6;
            btnSpectateRoom2.Text = "Watch";
            btnSpectateRoom2.UseVisualStyleBackColor = false;
            btnSpectateRoom2.Click += btnSpectateRoom2_Click;
            // 
            // lblRoom3Title
            // 
            lblRoom3Title.AutoSize = true;
            lblRoom3Title.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblRoom3Title.ForeColor = Color.Black;
            lblRoom3Title.Location = new Point(22, 218);
            lblRoom3Title.Name = "lblRoom3Title";
            lblRoom3Title.Size = new Size(88, 23);
            lblRoom3Title.TabIndex = 7;
            lblRoom3Title.Text = "Room 03";
            // 
            // lblRoom3Info
            // 
            lblRoom3Info.AutoSize = true;
            lblRoom3Info.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblRoom3Info.ForeColor = Color.DimGray;
            lblRoom3Info.Location = new Point(22, 243);
            lblRoom3Info.Name = "lblRoom3Info";
            lblRoom3Info.Size = new Size(116, 20);
            lblRoom3Info.TabIndex = 8;
            lblRoom3Info.Text = "1/4 · Waiting";
            // 
            // btnJoinRoom3
            // 
            btnJoinRoom3.BackColor = Color.FromArgb(155, 17, 24);
            btnJoinRoom3.FlatAppearance.BorderColor = Color.FromArgb(43, 27, 61);
            btnJoinRoom3.FlatAppearance.BorderSize = 2;
            btnJoinRoom3.FlatStyle = FlatStyle.Flat;
            btnJoinRoom3.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnJoinRoom3.ForeColor = Color.White;
            btnJoinRoom3.Location = new Point(222, 220);
            btnJoinRoom3.Name = "btnJoinRoom3";
            btnJoinRoom3.Size = new Size(82, 38);
            btnJoinRoom3.TabIndex = 9;
            btnJoinRoom3.Text = "Join";
            btnJoinRoom3.UseVisualStyleBackColor = false;
            btnJoinRoom3.Click += btnJoinRoom3_Click;
            // 
            // btnCreateRoom
            // 
            btnCreateRoom.BackColor = Color.FromArgb(166, 82, 0);
            btnCreateRoom.FlatAppearance.BorderColor = Color.FromArgb(43, 27, 61);
            btnCreateRoom.FlatAppearance.BorderSize = 2;
            btnCreateRoom.FlatStyle = FlatStyle.Flat;
            btnCreateRoom.Font = new Font("Segoe UI", 11F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnCreateRoom.ForeColor = Color.White;
            btnCreateRoom.Location = new Point(22, 434);
            btnCreateRoom.Name = "btnCreateRoom";
            btnCreateRoom.Size = new Size(282, 56);
            btnCreateRoom.TabIndex = 10;
            btnCreateRoom.Text = "Create Room";
            btnCreateRoom.UseVisualStyleBackColor = false;
            btnCreateRoom.Click += btnCreateRoom_Click;
            // 
            // shadowRooms
            // 
            shadowRooms.BackColor = Color.FromArgb(43, 27, 61);
            shadowRooms.Location = new Point(830, 124);
            shadowRooms.Name = "shadowRooms";
            shadowRooms.Size = new Size(326, 520);
            shadowRooms.TabIndex = 8;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            BackgroundImage = Properties.Resources.bg;
            BackgroundImageLayout = ImageLayout.Stretch;
            ClientSize = new Size(1220, 700);
            Controls.Add(cardRacer);
            Controls.Add(shadowRacer);
            Controls.Add(cardMenu);
            Controls.Add(shadowMenu);
            Controls.Add(cardRooms);
            Controls.Add(shadowRooms);
            Controls.Add(pnlHeaderBar);
            Controls.Add(pnlTitleBar);
            ForeColor = Color.Transparent;
            FormBorderStyle = FormBorderStyle.None;
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            pnlTitleBar.ResumeLayout(false);
            pnlTitleBar.PerformLayout();
            pnlHeaderBar.ResumeLayout(false);
            pnlHeaderBar.PerformLayout();
            cardRacer.ResumeLayout(false);
            cardRacer.PerformLayout();
            cardMenu.ResumeLayout(false);
            cardMenu.PerformLayout();
            cardRooms.ResumeLayout(false);
            cardRooms.PerformLayout();
            ResumeLayout(false);
        }

        #endregion
        private Panel pnlTitleBar;
        private Label titellbl;
        private Button btnMinimize;
        private Button btnMaximize;
        private Button btnExit;
        private Panel pnlHeaderBar;
        private Label lblHeaderTitle;
        private Label lblConnectionStatus;
        private Label lblHeaderUser;
        private ReaLTaiizor.Controls.NightPanel cardRacer;
        private Label lblRacerTitle;
        private Label lblRacerSub;
        private Label lblFullNameCaption;
        private Label lblFullNameValue;
        private Label lblUsernameCaption;
        private Label lblUsernameValue;
        private Label lblEmailCaption;
        private Label lblEmailValue;
        private Label lblBirthdayCaption;
        private Label lblBirthdayValue;
        private Label lblCreatedAtCaption;
        private Label lblCreatedAtValue;
        private Label lblLastLoginCaption;
        private Label lblLastLoginValue;
        private Label lblCoinsCaption;
        private Label lblCoinsValue;
        private Panel shadowRacer;
        private ReaLTaiizor.Controls.NightPanel cardMenu;
        private Label lblMenuTitle;
        private Label lblMenuSub;
        private Button btnSinglePlayer;
        private Button btnMultiplayer;
        private Button btnLeaderboard;
        private Button btnLogout;
        private Panel shadowMenu;
        private ReaLTaiizor.Controls.NightPanel cardRooms;
        private Label lblRoomsTitle;
        private Label lblRoom1Title;
        private Label lblRoom1Info;
        private Button btnJoinRoom1;
        private Label lblRoom2Title;
        private Label lblRoom2Info;
        private Button btnSpectateRoom2;
        private Label lblRoom3Title;
        private Label lblRoom3Info;
        private Button btnJoinRoom3;
        private Button btnCreateRoom;
        private Panel shadowRooms;
    }
}