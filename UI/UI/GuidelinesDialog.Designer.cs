using System.Drawing;
using System.Windows.Forms;

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
            btnTopMinimize = new Button();
            btnTopMaximize = new Button();
            btnTopClose = new Button();
            pnlCard = new Panel();
            lblDialogTitle = new Label();
            lblDialogSubtitle = new Label();
            pnlRulesScroll = new Panel();
            lblRulesContent = new Label();
            btnClose = new Button();
            pnlShadow = new Panel();

            pnlTopBar.SuspendLayout();
            pnlCard.SuspendLayout();
            pnlRulesScroll.SuspendLayout();
            SuspendLayout();

            // pnlTopBar
            pnlTopBar.BackColor = Color.FromArgb(43, 27, 61);
            pnlTopBar.Controls.Add(lblBarBrand);
            pnlTopBar.Controls.Add(btnTopMinimize);
            pnlTopBar.Controls.Add(btnTopMaximize);
            pnlTopBar.Controls.Add(btnTopClose);
            pnlTopBar.Dock = DockStyle.Top;
            pnlTopBar.Name = "pnlTopBar";
            pnlTopBar.Size = new Size(660, 38);
            pnlTopBar.TabIndex = 0;
            pnlTopBar.MouseDown += pnlTopBar_MouseDown;

            // lblBarBrand
            lblBarBrand.AutoSize = true;
            lblBarBrand.Font = new System.Drawing.Font("Segoe UI", 9.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblBarBrand.ForeColor = Color.FromArgb(245, 208, 120);
            lblBarBrand.Location = new Point(16, 9);
            lblBarBrand.Name = "lblBarBrand";
            lblBarBrand.Size = new Size(158, 21);
            lblBarBrand.TabIndex = 0;
            lblBarBrand.Text = "INPUT GUIDELINES";
            lblBarBrand.MouseDown += lblBarBrand_MouseDown;

            // btnTopMinimize
            btnTopMinimize.BackColor = Color.FromArgb(43, 27, 61);
            btnTopMinimize.FlatAppearance.BorderSize = 0;
            btnTopMinimize.FlatStyle = FlatStyle.Flat;
            btnTopMinimize.Font = new System.Drawing.Font("Segoe UI", 9.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnTopMinimize.ForeColor = Color.White;
            btnTopMinimize.Location = new Point(542, 4);
            btnTopMinimize.Name = "btnTopMinimize";
            btnTopMinimize.Size = new Size(34, 30);
            btnTopMinimize.TabIndex = 1;
            btnTopMinimize.TabStop = false;
            btnTopMinimize.Text = "-";
            btnTopMinimize.UseVisualStyleBackColor = false;
            btnTopMinimize.Click += btnTopMinimize_Click;

            // btnTopMaximize
            btnTopMaximize.BackColor = Color.FromArgb(43, 27, 61);
            btnTopMaximize.FlatAppearance.BorderSize = 0;
            btnTopMaximize.FlatStyle = FlatStyle.Flat;
            btnTopMaximize.Font = new System.Drawing.Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnTopMaximize.ForeColor = Color.White;
            btnTopMaximize.Location = new Point(582, 4);
            btnTopMaximize.Name = "btnTopMaximize";
            btnTopMaximize.Size = new Size(34, 30);
            btnTopMaximize.TabIndex = 2;
            btnTopMaximize.TabStop = false;
            btnTopMaximize.Text = "□";
            btnTopMaximize.UseVisualStyleBackColor = false;
            btnTopMaximize.Click += btnTopMaximize_Click;

            // btnTopClose
            btnTopClose.BackColor = Color.FromArgb(155, 17, 24);
            btnTopClose.FlatAppearance.BorderSize = 0;
            btnTopClose.FlatStyle = FlatStyle.Flat;
            btnTopClose.Font = new System.Drawing.Font("Segoe UI", 9.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnTopClose.ForeColor = Color.White;
            btnTopClose.Location = new Point(622, 4);
            btnTopClose.Name = "btnTopClose";
            btnTopClose.Size = new Size(34, 30);
            btnTopClose.TabIndex = 3;
            btnTopClose.TabStop = false;
            btnTopClose.Text = "X";
            btnTopClose.UseVisualStyleBackColor = false;
            btnTopClose.Click += btnClose_Click;

            // pnlCard
            pnlCard.BackColor = Color.FromArgb(253, 251, 247);
            pnlCard.Controls.Add(lblDialogTitle);
            pnlCard.Controls.Add(lblDialogSubtitle);
            pnlCard.Controls.Add(pnlRulesScroll);
            pnlCard.Controls.Add(btnClose);
            pnlCard.Location = new Point(64, 52);
            pnlCard.Name = "pnlCard";
            pnlCard.Size = new Size(532, 410);
            pnlCard.TabIndex = 1;

            // lblDialogTitle
            lblDialogTitle.AutoSize = true;
            lblDialogTitle.Font = new System.Drawing.Font("Segoe UI", 14F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblDialogTitle.ForeColor = Color.FromArgb(43, 27, 61);
            lblDialogTitle.Location = new Point(140, 14);
            lblDialogTitle.Name = "lblDialogTitle";
            lblDialogTitle.Size = new Size(250, 32);
            lblDialogTitle.TabIndex = 0;
            lblDialogTitle.Text = "FIELD REQUIREMENTS";

            // lblDialogSubtitle
            lblDialogSubtitle.AutoSize = true;
            lblDialogSubtitle.Font = new System.Drawing.Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblDialogSubtitle.ForeColor = Color.DimGray;
            lblDialogSubtitle.Location = new Point(130, 48);
            lblDialogSubtitle.Name = "lblDialogSubtitle";
            lblDialogSubtitle.Size = new Size(270, 20);
            lblDialogSubtitle.TabIndex = 1;
            lblDialogSubtitle.Text = "Follow these rules to submit cleanly";

            // pnlRulesScroll
            pnlRulesScroll.AutoScroll = true;
            pnlRulesScroll.BackColor = Color.FromArgb(253, 251, 247);
            pnlRulesScroll.Controls.Add(lblRulesContent);
            pnlRulesScroll.Location = new Point(22, 76);
            pnlRulesScroll.Name = "pnlRulesScroll";
            pnlRulesScroll.Size = new Size(488, 260);
            pnlRulesScroll.TabIndex = 2;

            // lblRulesContent
            lblRulesContent.AutoSize = true;
            lblRulesContent.Font = new System.Drawing.Font("Segoe UI", 9.2F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblRulesContent.ForeColor = Color.Black;
            lblRulesContent.Location = new Point(2, 2);
            lblRulesContent.MaximumSize = new Size(462, 0);
            lblRulesContent.Name = "lblRulesContent";
            lblRulesContent.Size = new Size(0, 0);
            lblRulesContent.TabIndex = 0;
            lblRulesContent.Text = "";

            // btnClose
            btnClose.BackColor = Color.FromArgb(155, 17, 24);
            btnClose.Cursor = Cursors.Hand;
            btnClose.FlatAppearance.BorderColor = Color.FromArgb(43, 27, 61);
            btnClose.FlatAppearance.BorderSize = 2;
            btnClose.FlatStyle = FlatStyle.Flat;
            btnClose.Font = new System.Drawing.Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnClose.ForeColor = Color.White;
            btnClose.Location = new Point(186, 350);
            btnClose.Name = "btnClose";
            btnClose.Size = new Size(160, 42);
            btnClose.TabIndex = 3;
            btnClose.Text = "Got it";
            btnClose.UseVisualStyleBackColor = false;
            btnClose.Click += btnClose_Click;

            // pnlShadow
            pnlShadow.BackColor = Color.FromArgb(43, 27, 61);
            pnlShadow.Location = new Point(70, 58);
            pnlShadow.Name = "pnlShadow";
            pnlShadow.Size = new Size(532, 410);
            pnlShadow.TabIndex = 0;

            // GuidelinesDialog
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(245, 243, 247);
            ClientSize = new Size(660, 482);
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
            pnlRulesScroll.ResumeLayout(false);
            pnlRulesScroll.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Panel pnlTopBar;
        private Label lblBarBrand;
        private Button btnTopMinimize;
        private Button btnTopMaximize;
        private Button btnTopClose;
        private Panel pnlCard;
        private Label lblDialogTitle;
        private Label lblDialogSubtitle;
        private Panel pnlRulesScroll;
        private Label lblRulesContent;
        private Button btnClose;
        private Panel pnlShadow;
    }
}