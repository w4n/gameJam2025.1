using System.Collections.Generic;
using System.Text.Json.Serialization;
using Godot;

namespace Wancraft.SaveGames;

public record ChunkInfo
{
    [JsonPropertyName("cp")] public Vector2I ChunkPosition { get; set; }

    [JsonPropertyName("b")] public List<BlockInfo> Blocks { get; set; } = [];
}