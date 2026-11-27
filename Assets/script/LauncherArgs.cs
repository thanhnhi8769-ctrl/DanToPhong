using System;
using UnityEngine;

public static class LauncherArgs
{
    public static string User = "player1";
    public static string Token = ReadAndClearSessionToken();
    public static string Host = Environment.GetEnvironmentVariable("DANTOPHONG_SERVER_HOST") ?? "";
    public static int Port = ParsePort(Environment.GetEnvironmentVariable("DANTOPHONG_SERVER_PORT"));
    public static bool UseTls = ParseBool(Environment.GetEnvironmentVariable("DANTOPHONG_USE_TLS"), true);
    public static string Mode = "single";
    public static string Room = "";
    public static string RoomMap = "";
    public static int SpawnIndex;
    public static bool IsRoomHost;
    public static bool IsSpectating;
    public static string RacerId = "";
    public static string FullName = "";
    public static bool HasLauncher;

    private static int ParsePort(string raw) => int.TryParse(raw, out int value) && value is > 0 and <= 65535 ? value : 9000;
    private static bool ParseBool(string raw, bool fallback) => bool.TryParse(raw, out bool value) ? value : fallback;

    private static string ReadAndClearSessionToken()
    {
        string token = Environment.GetEnvironmentVariable("DANTOPHONG_SESSION_TOKEN") ?? "";
        Environment.SetEnvironmentVariable("DANTOPHONG_SESSION_TOKEN", null);
        return token;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Parse()
    {
        string[] a = Environment.GetCommandLineArgs();
        for (int i = 0; i < a.Length - 1; i++)
        {
            switch (a[i])
            {
                case "--user":
                    User = a[i + 1];
                    HasLauncher = true;
                    break;
                case "--host": Host = a[++i]; break;
                case "--port": Port = ParsePort(a[++i]); break;
                case "--tls": UseTls = ParseBool(a[++i], true); break;
                case "--mode": Mode = a[++i]; break;
                case "--room": Room = a[++i]; break;
            }
        }
    }
}
