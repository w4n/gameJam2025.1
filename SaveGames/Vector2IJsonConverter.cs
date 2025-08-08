using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

namespace Wancraft.SaveGames;

/// <summary>
///     A custom JSON converter for serializing and deserializing instances of <see cref="Godot.Vector2I"/>.
///     This converter converts a <see cref="Godot.Vector2I"/> object into a string representation
///     and parses a string back into a <see cref="Godot.Vector2I"/> object using the format "X,Y".
/// </summary>
public sealed class Vector2IJsonConverter : JsonConverter<Vector2I>
{
    public override Vector2I Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var values = reader.GetString()?.Split(',');
        
        if (values == null)
            return default;
        
        return values.Length == 2 
            ? new Vector2I(int.Parse(values[0]), int.Parse(values[1]))
            : new Vector2I(0, 0);
    }

    public override void Write(Utf8JsonWriter writer, Vector2I value, JsonSerializerOptions options)
    {
        writer.WriteStringValue($"{value.X},{value.Y}");
    }
}