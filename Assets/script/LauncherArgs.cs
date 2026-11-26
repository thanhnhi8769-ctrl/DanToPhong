using System;
using UnityEngine;

public static class LauncherArgs
{
    public static string User = "player1";
    public static string Token = "";
    public static string Host = Environment.GetEnvironmentVariable("DANTOPHONG_SERVER_HOST") ?? "";
    public static int Port = ParsePort(Environment.GetEnvironmentVariable("DANTOPHONG_SERVER_PORT"));
    public static string Mode = "single";
    public static string Room = "";
    public static string RacerId = "";
    public static string FullName = "";
    public static bool HasLauncher;

    private static int ParsePort(string raw) => int.TryParse(raw, out int value) && value is > 0 and <= 65535 ? value : 9000;

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
                case "--token": Token = a[++i]; break;
                case "--host": Host = a[++i]; break;
                case "--port": Port = ParsePort(a[++i]); break;
                case "--mode": Mode = a[++i]; break;
                case "--room": Room = a[++i]; break;
            }
        }
    }
}
