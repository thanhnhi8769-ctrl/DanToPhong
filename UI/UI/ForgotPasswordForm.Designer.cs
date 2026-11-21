namespace UI
{
    partial class ForgotPasswordForm
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
            btnCancel = new Button();
            btnResetPassword = new Button();
            lblErrConfirmPassword = new Label();
            btnToggleConfirmPassword = new Button();
            txtConfirmPassword = new TextBox();
            lblConfirmPassword = new Label();
            lblErrNewPassword = new Label();
            btnToggleNewPassword = new Button();
            txtNewPassword = new TextBox();
            lblNewPassword = new Label();
            lblErrEmail = new Label();
            txtEmail = new TextBox();
            lblEmail = new Label();
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
            lblHeaderBadge.Text = "ACCOUNT RECOVERY";
            lblHeaderBadge.TextAlign = ContentAlignment.MiddleRight;
            // 
            // nightPanel1
            // 
            nightPanel1.Anchor = AnchorStyles.None;
            nightPanel1.Controls.Add(btnCancel);
            nightPanel1.Controls.Add(btnResetPassword);
            nightPanel1.Controls.Add(lblErrConfirmPassword);
            nightPanel1.Controls.Add(btnToggleConfirmPassword);
            nightPanel1.Controls.Add(txtConfirmPassword);
            nightPanel1.Controls.Add(lblConfirmPassword);
            nightPanel1.Controls.Add(lblErrNewPassword);
            nightPanel1.Controls.Add(btnToggleNewPassword);
            nightPanel1.Controls.Add(txtNewPassword);
            nightPanel1.Controls.Add(lblNewPassword);
            nightPanel1.Controls.Add(lblErrEmail);
            nightPanel1.Controls.Add(txtEmail);
            nightPanel1.Controls.Add(lblEmail);
            nightPanel1.Controls.Add(lblErrUsername);
            nightPanel1.Controls.Add(txtUsername);
            nightPanel1.Controls.Add(lblUsername);
            nightPanel1.Controls.Add(btnHelp);
            nightPanel1.Controls.Add(lblSubtitle);
            nightPanel1.Controls.Add(lblTitle);
            nightPanel1.ForeColor = Color.FromArgb(250, 250, 250);
            nightPanel1.LeftSideColor = Color.FromArgb(253, 251, 247);
            nightPanel1.Location = new Point(365, 122);
            nightPanel1.Name = "nightPanel1";
            nightPanel1.RightSideColor = Color.FromArgb(253, 251, 247);
            nightPanel1.Side = ReaLTaiizor.Controls.NightPanel.PanelSide.Left;
            nightPanel1.Size = new Size(490, 580);
            nightPanel1.TabIndex = 2;
            // 
            // btnCancel
            // 
            btnCancel.BackColor = Color.White;
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(43, 27, 61);
            btnCancel.FlatAppearance.BorderSize = 2;
            btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.Font = new Font("Segoe UI", 11F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnCancel.ForeColor = Color.Black;
            btnCancel.Location = new Point(331, 476);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(122, 48);
            btnCancel.TabIndex = 6;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = false;
            btnCancel.Click += btnCancel_Click;
            // 
            // btnResetPassword
            // 
            btnResetPassword.BackColor = Color.FromArgb(155, 17, 24);
            btnResetPassword.FlatAppearance.BorderColor = Color.FromArgb(43, 27, 61);
            btnResetPassword.FlatAppearance.BorderSize = 2;
            btnResetPassword.FlatStyle = FlatStyle.Flat;
            btnResetPassword.Font = new Font("Segoe UI", 11F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnResetPassword.ForeColor = Color.White;
            btnResetPassword.Location = new Point(37, 476);
            btnResetPassword.Name = "btnResetPassword";
            btnResetPassword.Size = new Size(284, 48);
            btnResetPassword.TabIndex = 5;
            btnResetPassword.Text = "Reset Password";
            btnResetPassword.UseVisualStyleBackColor = false;
            btnResetPassword.Click += btnResetPassword_Click;
            // 
            // lblErrConfirmPassword
            // 
            lblErrConfirmPassword.AutoSize = true;
            lblErrConfirmPassword.Font = new Font("Segoe UI", 8F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblErrConfirmPassword.ForeColor = Color.Crimson;
            lblErrConfirmPassword.Location = new Point(34, 393);
            lblErrConfirmPassword.Name = "lblErrConfirmPassword";
            lblErrConfirmPassword.Size = new Size(0, 21);
            lblErrConfirmPassword.TabIndex = 19;
            // 
            // btnToggleConfirmPassword
            // 
            btnToggleConfirmPassword.Cursor = Cursors.Hand;
            btnToggleConfirmPassword.FlatAppearance.BorderSize = 0;
            btnToggleConfirmPassword.FlatStyle = FlatStyle.Flat;
            btnToggleConfirmPassword.Font = new Font("Segoe UI", 6.5F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btnToggleConfirmPassword.ForeColor = Color.Black;
            btnToggleConfirmPassword.Location = new Point(423, 365);
            btnToggleConfirmPassword.Name = "btnToggleConfirmPassword";
            btnToggleConfirmPassword.Size = new Size(30, 24);
            btnToggleConfirmPassword.TabIndex = 15;
            btnToggleConfirmPassword.TabStop = false;
            btnToggleConfirmPassword.Text = "👁";
            btnToggleConfirmPassword.UseVisualStyleBackColor = false;
            btnToggleConfirmPassword.Click += btnToggleConfirmPassword_Click;
            // 
            // txtConfirmPassword
            // 
            txtConfirmPassword.BorderStyle = BorderStyle.None;
            txtConfirmPassword.Location = new Point(37, 366);
            txtConfirmPassword.Name = "txtConfirmPassword";
            txtConfirmPassword.PlaceholderText = "Re-enter new password";
            txtConfirmPassword.Size = new Size(382, 24);
            txtConfirmPassword.TabIndex = 4;
            txtConfirmPassword.UseSystemPasswordChar = true;
            txtConfirmPassword.TextChanged += txtConfirmPassword_TextChanged;
            // 
            // lblConfirmPassword
            // 
            lblConfirmPassword.AutoSize = true;
            lblConfirmPassword.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblConfirmPassword.ForeColor = Color.Black;
            lblConfirmPassword.Location = new Point(34, 338);
            lblConfirmPassword.Name = "lblConfirmPassword";
            lblConfirmPassword.Size = new Size(175, 25);
            lblConfirmPassword.TabIndex = 8;
            lblConfirmPassword.Text = "Confirm Password";
            // 
            // lblErrNewPassword
            // 
            lblErrNewPassword.AutoSize = true;
            lblErrNewPassword.Font = new Font("Segoe UI", 8F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblErrNewPassword.ForeColor = Color.Crimson;
            lblErrNewPassword.Location = new Point(34, 311);
            lblErrNewPassword.Name = "lblErrNewPassword";
            lblErrNewPassword.Size = new Size(0, 21);
            lblErrNewPassword.TabIndex = 18;
            // 
            // btnToggleNewPassword
            // 
            btnToggleNewPassword.Cursor = Cursors.Hand;
            btnToggleNewPassword.FlatAppearance.BorderSize = 0;
            btnToggleNewPassword.FlatStyle = FlatStyle.Flat;
            btnToggleNewPassword.Font = new Font("Segoe UI", 6.5F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btnToggleNewPassword.ForeColor = Color.Black;
            btnToggleNewPassword.Location = new Point(423, 283);
            btnToggleNewPassword.Name = "btnToggleNewPassword";
            btnToggleNewPassword.Size = new Size(30, 24);
            btnToggleNewPassword.TabIndex = 14;
            btnToggleNewPassword.TabStop = false;
            btnToggleNewPassword.Text = "👁";
            btnToggleNewPassword.UseVisualStyleBackColor = false;
            btnToggleNewPassword.Click += btnToggleNewPassword_Click;
            // 
            // txtNewPassword
            // 
            txtNewPassword.BorderStyle = BorderStyle.None;
            txtNewPassword.Location = new Point(37, 284);
            txtNewPassword.Name = "txtNewPassword";
            txtNewPassword.PlaceholderText = "Min 8 chars, letters & numbers";
            txtNewPassword.Size = new Size(382, 24);
            txtNewPassword.TabIndex = 3;
            txtNewPassword.UseSystemPasswordChar = true;
            txtNewPassword.TextChanged += txtNewPassword_TextChanged;
            // 
            // lblNewPassword
            // 
            lblNewPassword.AutoSize = true;
            lblNewPassword.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblNewPassword.ForeColor = Color.Black;
            lblNewPassword.Location = new Point(34, 256);
            lblNewPassword.Name = "lblNewPassword";
            lblNewPassword.Size = new Size(142, 25);
            lblNewPassword.TabIndex = 6;
            lblNewPassword.Text = "New Password";
            // 
            // lblErrEmail
            // 
            lblErrEmail.AutoSize = true;
            lblErrEmail.Font = new Font("Segoe UI", 8F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblErrEmail.ForeColor = Color.Crimson;
            lblErrEmail.Location = new Point(34, 229);
            lblErrEmail.Name = "lblErrEmail";
            lblErrEmail.Size = new Size(0, 21);
            lblErrEmail.TabIndex = 17;
            // 
            // txtEmail
            // 
            txtEmail.BorderStyle = BorderStyle.None;
            txtEmail.Location = new Point(37, 202);
            txtEmail.Name = "txtEmail";
            txtEmail.PlaceholderText = "name@domain.com";
            txtEmail.Size = new Size(416, 24);
            txtEmail.TabIndex = 2;
            txtEmail.TextChanged += txtEmail_TextChanged;
            // 
            // lblEmail
            // 
            lblEmail.AutoSize = true;
            lblEmail.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblEmail.ForeColor = Color.Black;
            lblEmail.Location = new Point(34, 174);
            lblEmail.Name = "lblEmail";
            lblEmail.Size = new Size(158, 25);
            lblEmail.TabIndex = 4;
            lblEmail.Text = "Registered Email";
            // 
            // lblErrUsername
            // 
            lblErrUsername.AutoSize = true;
            lblErrUsername.Font = new Font("Segoe UI", 8F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblErrUsername.ForeColor = Color.Crimson;
            lblErrUsername.Location = new Point(34, 147);
            lblErrUsername.Name = "lblErrUsername";
            lblErrUsername.Size = new Size(0, 21);
            lblErrUsername.TabIndex = 16;
            // 
            // txtUsername
            // 
            txtUsername.BorderStyle = BorderStyle.None;
            txtUsername.Location = new Point(37, 120);
            txtUsername.MaxLength = 20;
            txtUsername.Name = "txtUsername";
            txtUsername.PlaceholderText = "3-20 characters (a-z, 0-9, _)";
            txtUsername.Size = new Size(416, 24);
            txtUsername.TabIndex = 1;
            txtUsername.TextChanged += txtUsername_TextChanged;
            // 
            // lblUsername
            // 
            lblUsername.AutoSize = true;
            lblUsername.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblUsername.ForeColor = Color.Black;
            lblUsername.Location = new Point(34, 92);
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
            btnHelp.Location = new Point(444, 14);
            btnHelp.Name = "btnHelp";
            btnHelp.Size = new Size(32, 39);
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
            lblSubtitle.Location = new Point(110, 56);
            lblSubtitle.Name = "lblSubtitle";
            lblSubtitle.Size = new Size(381, 25);
            lblSubtitle.TabIndex = 1;
            lblSubtitle.Text = "Verify your account and set a new password";
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Font = new Font("Segoe UI", 17F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblTitle.ForeColor = Color.Black;
            lblTitle.Location = new Point(122, 14);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(352, 46);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "FORGOT PASSWORD";
            // 
            // panelShadow
            // 
            panelShadow.BackColor = Color.FromArgb(43, 27, 61);
            panelShadow.Location = new Point(371, 128);
            panelShadow.Name = "panelShadow";
            panelShadow.Size = new Size(490, 580);
            panelShadow.TabIndex = 3;
            // 
            // ForgotPasswordForm
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            BackgroundImage = Properties.Resources.bg;
            BackgroundImageLayout = ImageLayout.Stretch;
            ClientSize = new Size(1220, 760);
            Controls.Add(nightPanel1);
            Controls.Add(panelShadow);
            Controls.Add(pnlHeaderBar);
            Controls.Add(pnlTitleBar);
            ForeColor = Color.Transparent;
            FormBorderStyle = FormBorderStyle.None;
            Name = "ForgotPasswordForm";
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
        private Label lblEmail;
        private TextBox txtEmail;
        private Label lblErrEmail;
        private Label lblNewPassword;
        private TextBox txtNewPassword;
        private Button btnToggleNewPassword;
        private Label lblErrNewPassword;
        private Label lblConfirmPassword;
        private TextBox txtConfirmPassword;
        private Button btnToggleConfirmPassword;
        private Label lblErrConfirmPassword;
        private Button btnResetPassword;
        private Button btnCancel;
    }
}