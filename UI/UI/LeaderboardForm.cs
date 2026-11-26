using System;
using System.Drawing;
using System.Windows.Forms;
using DanToPhong.Protocol;
using UI.Services;

namespace UI
{
    // Form tạo hoàn toàn bằng code, không cần Designer.
    public sealed class LeaderboardForm : Form
    {
        private readonly NetworkClientService _net;
        private readonly string _me;
        private readonly ListView _list = new();
        private readonly Label _status = new();

        public LeaderboardForm(NetworkClientService net, string currentUsername)
        {
            _net = net;
            _me = currentUsername;

            Text = "Leaderboard";
            AutoScaleMode = AutoScaleMode.Font;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(580, 540);
            MinimumSize = new Size(480, 360);
            BackColor = Color.FromArgb(253, 251, 247);

            var header = new Label
            {
                Text = "LEADERBOARD",
                Dock = DockStyle.Top,
                Height = 64,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = Color.FromArgb(43, 27, 61),
                BackColor = Color.FromArgb(245, 208, 120)
            };

            _status.Dock = DockStyle.Bottom;
            _status.Height = 32;
            _status.TextAlign = ContentAlignment.MiddleCenter;
            _status.Font = new Font("Segoe UI", 10F);
            _status.ForeColor = Color.FromArgb(110, 102, 118);

            _list.Dock = DockStyle.Fill;
            _list.View = View.Details;
            _list.FullRowSelect = true;
            _list.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            _list.Font = new Font("Segoe UI", 11F);
            _list.BorderStyle = BorderStyle.None;
            _list.BackColor = Color.FromArgb(253, 251, 247);
            _list.Columns.Add("#", 56);
            _list.Columns.Add("ID", 100);
            _list.Columns.Add("Player", 290);
            _list.Columns.Add("Coins", 110, HorizontalAlignment.Right);

            Controls.Add(_list);
            Controls.Add(_status);
            Controls.Add(header);

            Shown += async (_, _) => await TaiAsync();
        }

        private async System.Threading.Tasks.Task TaiAsync()
        {
            _status.Text = "Loading...";
            ResponsePacket r = await _net.GetLeaderboardAsync();
            if (IsDisposed) return;

            if (!r.Success || r.Leaderboard == null)
            {
                _status.Text = string.IsNullOrWhiteSpace(r.Message) ? "Cannot load leaderboard." : r.Message;
                return;
            }

            _list.BeginUpdate();
            _list.Items.Clear();
            foreach (LeaderboardEntryDto e in r.Leaderboard)
            {
                string name = string.IsNullOrWhiteSpace(e.FullName) || e.FullName == e.Username
                    ? e.Username
                    : $"{e.FullName} (@{e.Username})";

                var item = new ListViewItem(e.Rank.ToString());
                item.SubItems.Add(string.IsNullOrWhiteSpace(e.RacerId) ? "-" : e.RacerId);
                item.SubItems.Add(name);
                item.SubItems.Add(e.Coins.ToString("N0"));

                if (string.Equals(e.Username, _me, StringComparison.OrdinalIgnoreCase))
                {
                    item.BackColor = Color.FromArgb(252, 240, 200);
                    item.Font = new Font(_list.Font, FontStyle.Bold);
                }
                _list.Items.Add(item);
            }
            _list.EndUpdate();

            _status.Text = r.Leaderboard.Count == 0 ? "No players yet." : $"Top {r.Leaderboard.Count} by coins";
        }
    }
}
