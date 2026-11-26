using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace UI.Services
{
    public sealed class GameLauncherService
    {
        private const string GameExeName = "DanToPhong.exe";

        public string ResolveGameExePath()
        {
            string baseDir = AppContext.BaseDirectory;
            // Prefer the game packaged beside the launcher. Walking parent folders also
            // supports the current workspace layout where BuildGame36 is a sibling folder.
            var candidates = new System.Collections.Generic.List<string>
            {
                Path.Combine(baseDir, "Game", GameExeName),
                Path.Combine(baseDir, GameExeName)
            };
            DirectoryInfo? folder = new DirectoryInfo(baseDir);
            while (folder != null)
            {
                candidates.Add(Path.Combine(folder.FullName, "Game", GameExeName));
                candidates.Add(Path.Combine(folder.FullName, "BuildGame36", GameExeName));
                folder = folder.Parent;
            }

            foreach (string path in candidates)
            {
                string fullPath = Path.GetFullPath(path);
                if (File.Exists(fullPath))
                    return fullPath;
            }
            return Path.Combine(baseDir, "Game", GameExeName);
        }

        // MỚI: không truyền coins nữa, game tự lấy dữ liệu từ server bằng token.
        public bool LaunchGame(
            string username,
            string token,
            string host,
            int port,
            bool useTls,
            string mode,
            string roomId = "",
            Action? onGameExited = null)
        {
            string exePath = ResolveGameExePath();
            if (!File.Exists(exePath))
            {
                MessageBox.Show(
                    $"Không tìm thấy {GameExeName} trong thư mục Game cạnh launcher hoặc BuildGame36 gần launcher:\n{exePath}",
                    "Launcher - Thiếu file game",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return false;
            }

            string safeUser = string.IsNullOrWhiteSpace(username) ? "player1" : username.Trim();
            string safeMode = string.IsNullOrWhiteSpace(mode) ? "single" : mode.Trim();
            if (string.IsNullOrWhiteSpace(host) || port is < 1 or > 65535 || string.IsNullOrWhiteSpace(token))
            {
                MessageBox.Show("Thiếu host, port hoặc token hợp lệ từ phiên đăng nhập.", "Lỗi cấu hình kết nối",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            string workingDir = Path.GetDirectoryName(exePath) ?? AppContext.BaseDirectory;
            var psi = new ProcessStartInfo
            {
                FileName = exePath,
                WorkingDirectory = workingDir,
                UseShellExecute = false
            };
            psi.ArgumentList.Add("--user"); psi.ArgumentList.Add(safeUser);
            psi.ArgumentList.Add("--token"); psi.ArgumentList.Add(token);
            psi.ArgumentList.Add("--host"); psi.ArgumentList.Add(host);
            psi.ArgumentList.Add("--port"); psi.ArgumentList.Add(port.ToString());
            psi.ArgumentList.Add("--tls");
            psi.ArgumentList.Add(useTls ? "true" : "false");
            psi.ArgumentList.Add("--mode"); psi.ArgumentList.Add(safeMode);
            if (!string.IsNullOrWhiteSpace(roomId))
            {
                psi.ArgumentList.Add("--room"); psi.ArgumentList.Add(roomId.Trim());
            }

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
                        try
                        {
                            if (System.Threading.Interlocked.Exchange(ref notified, 1) == 0)
                                onGameExited();
                        }
                        finally { process.Dispose(); }
                    };
                }
                else
                {
                    process.Exited += (_, _) => process.Dispose();
                }

                if (!process.Start())
                {
                    process.Dispose();
                    throw new InvalidOperationException("Windows không xác nhận game đã khởi chạy.");
                }
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
