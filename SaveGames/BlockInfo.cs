using System.Text.Json.Serialization;
using Godot;

namespace Wancraft.SaveGames;

public record BlockInfo
{
    [JsonPropertyName("bc")]
    public Vector3I BlockCoordinates { get; set; }
    
    [JsonPropertyName("bt")]
    public BlockType BlockType { get; set; }
}