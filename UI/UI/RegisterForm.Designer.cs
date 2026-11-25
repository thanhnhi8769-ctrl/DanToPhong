namespace UI
{
    partial class RegisterForm
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
            btnRegister = new Button();
            lblCodeStatus = new Label();
            btnSendCode = new Button();
            txtVerificationCode = new TextBox();
            lblVerificationCode = new Label();
            cmbGender = new ComboBox();
            lblGender = new Label();
            dtpBirthday = new DateTimePicker();
            lblBirthday = new Label();
            lblErrEmail = new Label();
            txtEmail = new TextBox();
            lblEmail = new Label();
            lblErrFullName = new Label();
            txtFullName = new TextBox();
            lblFullName = new Label();
            lblErrConfirmPassword = new Label();
            btnToggleConfirmPassword = new Button();
            txtConfirmPassword = new TextBox();
            lblConfirmPassword = new Label();
            lblErrPassword = new Label();
            btnTogglePassword = new Button();
            txtPassword = new TextBox();
            lblPassword = new Label();
            lblErrUsername = new Label();
            txtUsername = new TextBox();
            lblUsername = new Label();
            txtRacerId = new TextBox();
            lblRacerId = new Label();
            btnChooseAvatar = new Button();
            picAvatar = new PictureBox();
            btnHelp = new Button();
            lblSubtitle = new Label();
            lblTitle = new Label();
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
            pnlTitleBar.Size = new Size(1326, 43);
            pnlTitleBar.TabIndex = 0;
            pnlTitleBar.MouseDown += pnlTitleBar_MouseDown;
            // 
            // titellbl
            // 
            titellbl.AutoSize = true;
            titellbl.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            titellbl.ForeColor = Color.Indigo;
            titellbl.Location = new Point(23, 9);
            titellbl.Name = "titellbl";
            titellbl.Size = new Size(146, 25);
            titellbl.TabIndex = 0;
            titellbl.Text = "Scooter Racing";
            // 
            // btnMinimize
            // 
            btnMinimize.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnMinimize.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnMinimize.ForeColor = Color.Black;
            btnMinimize.Location = new Point(1154, 5);
            btnMinimize.Name = "btnMinimize";
            btnMinimize.Size = new Size(49, 32);
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
            btnMaximize.Location = new Point(1209, 5);
            btnMaximize.Name = "btnMaximize";
            btnMaximize.Size = new Size(49, 32);
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
            btnExit.Location = new Point(1265, 5);
            btnExit.Name = "btnExit";
            btnExit.Size = new Size(49, 32);
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
            pnlHeaderBar.Location = new Point(0, 43);
            pnlHeaderBar.Name = "pnlHeaderBar";
            pnlHeaderBar.Size = new Size(1326, 50);
            pnlHeaderBar.TabIndex = 1;
            // 
            // lblHeaderTitle
            // 
            lblHeaderTitle.AutoSize = true;
            lblHeaderTitle.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblHeaderTitle.ForeColor = Color.White;
            lblHeaderTitle.Location = new Point(23, 10);
            lblHeaderTitle.Name = "lblHeaderTitle";
            lblHeaderTitle.Size = new Size(219, 32);
            lblHeaderTitle.TabIndex = 0;
            lblHeaderTitle.Text = "SCOOTER RACING";
            // 
            // lblHeaderBadge
            // 
            lblHeaderBadge.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblHeaderBadge.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblHeaderBadge.ForeColor = Color.FromArgb(245, 208, 120);
            lblHeaderBadge.Location = new Point(1034, 11);
            lblHeaderBadge.Name = "lblHeaderBadge";
            lblHeaderBadge.Size = new Size(267, 27);
            lblHeaderBadge.TabIndex = 1;
            lblHeaderBadge.Text = "NEW RACER";
            lblHeaderBadge.TextAlign = ContentAlignment.MiddleRight;
            // 
            // nightPanel1
            // 
            nightPanel1.Anchor = AnchorStyles.None;
            nightPanel1.Controls.Add(btnCancel);
            nightPanel1.Controls.Add(btnRegister);
            nightPanel1.Controls.Add(lblCodeStatus);
            nightPanel1.Controls.Add(btnSendCode);
            nightPanel1.Controls.Add(txtVerificationCode);
            nightPanel1.Controls.Add(lblVerificationCode);
            nightPanel1.Controls.Add(cmbGender);
            nightPanel1.Controls.Add(lblGender);
            nightPanel1.Controls.Add(dtpBirthday);
            nightPanel1.Controls.Add(lblBirthday);
            nightPanel1.Controls.Add(lblErrEmail);
            nightPanel1.Controls.Add(txtEmail);
            nightPanel1.Controls.Add(lblEmail);
            nightPanel1.Controls.Add(lblErrFullName);
            nightPanel1.Controls.Add(txtFullName);
            nightPanel1.Controls.Add(lblFullName);
            nightPanel1.Controls.Add(lblErrConfirmPassword);
            nightPanel1.Controls.Add(btnToggleConfirmPassword);
            nightPanel1.Controls.Add(txtConfirmPassword);
            nightPanel1.Controls.Add(lblConfirmPassword);
            nightPanel1.Controls.Add(lblErrPassword);
            nightPanel1.Controls.Add(btnTogglePassword);
            nightPanel1.Controls.Add(txtPassword);
            nightPanel1.Controls.Add(lblPassword);
            nightPanel1.Controls.Add(lblErrUsername);
            nightPanel1.Controls.Add(txtUsername);
            nightPanel1.Controls.Add(lblUsername);
            nightPanel1.Controls.Add(txtRacerId);
            nightPanel1.Controls.Add(lblRacerId);
            nightPanel1.Controls.Add(btnChooseAvatar);
            nightPanel1.Controls.Add(picAvatar);
            nightPanel1.Controls.Add(btnHelp);
            nightPanel1.Controls.Add(lblSubtitle);
            nightPanel1.Controls.Add(lblTitle);
            nightPanel1.ForeColor = Color.FromArgb(250, 250, 250);
            nightPanel1.LeftSideColor = Color.FromArgb(253, 251, 247);
            nightPanel1.Location = new Point(200, 105);
            nightPanel1.Name = "nightPanel1";
            nightPanel1.RightSideColor = Color.FromArgb(253, 251, 247);
            nightPanel1.Side = ReaLTaiizor.Controls.NightPanel.PanelSide.Left;
            nightPanel1.Size = new Size(926, 659);
            nightPanel1.TabIndex = 2;
            // 
            // btnCancel
            // 
            btnCancel.BackColor = Color.White;
            btnCancel.Cursor = Cursors.Hand;
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(43, 27, 61);
            btnCancel.FlatAppearance.BorderSize = 2;
            btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnCancel.ForeColor = Color.Black;
            btnCancel.Location = new Point(737, 545);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(139, 50);
            btnCancel.TabIndex = 11;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = false;
            btnCancel.Click += btnCancel_Click;
            // 
            // btnRegister
            // 
            btnRegister.BackColor = Color.FromArgb(155, 17, 24);
            btnRegister.Cursor = Cursors.Hand;
            btnRegister.FlatAppearance.BorderColor = Color.FromArgb(43, 27, 61);
            btnRegister.FlatAppearance.BorderSize = 2;
            btnRegister.FlatStyle = FlatStyle.Flat;
            btnRegister.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnRegister.ForeColor = Color.White;
            btnRegister.Location = new Point(51, 545);
            btnRegister.Name = "btnRegister";
            btnRegister.Size = new Size(674, 50);
            btnRegister.TabIndex = 10;
            btnRegister.Text = "Register";
            btnRegister.UseVisualStyleBackColor = false;
            btnRegister.Click += btnRegister_Click;
            // 
            // lblCodeStatus
            // 
            lblCodeStatus.AutoSize = true;
            lblCodeStatus.Font = new Font("Segoe UI", 7.5F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblCodeStatus.ForeColor = Color.ForestGreen;
            lblCodeStatus.Location = new Point(486, 493);
            lblCodeStatus.Name = "lblCodeStatus";
            lblCodeStatus.Size = new Size(0, 20);
            lblCodeStatus.TabIndex = 31;
            // 
            // btnSendCode
            // 
            btnSendCode.BackColor = Color.FromArgb(43, 27, 61);
            btnSendCode.Cursor = Cursors.Hand;
            btnSendCode.FlatAppearance.BorderSize = 0;
            btnSendCode.FlatStyle = FlatStyle.Flat;
            btnSendCode.Font = new Font("Segoe UI", 8F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnSendCode.ForeColor = Color.White;
            btnSendCode.Location = new Point(764, 451);
            btnSendCode.Name = "btnSendCode";
            btnSendCode.Size = new Size(112, 34);
            btnSendCode.TabIndex = 9;
            btnSendCode.Text = "Get Code";
            btnSendCode.UseVisualStyleBackColor = false;
            btnSendCode.Click += btnSendCode_Click;
            // 
            // txtVerificationCode
            // 
            txtVerificationCode.BackColor = Color.FromArgb(253, 251, 247);
            txtVerificationCode.BorderStyle = BorderStyle.None;
            txtVerificationCode.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            txtVerificationCode.ForeColor = Color.Black;
            txtVerificationCode.Location = new Point(486, 461);
            txtVerificationCode.MaxLength = 6;
            txtVerificationCode.Name = "txtVerificationCode";
            txtVerificationCode.PlaceholderText = "Enter 6-digit code";
            txtVerificationCode.Size = new Size(267, 24);
            txtVerificationCode.TabIndex = 8;
            txtVerificationCode.TextChanged += txtVerificationCode_TextChanged;
            // 
            // lblVerificationCode
            // 
            lblVerificationCode.AutoSize = true;
            lblVerificationCode.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblVerificationCode.ForeColor = Color.Black;
            lblVerificationCode.Location = new Point(486, 434);
            lblVerificationCode.Name = "lblVerificationCode";
            lblVerificationCode.Size = new Size(148, 23);
            lblVerificationCode.TabIndex = 29;
            lblVerificationCode.Text = "Verification Code";
            // 
            // cmbGender
            // 
            cmbGender.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbGender.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            cmbGender.FormattingEnabled = true;
            cmbGender.Items.AddRange(new object[] { "Male", "Female", "Other" });
            cmbGender.Location = new Point(301, 459);
            cmbGender.Name = "cmbGender";
            cmbGender.Size = new Size(139, 33);
            cmbGender.TabIndex = 7;
            // 
            // lblGender
            // 
            lblGender.AutoSize = true;
            lblGender.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblGender.ForeColor = Color.Black;
            lblGender.Location = new Point(301, 434);
            lblGender.Name = "lblGender";
            lblGender.Size = new Size(68, 23);
            lblGender.TabIndex = 28;
            lblGender.Text = "Gender";
            // 
            // dtpBirthday
            // 
            dtpBirthday.CustomFormat = "dd/MM/yyyy";
            dtpBirthday.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            dtpBirthday.Format = DateTimePickerFormat.Custom;
            dtpBirthday.Location = new Point(51, 459);
            dtpBirthday.Name = "dtpBirthday";
            dtpBirthday.Size = new Size(238, 31);
            dtpBirthday.TabIndex = 6;
            // 
            // lblBirthday
            // 
            lblBirthday.AutoSize = true;
            lblBirthday.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblBirthday.ForeColor = Color.Black;
            lblBirthday.Location = new Point(51, 434);
            lblBirthday.Name = "lblBirthday";
            lblBirthday.Size = new Size(79, 23);
            lblBirthday.TabIndex = 12;
            lblBirthday.Text = "Birthday";
            // 
            // lblErrEmail
            // 
            lblErrEmail.AutoSize = true;
            lblErrEmail.Font = new Font("Segoe UI", 7.5F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblErrEmail.ForeColor = Color.Crimson;
            lblErrEmail.Location = new Point(486, 407);
            lblErrEmail.Name = "lblErrEmail";
            lblErrEmail.Size = new Size(0, 20);
            lblErrEmail.TabIndex = 24;
            // 
            // txtEmail
            // 
            txtEmail.BackColor = Color.FromArgb(253, 251, 247);
            txtEmail.BorderStyle = BorderStyle.None;
            txtEmail.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            txtEmail.ForeColor = Color.Black;
            txtEmail.Location = new Point(486, 377);
            txtEmail.Name = "txtEmail";
            txtEmail.PlaceholderText = "name@domain.com";
            txtEmail.Size = new Size(389, 24);
            txtEmail.TabIndex = 5;
            txtEmail.TextChanged += txtEmail_TextChanged;
            // 
            // lblEmail
            // 
            lblEmail.AutoSize = true;
            lblEmail.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblEmail.ForeColor = Color.Black;
            lblEmail.Location = new Point(486, 352);
            lblEmail.Name = "lblEmail";
            lblEmail.Size = new Size(54, 23);
            lblEmail.TabIndex = 10;
            lblEmail.Text = "Email";
            // 
            // lblErrFullName
            // 
            lblErrFullName.AutoSize = true;
            lblErrFullName.Font = new Font("Segoe UI", 7.5F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblErrFullName.ForeColor = Color.Crimson;
            lblErrFullName.Location = new Point(486, 330);
            lblErrFullName.Name = "lblErrFullName";
            lblErrFullName.Size = new Size(0, 20);
            lblErrFullName.TabIndex = 23;
            // 
            // txtFullName
            // 
            txtFullName.BackColor = Color.FromArgb(253, 251, 247);
            txtFullName.BorderStyle = BorderStyle.None;
            txtFullName.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            txtFullName.ForeColor = Color.Black;
            txtFullName.Location = new Point(486, 300);
            txtFullName.MaxLength = 100;
            txtFullName.Name = "txtFullName";
            txtFullName.PlaceholderText = "2-100 characters";
            txtFullName.Size = new Size(389, 24);
            txtFullName.TabIndex = 4;
            txtFullName.TextChanged += txtFullName_TextChanged;
            // 
            // lblFullName
            // 
            lblFullName.AutoSize = true;
            lblFullName.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblFullName.ForeColor = Color.Black;
            lblFullName.Location = new Point(486, 275);
            lblFullName.Name = "lblFullName";
            lblFullName.Size = new Size(91, 23);
            lblFullName.TabIndex = 8;
            lblFullName.Text = "Full Name";
            // 
            // lblErrConfirmPassword
            // 
            lblErrConfirmPassword.AutoSize = true;
            lblErrConfirmPassword.Font = new Font("Segoe UI", 7.5F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblErrConfirmPassword.ForeColor = Color.Crimson;
            lblErrConfirmPassword.Location = new Point(51, 407);
            lblErrConfirmPassword.Name = "lblErrConfirmPassword";
            lblErrConfirmPassword.Size = new Size(0, 20);
            lblErrConfirmPassword.TabIndex = 22;
            // 
            // btnToggleConfirmPassword
            // 
            btnToggleConfirmPassword.BackColor = Color.FromArgb(253, 251, 247);
            btnToggleConfirmPassword.Cursor = Cursors.Hand;
            btnToggleConfirmPassword.FlatAppearance.BorderSize = 0;
            btnToggleConfirmPassword.FlatStyle = FlatStyle.Flat;
            btnToggleConfirmPassword.Font = new Font("Segoe UI Emoji", 8F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btnToggleConfirmPassword.ForeColor = Color.Black;
            btnToggleConfirmPassword.Location = new Point(412, 374);
            btnToggleConfirmPassword.Name = "btnToggleConfirmPassword";
            btnToggleConfirmPassword.Padding = new Padding(0, 1, 0, 0);
            btnToggleConfirmPassword.Size = new Size(25, 27);
            btnToggleConfirmPassword.TabIndex = 16;
            btnToggleConfirmPassword.TabStop = false;
            btnToggleConfirmPassword.Text = "👁";
            btnToggleConfirmPassword.UseVisualStyleBackColor = false;
            btnToggleConfirmPassword.Click += btnToggleConfirmPassword_Click;
            // 
            // txtConfirmPassword
            // 
            txtConfirmPassword.BackColor = Color.FromArgb(253, 251, 247);
            txtConfirmPassword.BorderStyle = BorderStyle.None;
            txtConfirmPassword.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            txtConfirmPassword.ForeColor = Color.Black;
            txtConfirmPassword.Location = new Point(51, 377);
            txtConfirmPassword.Name = "txtConfirmPassword";
            txtConfirmPassword.PlaceholderText = "Re-enter password";
            txtConfirmPassword.Size = new Size(355, 24);
            txtConfirmPassword.TabIndex = 3;
            txtConfirmPassword.UseSystemPasswordChar = true;
            txtConfirmPassword.TextChanged += txtConfirmPassword_TextChanged;
            // 
            // lblConfirmPassword
            // 
            lblConfirmPassword.AutoSize = true;
            lblConfirmPassword.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblConfirmPassword.ForeColor = Color.Black;
            lblConfirmPassword.Location = new Point(51, 352);
            lblConfirmPassword.Name = "lblConfirmPassword";
            lblConfirmPassword.Size = new Size(156, 23);
            lblConfirmPassword.TabIndex = 6;
            lblConfirmPassword.Text = "Confirm Password";
            // 
            // lblErrPassword
            // 
            lblErrPassword.AutoSize = true;
            lblErrPassword.Font = new Font("Segoe UI", 7.5F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblErrPassword.ForeColor = Color.Crimson;
            lblErrPassword.Location = new Point(51, 330);
            lblErrPassword.Name = "lblErrPassword";
            lblErrPassword.Size = new Size(0, 20);
            lblErrPassword.TabIndex = 21;
            // 
            // btnTogglePassword
            // 
            btnTogglePassword.BackColor = Color.FromArgb(253, 251, 247);
            btnTogglePassword.Cursor = Cursors.Hand;
            btnTogglePassword.FlatAppearance.BorderSize = 0;
            btnTogglePassword.FlatStyle = FlatStyle.Flat;
            btnTogglePassword.Font = new Font("Segoe UI Emoji", 8F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btnTogglePassword.ForeColor = Color.Black;
            btnTogglePassword.Location = new Point(412, 298);
            btnTogglePassword.Name = "btnTogglePassword";
            btnTogglePassword.Padding = new Padding(0, 1, 0, 0);
            btnTogglePassword.Size = new Size(25, 27);
            btnTogglePassword.TabIndex = 15;
            btnTogglePassword.TabStop = false;
            btnTogglePassword.Text = "👁";
            btnTogglePassword.UseVisualStyleBackColor = false;
            btnTogglePassword.Click += btnTogglePassword_Click;
            // 
            // txtPassword
            // 
            txtPassword.BackColor = Color.FromArgb(253, 251, 247);
            txtPassword.BorderStyle = BorderStyle.None;
            txtPassword.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            txtPassword.ForeColor = Color.Black;
            txtPassword.Location = new Point(51, 300);
            txtPassword.Name = "txtPassword";
            txtPassword.PlaceholderText = "Min 8 chars, letters & numbers";
            txtPassword.Size = new Size(355, 24);
            txtPassword.TabIndex = 2;
            txtPassword.UseSystemPasswordChar = true;
            txtPassword.TextChanged += txtPassword_TextChanged;
            // 
            // lblPassword
            // 
            lblPassword.AutoSize = true;
            lblPassword.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblPassword.ForeColor = Color.Black;
            lblPassword.Location = new Point(51, 275);
            lblPassword.Name = "lblPassword";
            lblPassword.Size = new Size(85, 23);
            lblPassword.TabIndex = 4;
            lblPassword.Text = "Password";
            // 
            // lblErrUsername
            // 
            lblErrUsername.AutoSize = true;
            lblErrUsername.Font = new Font("Segoe UI", 7.5F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblErrUsername.ForeColor = Color.Crimson;
            lblErrUsername.Location = new Point(486, 252);
            lblErrUsername.Name = "lblErrUsername";
            lblErrUsername.Size = new Size(0, 20);
            lblErrUsername.TabIndex = 20;
            // 
            // txtUsername
            // 
            txtUsername.BackColor = Color.FromArgb(253, 251, 247);
            txtUsername.BorderStyle = BorderStyle.None;
            txtUsername.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            txtUsername.ForeColor = Color.Black;
            txtUsername.Location = new Point(486, 223);
            txtUsername.MaxLength = 20;
            txtUsername.Name = "txtUsername";
            txtUsername.PlaceholderText = "3-20 characters (a-z, 0-9, _)";
            txtUsername.Size = new Size(389, 24);
            txtUsername.TabIndex = 1;
            txtUsername.TextChanged += txtUsername_TextChanged;
            // 
            // lblUsername
            // 
            lblUsername.AutoSize = true;
            lblUsername.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblUsername.ForeColor = Color.Black;
            lblUsername.Location = new Point(486, 198);
            lblUsername.Name = "lblUsername";
            lblUsername.Size = new Size(89, 23);
            lblUsername.TabIndex = 2;
            lblUsername.Text = "Username";
            // 
            // txtRacerId
            // 
            txtRacerId.BackColor = Color.FromArgb(253, 251, 247);
            txtRacerId.BorderStyle = BorderStyle.None;
            txtRacerId.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            txtRacerId.ForeColor = Color.FromArgb(43, 27, 61);
            txtRacerId.Location = new Point(51, 223);
            txtRacerId.Name = "txtRacerId";
            txtRacerId.ReadOnly = true;
            txtRacerId.Size = new Size(389, 26);
            txtRacerId.TabIndex = 27;
            txtRacerId.TabStop = false;
            txtRacerId.Text = "000001";
            // 
            // lblRacerId
            // 
            lblRacerId.AutoSize = true;
            lblRacerId.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblRacerId.ForeColor = Color.Black;
            lblRacerId.Location = new Point(51, 198);
            lblRacerId.Name = "lblRacerId";
            lblRacerId.Size = new Size(77, 23);
            lblRacerId.TabIndex = 26;
            lblRacerId.Text = "Racer ID";
            // 
            // btnChooseAvatar
            // 
            btnChooseAvatar.BackColor = Color.FromArgb(43, 27, 61);
            btnChooseAvatar.Cursor = Cursors.Hand;
            btnChooseAvatar.FlatAppearance.BorderSize = 0;
            btnChooseAvatar.FlatStyle = FlatStyle.Flat;
            btnChooseAvatar.Font = new Font("Segoe UI", 8F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnChooseAvatar.ForeColor = Color.White;
            btnChooseAvatar.Location = new Point(394, 152);
            btnChooseAvatar.Name = "btnChooseAvatar";
            btnChooseAvatar.Size = new Size(139, 30);
            btnChooseAvatar.TabIndex = 0;
            btnChooseAvatar.Text = "Choose Avatar";
            btnChooseAvatar.UseVisualStyleBackColor = false;
            btnChooseAvatar.Click += btnChooseAvatar_Click;
            // 
            // picAvatar
            // 
            picAvatar.BackColor = Color.FromArgb(240, 236, 246);
            picAvatar.Cursor = Cursors.Hand;
            picAvatar.Location = new Point(432, 77);
            picAvatar.Name = "picAvatar";
            picAvatar.Size = new Size(63, 68);
            picAvatar.SizeMode = PictureBoxSizeMode.Zoom;
            picAvatar.TabIndex = 25;
            picAvatar.TabStop = false;
            picAvatar.Click += btnChooseAvatar_Click;
            picAvatar.Paint += picAvatar_Paint;
            // 
            // btnHelp
            // 
            btnHelp.Cursor = Cursors.Hand;
            btnHelp.FlatAppearance.BorderColor = Color.FromArgb(43, 27, 61);
            btnHelp.FlatStyle = FlatStyle.Flat;
            btnHelp.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnHelp.ForeColor = Color.FromArgb(43, 27, 61);
            btnHelp.Location = new Point(876, 14);
            btnHelp.Name = "btnHelp";
            btnHelp.Size = new Size(34, 34);
            btnHelp.TabIndex = 30;
            btnHelp.TabStop = false;
            btnHelp.Text = "?";
            btnHelp.UseVisualStyleBackColor = false;
            btnHelp.Click += btnHelp_Click;
            // 
            // lblSubtitle
            // 
            lblSubtitle.AutoSize = true;
            lblSubtitle.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblSubtitle.ForeColor = Color.DimGray;
            lblSubtitle.Location = new Point(363, 50);
            lblSubtitle.Name = "lblSubtitle";
            lblSubtitle.Size = new Size(219, 23);
            lblSubtitle.TabIndex = 1;
            lblSubtitle.Text = "Create a new racer account";
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Font = new Font("Segoe UI", 15F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblTitle.ForeColor = Color.Black;
            lblTitle.Location = new Point(395, 11);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(156, 41);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "REGISTER";
            // 
            // panelShadow
            // 
            panelShadow.BackColor = Color.FromArgb(43, 27, 61);
            panelShadow.Location = new Point(206, 111);
            panelShadow.Name = "panelShadow";
            panelShadow.Size = new Size(926, 659);
            panelShadow.TabIndex = 3;
            // 
            // RegisterForm
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            BackgroundImage = Properties.Resources.bg;
            BackgroundImageLayout = ImageLayout.Stretch;
            ClientSize = new Size(1326, 790);
            Controls.Add(nightPanel1);
            Controls.Add(panelShadow);
            Controls.Add(pnlHeaderBar);
            Controls.Add(pnlTitleBar);
            ForeColor = Color.Transparent;
            FormBorderStyle = FormBorderStyle.None;
            Name = "RegisterForm";
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
        private Button btnChooseAvatar;
        private Label lblRacerId;
        private TextBox txtRacerId;
        private Label lblUsername;
        private TextBox txtUsername;
        private Label lblErrUsername;
        private Label lblPassword;
        private TextBox txtPassword;
        private Button btnTogglePassword;
        private Label lblErrPassword;
        private Label lblConfirmPassword;
        private TextBox txtConfirmPassword;
        private Button btnToggleConfirmPassword;
        private Label lblErrConfirmPassword;
        private Label lblFullName;
        private TextBox txtFullName;
        private Label lblErrFullName;
        private Label lblEmail;
        private TextBox txtEmail;
        private Label lblErrEmail;
        private Label lblBirthday;
        private DateTimePicker dtpBirthday;
        private Label lblGender;
        private ComboBox cmbGender;
        private Label lblVerificationCode;
        private TextBox txtVerificationCode;
        private Button btnSendCode;
        private Label lblCodeStatus;
        private Button btnRegister;
        private Button btnCancel;
    }
}