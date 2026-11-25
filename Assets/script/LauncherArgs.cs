using System;
using UnityEngine;

public static class LauncherArgs
{
    public static string User = "player1";
    public static string Token = "";
    public static string Host = "127.0.0.1";
    public static int Port = 9000;
    public static string Mode = "single";
    public static string Room = "";
    public static string RacerId = "";
    public static string FullName = "";
    public static bool HasLauncher;

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
                case "--token": Token = a[i + 1]; break;
                case "--host": Host = a[i + 1]; break;
                case "--port": int.TryParse(a[i + 1], out Port); break;
                case "--mode": Mode = a[i + 1]; break;
                case "--room": Room = a[i + 1]; break;
            }
        }
    }
}