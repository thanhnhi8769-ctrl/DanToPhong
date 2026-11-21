namespace UI
{
    partial class LoginForm
    {
        private System.ComponentModel.IContainer? components = null;

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
            lblHeaderBadge = new Label();
            nightPanel1 = new ReaLTaiizor.Controls.NightPanel();
            btnRegister = new Button();
            btnLogin = new Button();
            btnForgotPassword = new Button();
            chkRememberMe = new CheckBox();
            lblErrPassword = new Label();
            btnTogglePassword = new Button();
            txtPassword = new TextBox();
            lblPassword = new Label();
            lblErrUsername = new Label();
            txtUsername = new TextBox();
            lblUsername = new Label();
            btnHelp = new Button();
            lblSubtitle = new Label();
            lblTitle = new Label();
            panelShadow = new Panel();
            pnlTitleBar.SuspendLayout();
            pnlHeaderBar.SuspendLayout();
            nightPanel1.SuspendLayout();
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
            pnlTitleBar.Location = new Point(0, 0);
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
            titellbl.Size = new Size(154, 28);
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
            btnMinimize.TabIndex = 10;
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
            btnMaximize.TabIndex = 11;
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
            btnExit.TabIndex = 12;
            btnExit.Text = "X";
            btnExit.UseVisualStyleBackColor = true;
            btnExit.Click += btnExit_Click;
            // 
            // pnlHeaderBar
            // 
            pnlHeaderBar.BackColor = Color.FromArgb(43, 27, 61);
            pnlHeaderBar.Controls.Add(lblHeaderTitle);
            pnlHeaderBar.Controls.Add(lblHeaderBadge);
            pnlHeaderBar.Dock = DockStyle.Top;
            pnlHeaderBar.Location = new Point(0, 44);
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
            lblHeaderTitle.Size = new Size(236, 36);
            lblHeaderTitle.TabIndex = 0;
            lblHeaderTitle.Text = "SCOOTER RACING";
            // 
            // lblHeaderBadge
            // 
            lblHeaderBadge.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblHeaderBadge.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblHeaderBadge.ForeColor = Color.FromArgb(245, 208, 120);
            lblHeaderBadge.Location = new Point(942, 15);
            lblHeaderBadge.Name = "lblHeaderBadge";
            lblHeaderBadge.Size = new Size(254, 24);
            lblHeaderBadge.TabIndex = 1;
            lblHeaderBadge.Text = "RACER PORTAL";
            lblHeaderBadge.TextAlign = ContentAlignment.MiddleRight;
            // 
            // nightPanel1
            // 
            nightPanel1.Anchor = AnchorStyles.None;
            nightPanel1.Controls.Add(btnRegister);
            nightPanel1.Controls.Add(btnLogin);
            nightPanel1.Controls.Add(btnForgotPassword);
            nightPanel1.Controls.Add(chkRememberMe);
            nightPanel1.Controls.Add(lblErrPassword);
            nightPanel1.Controls.Add(btnTogglePassword);
            nightPanel1.Controls.Add(txtPassword);
            nightPanel1.Controls.Add(lblPassword);
            nightPanel1.Controls.Add(lblErrUsername);
            nightPanel1.Controls.Add(txtUsername);
            nightPanel1.Controls.Add(lblUsername);
            nightPanel1.Controls.Add(btnHelp);
            nightPanel1.Controls.Add(lblSubtitle);
            nightPanel1.Controls.Add(lblTitle);
            nightPanel1.ForeColor = Color.FromArgb(250, 250, 250);
            nightPanel1.LeftSideColor = Color.FromArgb(253, 251, 247);
            nightPanel1.Location = new Point(365, 130);
            nightPanel1.Name = "nightPanel1";
            nightPanel1.RightSideColor = Color.FromArgb(253, 251, 247);
            nightPanel1.Side = ReaLTaiizor.Controls.NightPanel.PanelSide.Left;
            nightPanel1.Size = new Size(490, 480);
            nightPanel1.TabIndex = 2;
            // 
            // btnRegister
            // 
            btnRegister.BackColor = Color.White;
            btnRegister.FlatAppearance.BorderColor = Color.FromArgb(43, 27, 61);
            btnRegister.FlatAppearance.BorderSize = 2;
            btnRegister.FlatStyle = FlatStyle.Flat;
            btnRegister.Font = new Font("Segoe UI", 11F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnRegister.ForeColor = Color.Black;
            btnRegister.Location = new Point(37, 366);
            btnRegister.Name = "btnRegister";
            btnRegister.Size = new Size(416, 48);
            btnRegister.TabIndex = 7;
            btnRegister.Text = "Create New Account";
            btnRegister.UseVisualStyleBackColor = false;
            btnRegister.Click += btnRegister_Click;
            // 
            // btnLogin
            // 
            btnLogin.BackColor = Color.FromArgb(155, 17, 24);
            btnLogin.FlatAppearance.BorderColor = Color.FromArgb(43, 27, 61);
            btnLogin.FlatAppearance.BorderSize = 2;
            btnLogin.FlatStyle = FlatStyle.Flat;
            btnLogin.Font = new Font("Segoe UI", 11F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnLogin.ForeColor = Color.White;
            btnLogin.Location = new Point(37, 304);
            btnLogin.Name = "btnLogin";
            btnLogin.Size = new Size(416, 48);
            btnLogin.TabIndex = 6;
            btnLogin.Text = "Login";
            btnLogin.UseVisualStyleBackColor = false;
            btnLogin.Click += btnLogin_Click;
            // 
            // btnForgotPassword
            // 
            btnForgotPassword.Cursor = Cursors.Hand;
            btnForgotPassword.FlatAppearance.BorderSize = 0;
            btnForgotPassword.FlatStyle = FlatStyle.Flat;
            btnForgotPassword.Font = new Font("Segoe UI", 9F, FontStyle.Underline, GraphicsUnit.Point, 0);
            btnForgotPassword.ForeColor = Color.FromArgb(43, 27, 61);
            btnForgotPassword.Location = new Point(277, 253);
            btnForgotPassword.Name = "btnForgotPassword";
            btnForgotPassword.Size = new Size(176, 40);
            btnForgotPassword.TabIndex = 5;
            btnForgotPassword.Text = "Forgot password?";
            btnForgotPassword.TextAlign = ContentAlignment.MiddleRight;
            btnForgotPassword.UseVisualStyleBackColor = false;
            btnForgotPassword.Click += btnForgotPassword_Click;
            // 
            // chkRememberMe
            // 
            chkRememberMe.AutoSize = true;
            chkRememberMe.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            chkRememberMe.ForeColor = Color.Black;
            chkRememberMe.Location = new Point(37, 260);
            chkRememberMe.Name = "chkRememberMe";
            chkRememberMe.Size = new Size(154, 29);
            chkRememberMe.TabIndex = 4;
            chkRememberMe.Text = "Remember me";
            chkRememberMe.UseVisualStyleBackColor = true;
            chkRememberMe.CheckedChanged += chkRememberMe_CheckedChanged;
            // 
            // lblErrPassword
            // 
            lblErrPassword.AutoSize = true;
            lblErrPassword.Font = new Font("Segoe UI", 8F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblErrPassword.ForeColor = Color.Crimson;
            lblErrPassword.Location = new Point(34, 233);
            lblErrPassword.Name = "lblErrPassword";
            lblErrPassword.Size = new Size(0, 21);
            lblErrPassword.TabIndex = 21;
            // 
            // btnTogglePassword
            // 
            btnTogglePassword.Cursor = Cursors.Hand;
            btnTogglePassword.FlatAppearance.BorderSize = 0;
            btnTogglePassword.FlatStyle = FlatStyle.Flat;
            btnTogglePassword.Font = new Font("Segoe UI", 6.5F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btnTogglePassword.ForeColor = Color.Black;
            btnTogglePassword.Location = new Point(423, 205);
            btnTogglePassword.Name = "btnTogglePassword";
            btnTogglePassword.Size = new Size(30, 24);
            btnTogglePassword.TabIndex = 3;
            btnTogglePassword.TabStop = false;
            btnTogglePassword.Text = "👁";
            btnTogglePassword.UseVisualStyleBackColor = false;
            btnTogglePassword.Click += btnTogglePassword_Click;
            // 
            // txtPassword
            // 
            txtPassword.BorderStyle = BorderStyle.None;
            txtPassword.Location = new Point(37, 206);
            txtPassword.Name = "txtPassword";
            txtPassword.PlaceholderText = "  Enter your password";
            txtPassword.Size = new Size(382, 24);
            txtPassword.TabIndex = 2;
            txtPassword.UseSystemPasswordChar = true;
            txtPassword.TextChanged += txtPassword_TextChanged;
            // 
            // lblPassword
            // 
            lblPassword.AutoSize = true;
            lblPassword.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblPassword.ForeColor = Color.Black;
            lblPassword.Location = new Point(34, 178);
            lblPassword.Name = "lblPassword";
            lblPassword.Size = new Size(97, 25);
            lblPassword.TabIndex = 4;
            lblPassword.Text = "Password";
            // 
            // lblErrUsername
            // 
            lblErrUsername.AutoSize = true;
            lblErrUsername.Font = new Font("Segoe UI", 8F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblErrUsername.ForeColor = Color.Crimson;
            lblErrUsername.Location = new Point(34, 151);
            lblErrUsername.Name = "lblErrUsername";
            lblErrUsername.Size = new Size(0, 21);
            lblErrUsername.TabIndex = 20;
            // 
            // txtUsername
            // 
            txtUsername.BorderStyle = BorderStyle.None;
            txtUsername.Location = new Point(37, 124);
            txtUsername.MaxLength = 20;
            txtUsername.Name = "txtUsername";
            txtUsername.PlaceholderText = "  3-20 characters (a-z, 0-9, _)";
            txtUsername.Size = new Size(416, 24);
            txtUsername.TabIndex = 1;
            txtUsername.TextChanged += txtUsername_TextChanged;
            // 
            // lblUsername
            // 
            lblUsername.AutoSize = true;
            lblUsername.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblUsername.ForeColor = Color.Black;
            lblUsername.Location = new Point(34, 96);
            lblUsername.Name = "lblUsername";
            lblUsername.Size = new Size(101, 25);
            lblUsername.TabIndex = 2;
            lblUsername.Text = "Username";
            // 
            // btnHelp
            // 
            btnHelp.Cursor = Cursors.Hand;
            btnHelp.FlatAppearance.BorderColor = Color.FromArgb(43, 27, 61);
            btnHelp.FlatStyle = FlatStyle.Flat;
            btnHelp.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnHelp.ForeColor = Color.FromArgb(43, 27, 61);
            btnHelp.Location = new Point(438, 14);
            btnHelp.Name = "btnHelp";
            btnHelp.Size = new Size(38, 35);
            btnHelp.TabIndex = 30;
            btnHelp.TabStop = false;
            btnHelp.Text = "?";
            btnHelp.UseVisualStyleBackColor = false;
            btnHelp.Click += btnHelp_Click;
            // 
            // lblSubtitle
            // 
            lblSubtitle.AutoSize = true;
            lblSubtitle.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblSubtitle.ForeColor = Color.DimGray;
            lblSubtitle.Location = new Point(150, 58);
            lblSubtitle.Name = "lblSubtitle";
            lblSubtitle.Size = new Size(197, 25);
            lblSubtitle.TabIndex = 1;
            lblSubtitle.Text = "Sign in to hit the track";
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Font = new Font("Segoe UI", 17F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblTitle.ForeColor = Color.Black;
            lblTitle.Location = new Point(195, 16);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(124, 46);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "LOGIN";
            // 
            // panelShadow
            // 
            panelShadow.BackColor = Color.FromArgb(43, 27, 61);
            panelShadow.Location = new Point(371, 136);
            panelShadow.Name = "panelShadow";
            panelShadow.Size = new Size(490, 480);
            panelShadow.TabIndex = 3;
            // 
            // LoginForm
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            BackgroundImage = Properties.Resources.bg;
            BackgroundImageLayout = ImageLayout.Stretch;
            ClientSize = new Size(1220, 720);
            Controls.Add(nightPanel1);
            Controls.Add(panelShadow);
            Controls.Add(pnlHeaderBar);
            Controls.Add(pnlTitleBar);
            ForeColor = Color.Transparent;
            FormBorderStyle = FormBorderStyle.None;
            Name = "LoginForm";
            StartPosition = FormStartPosition.CenterScreen;
            pnlTitleBar.ResumeLayout(false);
            pnlTitleBar.PerformLayout();
            pnlHeaderBar.ResumeLayout(false);
            pnlHeaderBar.PerformLayout();
            nightPanel1.ResumeLayout(false);
            nightPanel1.PerformLayout();
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
        private Label lblHeaderBadge;
        private ReaLTaiizor.Controls.NightPanel nightPanel1;
        private Panel panelShadow;
        private Label lblTitle;
        private Label lblSubtitle;
        private Button btnHelp;
        private Label lblUsername;
        private TextBox txtUsername;
        private Label lblErrUsername;
        private Label lblPassword;
        private TextBox txtPassword;
        private Button btnTogglePassword;
        private Label lblErrPassword;
        private CheckBox chkRememberMe;
        private Button btnForgotPassword;
        private Button btnLogin;
        private Button btnRegister;
    }
}