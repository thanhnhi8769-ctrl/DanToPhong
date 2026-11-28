using System;

namespace DanToPhong.Protocol
{
    public static class MapNames
    {
        public const string Game1 = "game1";
        public const string Game2 = "game2";
        public const string Game3 = "game3";
        public const string Default = Game1;

        public static bool IsAllowed(string? mapName)
        {
            return string.Equals(mapName, Game1, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(mapName, Game2, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(mapName, Game3, StringComparison.OrdinalIgnoreCase);
        }

        public static string Normalize(string? mapName)
        {
            if (string.Equals(mapName, Game1, StringComparison.OrdinalIgnoreCase)) return Game1;
            if (string.Equals(mapName, Game2, StringComparison.OrdinalIgnoreCase)) return Game2;
            if (string.Equals(mapName, Game3, StringComparison.OrdinalIgnoreCase)) return Game3;
            return Default;
        }
    }
}
