using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace UI.Services
{
    public sealed class GameLauncherService
    {
        private const string Build36Folder = @"D:\DanToPhong\BuildGame36";

        public string ResolveGameExePath()
        {
            string baseDir = AppContext.BaseDirectory;
            string[] candidates =
            {
                Path.Combine(Build36Folder, "DanToPhong.exe"),
                Path.Combine(Build36Folder, "ScooterRacing.exe"),
                Path.Combine(baseDir, "Game", "DanToPhong.exe"),
                Path.Combine(baseDir, "Game", "ScooterRacing.exe"),
                Path.Combine(baseDir, "DanToPhong.exe"),
                Path.Combine(baseDir, "ScooterRacing.exe")
            };

            foreach (string path in candidates)
            {
                string fullPath = Path.GetFullPath(path);
                if (File.Exists(fullPath))
                    return fullPath;
            }

            if (Directory.Exists(Build36Folder))
            {
                string? anyExe = Directory
                    .GetFiles(Build36Folder, "*.exe", SearchOption.TopDirectoryOnly)
                    .FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(anyExe))
                    return anyExe;
            }

            return candidates[0];
        }

        // MỚI: không truyền coins nữa, game tự lấy dữ liệu từ server bằng token.
        public bool LaunchGame(
            string username,
            string token,
            string host,
            int port,
            string mode,
            string roomId = "",
            Action? onGameExited = null)
        {
            string exePath = ResolveGameExePath();
            if (!File.Exists(exePath))
            {
                MessageBox.Show(
                    $"Không tìm thấy file game tại:\n{exePath}\n\nHãy kiểm tra thư mục {Build36Folder} có file .exe của game chưa.",
                    "Launcher - Thiếu file game",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return false;
            }

            string safeUser = string.IsNullOrWhiteSpace(username) ? "player1" : username.Trim();
            string safeMode = string.IsNullOrWhiteSpace(mode) ? "single" : mode.Trim();
            string args = $"--user \"{safeUser}\" --token {token} --host {host} --port {port} --mode {safeMode}";
            if (!string.IsNullOrWhiteSpace(roomId))
                args += $" --room \"{roomId.Trim()}\"";

            string workingDir = Path.GetDirectoryName(exePath) ?? Build36Folder;
            var psi = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = args,
                WorkingDirectory = workingDir,
                UseShellExecute = false
            };

            try
            {
                var process = new Process
                {
                    StartInfo = psi,
                    EnableRaisingEvents = true
                };

                if (onGameExited != null)
                {
                    int notified = 0;
                    process.Exited += (_, _) =>
                    {
                        if (System.Threading.Interlocked.Exchange(ref notified, 1) == 0)
                            onGameExited();
                    };
                }

                process.Start();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Không thể khởi chạy game: {ex.Message}",
                    "Lỗi Launcher",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }
        }
    }
}