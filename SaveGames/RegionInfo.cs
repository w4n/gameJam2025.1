using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Wancraft.SaveGames;

public record RegionInfo
{
    [JsonPropertyName("c")] public List<ChunkInfo> Chunks { get; set; } = [];
}