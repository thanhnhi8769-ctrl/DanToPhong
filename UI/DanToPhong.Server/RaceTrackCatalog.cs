using System.Reflection;
using System.Text.Json;

namespace DanToPhong.Server;

internal sealed class RaceTrackCatalog
{
    private static readonly IReadOnlyDictionary<string, RaceTrack> Tracks = LoadTracks();

    internal static bool TryGet(string name, out RaceTrack track) =>
        Tracks.TryGetValue(name, out track!);

    private static IReadOnlyDictionary<string, RaceTrack> LoadTracks()
    {
        using Stream stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("DanToPhong.Server.RaceTrackCatalog.json")
            ?? throw new InvalidOperationException("Embedded race track catalog is missing.");
        RaceTrackCatalogFile catalog = JsonSerializer.Deserialize<RaceTrackCatalogFile>(stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("Embedded race track catalog is invalid.");

        var tracks = new Dictionary<string, RaceTrack>(StringComparer.OrdinalIgnoreCase);
        foreach (RaceTrack track in catalog.Tracks)
        {
            if (string.IsNullOrWhiteSpace(track.Name) || track.Spawns.Length == 0 ||
                track.Checkpoints.Length == 0 || !tracks.TryAdd(track.Name, track))
                throw new InvalidOperationException("Embedded race track catalog contains an invalid track.");
        }
        return tracks;
    }

    internal sealed class RaceTrackCatalogFile
    {
        public RaceTrack[] Tracks { get; set; } = Array.Empty<RaceTrack>();
    }

    internal sealed class RaceTrack
    {
        public string Name { get; set; } = string.Empty;
        public RaceSpawn[] Spawns { get; set; } = Array.Empty<RaceSpawn>();
        public RaceGate[] Checkpoints { get; set; } = Array.Empty<RaceGate>();
        public RaceGate Finish { get; set; } = new();
    }

    internal sealed class RaceSpawn
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float Yaw { get; set; }
    }

    internal sealed class RaceGate
    {
        public int Index { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float ExtentX { get; set; }
        public float ExtentY { get; set; }
        public float ExtentZ { get; set; }
    }
}
