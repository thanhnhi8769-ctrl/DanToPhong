using System.Drawing;
using System.Windows.Forms;

namespace UI
{
    partial class PopupNoticeForm
    {
        private System.ComponentModel.IContainer? components = null;
        private Panel pnlTitleBar = null!;
        private Label lblBarTitle = null!;
        private Button btnMinimize = null!;
        private Button btnMaximize = null!;
        private Button btnClose = null!;
        private Panel panelShadow = null!;
        private Panel nightPanel1 = null!;
        private Panel pnlHeader = null!;
        private Label lblTitle = null!;
        private Label lblMessage = null!;
        private Button btnOk = null!;
        private Button btnYes = null!;
        private Button btnNo = null!;

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
            lblBarTitle = new Label();
            btnMinimize = new Button();
            btnMaximize = new Button();
            btnClose = new Button();
            panelShadow = new Panel();
            nightPanel1 = new Panel();
            lblMessage = new Label();
            btnOk = new Button();
            btnYes = new Button();
            btnNo = new Button();
            pnlHeader = new Panel();
            lblTitle = new Label();
            pnlTitleBar.SuspendLayout();
            nightPanel1.SuspendLayout();
            pnlHeader.SuspendLayout();
            SuspendLayout();
            // 
            // pnlTitleBar
            // 
            pnlTitleBar.BackColor = Color.FromArgb(26, 16, 38);
            pnlTitleBar.Controls.Add(lblBarTitle);
            pnlTitleBar.Controls.Add(btnMinimize);
            pnlTitleBar.Controls.Add(btnMaximize);
            pnlTitleBar.Controls.Add(btnClose);
            pnlTitleBar.Dock = DockStyle.Top;
            pnlTitleBar.Location = new Point(0, 0);
            pnlTitleBar.Name = "pnlTitleBar";
            pnlTitleBar.Size = new Size(520, 42);
            pnlTitleBar.TabIndex = 2;
            // 
            // lblBarTitle
            // 
            lblBarTitle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            lblBarTitle.ForeColor = Color.FromArgb(245, 208, 120);
            lblBarTitle.Location = new Point(12, 0);
            lblBarTitle.Name = "lblBarTitle";
            lblBarTitle.Size = new Size(370, 42);
            lblBarTitle.TabIndex = 0;
            lblBarTitle.Text = "SCOOTER RACING · NOTICE";
            lblBarTitle.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // btnMinimize
            // 
            btnMinimize.BackColor = Color.FromArgb(43, 27, 61);
            btnMinimize.FlatAppearance.BorderSize = 0;
            btnMinimize.FlatStyle = FlatStyle.Flat;
            btnMinimize.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnMinimize.ForeColor = Color.White;
            btnMinimize.Location = new Point(392, 6);
            btnMinimize.Name = "btnMinimize";
            btnMinimize.Size = new Size(36, 30);
            btnMinimize.TabIndex = 1;
            btnMinimize.Text = "-";
            btnMinimize.UseVisualStyleBackColor = false;
            // 
            // btnMaximize
            // 
            btnMaximize.BackColor = Color.FromArgb(43, 27, 61);
            btnMaximize.FlatAppearance.BorderSize = 0;
            btnMaximize.FlatStyle = FlatStyle.Flat;
            btnMaximize.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            btnMaximize.ForeColor = Color.White;
            btnMaximize.Location = new Point(432, 6);
            btnMaximize.Name = "btnMaximize";
            btnMaximize.Size = new Size(36, 30);
            btnMaximize.TabIndex = 2;
            btnMaximize.Text = "□";
            btnMaximize.UseVisualStyleBackColor = false;
            // 
            // btnClose
            // 
            btnClose.BackColor = Color.FromArgb(155, 17, 24);
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.FlatStyle = FlatStyle.Flat;
            btnClose.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            btnClose.ForeColor = Color.White;
            btnClose.Location = new Point(472, 6);
            btnClose.Name = "btnClose";
            btnClose.Size = new Size(36, 30);
            btnClose.TabIndex = 3;
            btnClose.Text = "X";
            btnClose.UseVisualStyleBackColor = false;
            // 
            // panelShadow
            // 
            panelShadow.BackColor = Color.FromArgb(24, 15, 36);
            panelShadow.Location = new Point(35, 62);
            panelShadow.Name = "panelShadow";
            panelShadow.Size = new Size(460, 195);
            panelShadow.TabIndex = 1;
            // 
            // nightPanel1
            // 
            nightPanel1.BackColor = Color.FromArgb(253, 251, 247);
            nightPanel1.Controls.Add(lblMessage);
            nightPanel1.Controls.Add(btnOk);
            nightPanel1.Controls.Add(btnYes);
            nightPanel1.Controls.Add(btnNo);
            nightPanel1.Controls.Add(pnlHeader);
            nightPanel1.Location = new Point(30, 57);
            nightPanel1.Name = "nightPanel1";
            nightPanel1.Size = new Size(460, 195);
            nightPanel1.TabIndex = 0;
            // 
            // lblMessage
            // 
            lblMessage.Font = new Font("Segoe UI", 9.5F);
            lblMessage.ForeColor = Color.FromArgb(30, 25, 38);
            lblMessage.Location = new Point(16, 54);
            lblMessage.Name = "lblMessage";
            lblMessage.Size = new Size(428, 82);
            lblMessage.TabIndex = 0;
            // 
            // btnOk
            // 
            btnOk.BackColor = Color.FromArgb(43, 27, 61);
            btnOk.DialogResult = DialogResult.OK;
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.FlatStyle = FlatStyle.Flat;
            btnOk.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnOk.ForeColor = Color.White;
            btnOk.Location = new Point(344, 144);
            btnOk.Name = "btnOk";
            btnOk.Size = new Size(100, 38);
            btnOk.TabIndex = 1;
            btnOk.Text = "OK";
            btnOk.UseVisualStyleBackColor = false;
            // 
            // btnYes
            // 
            btnYes.BackColor = Color.FromArgb(43, 27, 61);
            btnYes.DialogResult = DialogResult.Yes;
            btnYes.FlatAppearance.BorderSize = 0;
            btnYes.FlatStyle = FlatStyle.Flat;
            btnYes.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnYes.ForeColor = Color.White;
            btnYes.Location = new Point(344, 144);
            btnYes.Name = "btnYes";
            btnYes.Size = new Size(100, 38);
            btnYes.TabIndex = 2;
            btnYes.Text = "Yes";
            btnYes.UseVisualStyleBackColor = false;
            btnYes.Visible = false;
            // 
            // btnNo
            // 
            btnNo.BackColor = Color.FromArgb(235, 230, 240);
            btnNo.DialogResult = DialogResult.No;
            btnNo.FlatAppearance.BorderColor = Color.FromArgb(175, 165, 185);
            btnNo.FlatStyle = FlatStyle.Flat;
            btnNo.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnNo.ForeColor = Color.FromArgb(43, 27, 61);
            btnNo.Location = new Point(236, 144);
            btnNo.Name = "btnNo";
            btnNo.Size = new Size(98, 38);
            btnNo.TabIndex = 3;
            btnNo.Text = "No";
            btnNo.UseVisualStyleBackColor = false;
            btnNo.Visible = false;
            // 
            // pnlHeader
            // 
            pnlHeader.BackColor = Color.FromArgb(43, 27, 61);
            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Dock = DockStyle.Top;
            pnlHeader.Location = new Point(0, 0);
            pnlHeader.Name = "pnlHeader";
            pnlHeader.Size = new Size(460, 44);
            pnlHeader.TabIndex = 4;
            // 
            // lblTitle
            // 
            lblTitle.Dock = DockStyle.Fill;
            lblTitle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            lblTitle.ForeColor = Color.White;
            lblTitle.Location = new Point(0, 0);
            lblTitle.Name = "lblTitle";
            lblTitle.Padding = new Padding(16, 0, 16, 0);
            lblTitle.Size = new Size(460, 44);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "Notice";
            lblTitle.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // PopupNoticeForm
            // 
            AcceptButton = btnOk;
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(36, 22, 52);
            BackgroundImage = Properties.Resources.bg;
            BackgroundImageLayout = ImageLayout.Stretch;
            CancelButton = btnOk;
            ClientSize = new Size(520, 270);
            Controls.Add(nightPanel1);
            Controls.Add(panelShadow);
            Controls.Add(pnlTitleBar);
            FormBorderStyle = FormBorderStyle.None;
            Name = "PopupNoticeForm";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Notice";
            pnlTitleBar.ResumeLayout(false);
            nightPanel1.ResumeLayout(false);
            pnlHeader.ResumeLayout(false);
            ResumeLayout(false);
        }
    }
}