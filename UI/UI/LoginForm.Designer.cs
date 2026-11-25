using System.Xml.Linq;
using static ReaLTaiizor.Drawing.Poison.PoisonPaint;

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
            picAvatar = new PictureBox();
            panelShadow = new Panel();
            pnlTitleBar.SuspendLayout();
            pnlHeaderBar.SuspendLayout();
            nightPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)picAvatar).BeginInit();
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
            pnlTitleBar.Size = new Size(1220, 40);
            pnlTitleBar.TabIndex = 0;
            pnlTitleBar.MouseDown += pnlTitleBar_MouseDown;
            // 
            // titellbl
            // 
            titellbl.AutoSize = true;
            titellbl.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point, 0);
            titellbl.ForeColor = Color.Indigo;
            titellbl.Location = new Point(22, 9);
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
            btnMinimize.Location = new Point(1056, 5);
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
            btnMaximize.Location = new Point(1109, 5);
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
            btnExit.Location = new Point(1162, 5);
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
            pnlHeaderBar.Location = new Point(0, 40);
            pnlHeaderBar.Name = "pnlHeaderBar";
            pnlHeaderBar.Size = new Size(1220, 48);
            pnlHeaderBar.TabIndex = 1;
            // 
            // lblHeaderTitle
            // 
            lblHeaderTitle.AutoSize = true;
            lblHeaderTitle.Font = new Font("Segoe UI", 12.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblHeaderTitle.ForeColor = Color.White;
            lblHeaderTitle.Location = new Point(22, 10);
            lblHeaderTitle.Name = "lblHeaderTitle";
            lblHeaderTitle.Size = new Size(229, 35);
            lblHeaderTitle.TabIndex = 0;
            lblHeaderTitle.Text = "SCOOTER RACING";
            // 
            // lblHeaderBadge
            // 
            lblHeaderBadge.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblHeaderBadge.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblHeaderBadge.ForeColor = Color.FromArgb(245, 208, 120);
            lblHeaderBadge.Location = new Point(942, 12);
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
            nightPanel1.Controls.Add(picAvatar);
            nightPanel1.ForeColor = Color.FromArgb(250, 250, 250);
            nightPanel1.LeftSideColor = Color.FromArgb(253, 251, 247);
            nightPanel1.Location = new Point(375, 114);
            nightPanel1.Name = "nightPanel1";
            nightPanel1.RightSideColor = Color.FromArgb(253, 251, 247);
            nightPanel1.Side = ReaLTaiizor.Controls.NightPanel.PanelSide.Left;
            nightPanel1.Size = new Size(470, 583);
            nightPanel1.TabIndex = 2;
            // 
            // btnRegister
            // 
            btnRegister.BackColor = Color.White;
            btnRegister.Cursor = Cursors.Hand;
            btnRegister.FlatAppearance.BorderColor = Color.FromArgb(43, 27, 61);
            btnRegister.FlatAppearance.BorderSize = 2;
            btnRegister.FlatStyle = FlatStyle.Flat;
            btnRegister.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnRegister.ForeColor = Color.Black;
            btnRegister.Location = new Point(38, 444);
            btnRegister.Name = "btnRegister";
            btnRegister.Size = new Size(394, 46);
            btnRegister.TabIndex = 7;
            btnRegister.Text = "Create New Account";
            btnRegister.UseVisualStyleBackColor = false;
            btnRegister.Click += btnRegister_Click;
            // 
            // btnLogin
            // 
            btnLogin.BackColor = Color.FromArgb(155, 17, 24);
            btnLogin.Cursor = Cursors.Hand;
            btnLogin.FlatAppearance.BorderColor = Color.FromArgb(43, 27, 61);
            btnLogin.FlatAppearance.BorderSize = 2;
            btnLogin.FlatStyle = FlatStyle.Flat;
            btnLogin.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnLogin.ForeColor = Color.White;
            btnLogin.Location = new Point(38, 384);
            btnLogin.Name = "btnLogin";
            btnLogin.Size = new Size(394, 46);
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
            btnForgotPassword.Location = new Point(266, 335);
            btnForgotPassword.Name = "btnForgotPassword";
            btnForgotPassword.Size = new Size(166, 37);
            btnForgotPassword.TabIndex = 5;
            btnForgotPassword.Text = "Forgot password?";
            btnForgotPassword.TextAlign = ContentAlignment.MiddleRight;
            btnForgotPassword.UseVisualStyleBackColor = false;
            btnForgotPassword.Click += btnForgotPassword_Click;
            // 
            // chkRememberMe
            // 
            chkRememberMe.AutoSize = true;
            chkRememberMe.Cursor = Cursors.Hand;
            chkRememberMe.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            chkRememberMe.ForeColor = Color.Black;
            chkRememberMe.Location = new Point(38, 340);
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
            lblErrPassword.Location = new Point(38, 314);
            lblErrPassword.Name = "lblErrPassword";
            lblErrPassword.Size = new Size(0, 21);
            lblErrPassword.TabIndex = 21;
            // 
            // btnTogglePassword
            // 
            btnTogglePassword.Cursor = Cursors.Hand;
            btnTogglePassword.FlatAppearance.BorderSize = 0;
            btnTogglePassword.FlatStyle = FlatStyle.Flat;
            btnTogglePassword.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btnTogglePassword.ForeColor = Color.FromArgb(43, 27, 61);
            btnTogglePassword.Location = new Point(400, 282);
            btnTogglePassword.Name = "btnTogglePassword";
            btnTogglePassword.Size = new Size(32, 29);
            btnTogglePassword.TabIndex = 3;
            btnTogglePassword.TabStop = false;
            btnTogglePassword.Text = "👁";
            btnTogglePassword.UseVisualStyleBackColor = false;
            btnTogglePassword.Click += btnTogglePassword_Click;
            // 
            // txtPassword
            // 
            txtPassword.BackColor = Color.FromArgb(253, 251, 247);
            txtPassword.BorderStyle = BorderStyle.None;
            txtPassword.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point, 0);
            txtPassword.ForeColor = Color.Black;
            txtPassword.Location = new Point(38, 284);
            txtPassword.Name = "txtPassword";
            txtPassword.PlaceholderText = "Enter your password";
            txtPassword.Size = new Size(358, 27);
            txtPassword.TabIndex = 2;
            txtPassword.UseSystemPasswordChar = true;
            txtPassword.TextChanged += txtPassword_TextChanged;
            // 
            // lblPassword
            // 
            lblPassword.AutoSize = true;
            lblPassword.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblPassword.ForeColor = Color.Black;
            lblPassword.Location = new Point(38, 256);
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
            lblErrUsername.Location = new Point(38, 232);
            lblErrUsername.Name = "lblErrUsername";
            lblErrUsername.Size = new Size(0, 21);
            lblErrUsername.TabIndex = 20;
            // 
            // txtUsername
            // 
            txtUsername.BackColor = Color.FromArgb(253, 251, 247);
            txtUsername.BorderStyle = BorderStyle.None;
            txtUsername.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point, 0);
            txtUsername.ForeColor = Color.Black;
            txtUsername.Location = new Point(38, 202);
            txtUsername.MaxLength = 20;
            txtUsername.Name = "txtUsername";
            txtUsername.PlaceholderText = "3-20 characters (a-z, 0-9, _)";
            txtUsername.Size = new Size(394, 27);
            txtUsername.TabIndex = 1;
            txtUsername.TextChanged += txtUsername_TextChanged;
            // 
            // lblUsername
            // 
            lblUsername.AutoSize = true;
            lblUsername.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblUsername.ForeColor = Color.Black;
            lblUsername.Location = new Point(38, 174);
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
            btnHelp.Location = new Point(422, 18);
            btnHelp.Name = "btnHelp";
            btnHelp.Size = new Size(32, 32);
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
            lblSubtitle.Location = new Point(145, 58);
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
            lblTitle.Location = new Point(180, 18);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(124, 46);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "LOGIN";
            // 
            // picAvatar
            // 
            picAvatar.BackColor = Color.FromArgb(240, 236, 246);
            picAvatar.Location = new Point(201, 90);
            picAvatar.Name = "picAvatar";
            picAvatar.Size = new Size(68, 68);
            picAvatar.SizeMode = PictureBoxSizeMode.Zoom;
            picAvatar.TabIndex = 22;
            picAvatar.TabStop = false;
            picAvatar.Paint += picAvatar_Paint;
            // 
            // panelShadow
            // 
            panelShadow.BackColor = Color.FromArgb(43, 27, 61);
            panelShadow.Location = new Point(381, 120);
            panelShadow.Name = "panelShadow";
            panelShadow.Size = new Size(470, 560);
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
            ((System.ComponentModel.ISupportInitialize)picAvatar).EndInit();
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
        private PictureBox picAvatar;
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