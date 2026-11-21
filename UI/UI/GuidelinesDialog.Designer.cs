namespace UI
{
    partial class GuidelinesDialog
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
            pnlTopBar = new Panel();
            lblBarBrand = new Label();
            btnTopClose = new Button();
            pnlCard = new Panel();
            lblDialogTitle = new Label();
            lblDialogSubtitle = new Label();
            lblRulesContent = new Label();
            btnClose = new Button();
            pnlShadow = new Panel();
            pnlTopBar.SuspendLayout();
            pnlCard.SuspendLayout();
            SuspendLayout();
            // 
            // pnlTopBar
            // 
            pnlTopBar.BackColor = Color.FromArgb(43, 27, 61);
            pnlTopBar.Controls.Add(lblBarBrand);
            pnlTopBar.Controls.Add(btnTopClose);
            pnlTopBar.Dock = DockStyle.Top;
            pnlTopBar.Name = "pnlTopBar";
            pnlTopBar.Size = new Size(540, 40);
            pnlTopBar.TabIndex = 0;
            pnlTopBar.MouseDown += pnlTopBar_MouseDown;
            // 
            // lblBarBrand
            // 
            lblBarBrand.AutoSize = true;
            lblBarBrand.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblBarBrand.ForeColor = Color.FromArgb(245, 208, 120);
            lblBarBrand.Location = new Point(16, 10);
            lblBarBrand.Name = "lblBarBrand";
            lblBarBrand.Size = new Size(158, 21);
            lblBarBrand.TabIndex = 0;
            lblBarBrand.Text = "INPUT GUIDELINES";
            // 
            // btnTopClose
            // 
            btnTopClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnTopClose.BackColor = Color.FromArgb(43, 27, 61);
            btnTopClose.FlatAppearance.BorderSize = 0;
            btnTopClose.FlatStyle = FlatStyle.Flat;
            btnTopClose.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnTopClose.ForeColor = Color.White;
            btnTopClose.Location = new Point(496, 5);
            btnTopClose.Name = "btnTopClose";
            btnTopClose.Size = new Size(34, 30);
            btnTopClose.TabIndex = 1;
            btnTopClose.TabStop = false;
            btnTopClose.Text = "✕";
            btnTopClose.UseVisualStyleBackColor = false;
            btnTopClose.Click += btnClose_Click;
            // 
            // pnlCard
            // 
            pnlCard.BackColor = Color.FromArgb(253, 251, 247);
            pnlCard.Controls.Add(lblDialogTitle);
            pnlCard.Controls.Add(lblDialogSubtitle);
            pnlCard.Controls.Add(lblRulesContent);
            pnlCard.Controls.Add(btnClose);
            pnlCard.Location = new Point(22, 54);
            pnlCard.Name = "pnlCard";
            pnlCard.Size = new Size(492, 340);
            pnlCard.TabIndex = 1;
            // 
            // lblDialogTitle
            // 
            lblDialogTitle.AutoSize = true;
            lblDialogTitle.Font = new Font("Segoe UI", 14F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblDialogTitle.ForeColor = Color.FromArgb(43, 27, 61);
            lblDialogTitle.Location = new Point(120, 14);
            lblDialogTitle.Name = "lblDialogTitle";
            lblDialogTitle.Size = new Size(250, 32);
            lblDialogTitle.TabIndex = 0;
            lblDialogTitle.Text = "FIELD REQUIREMENTS";
            // 
            // lblDialogSubtitle
            // 
            lblDialogSubtitle.AutoSize = true;
            lblDialogSubtitle.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblDialogSubtitle.ForeColor = Color.DimGray;
            lblDialogSubtitle.Location = new Point(110, 48);
            lblDialogSubtitle.Name = "lblDialogSubtitle";
            lblDialogSubtitle.Size = new Size(270, 20);
            lblDialogSubtitle.TabIndex = 1;
            lblDialogSubtitle.Text = "Follow these rules to submit cleanly";
            // 
            // lblRulesContent
            // 
            lblRulesContent.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblRulesContent.ForeColor = Color.Black;
            lblRulesContent.Location = new Point(24, 78);
            lblRulesContent.Name = "lblRulesContent";
            lblRulesContent.Size = new Size(444, 194);
            lblRulesContent.TabIndex = 2;
            lblRulesContent.Text = "";
            // 
            // btnClose
            // 
            btnClose.BackColor = Color.FromArgb(155, 17, 24);
            btnClose.Cursor = Cursors.Hand;
            btnClose.FlatAppearance.BorderColor = Color.FromArgb(43, 27, 61);
            btnClose.FlatAppearance.BorderSize = 2;
            btnClose.FlatStyle = FlatStyle.Flat;
            btnClose.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnClose.ForeColor = Color.White;
            btnClose.Location = new Point(166, 280);
            btnClose.Name = "btnClose";
            btnClose.Size = new Size(160, 42);
            btnClose.TabIndex = 3;
            btnClose.Text = "Got it";
            btnClose.UseVisualStyleBackColor = false;
            btnClose.Click += btnClose_Click;
            // 
            // pnlShadow
            // 
            pnlShadow.BackColor = Color.FromArgb(43, 27, 61);
            pnlShadow.Location = new Point(28, 60);
            pnlShadow.Name = "pnlShadow";
            pnlShadow.Size = new Size(492, 340);
            pnlShadow.TabIndex = 0;
            // 
            // GuidelinesDialog
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(245, 243, 247);
            ClientSize = new Size(540, 412);
            Controls.Add(pnlCard);
            Controls.Add(pnlShadow);
            Controls.Add(pnlTopBar);
            FormBorderStyle = FormBorderStyle.None;
            Name = "GuidelinesDialog";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            pnlTopBar.ResumeLayout(false);
            pnlTopBar.PerformLayout();
            pnlCard.ResumeLayout(false);
            pnlCard.PerformLayout();
            ResumeLayout(false);
        }

        #endregion
        private Panel pnlTopBar;
        private Label lblBarBrand;
        private Button btnTopClose;
        private Panel pnlCard;
        private Label lblDialogTitle;
        private Label lblDialogSubtitle;
        private Label lblRulesContent;
        private Button btnClose;
        private Panel pnlShadow;
    }
}