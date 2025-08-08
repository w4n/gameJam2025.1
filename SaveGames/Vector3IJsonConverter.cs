using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

namespace Wancraft.SaveGames;

/// <summary>
///     A custom JSON converter for the <see cref="Vector3I"/> struct that enables serialization
///     and deserialization of a Vector3I instance to and from JSON.
/// </summary>
/// <remarks>
///     The Vector3I is serialized as a string in the format "X,Y,Z", where
///     X, Y, and Z are integer values representing its components. During deserialization, the
///     converter expects a similarly formatted string and will reconstruct the Vector3I instance.
///     If the format is invalid or the string is null, a default <see cref="Vector3I"/> with components
///     (0, 0, 0) is returned.
/// </remarks>
public sealed class Vector3IJsonConverter : JsonConverter<Vector3I>
{
    public override Vector3I Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var values = reader.GetString()?.Split(',');
        
        if (values == null)
            return default;
        
        return values.Length == 3 
            ? new Vector3I(int.Parse(values[0]), int.Parse(values[1]), int.Parse(values[2]))
            : new Vector3I(0, 0, 0);
    }

    public override void Write(Utf8JsonWriter writer, Vector3I value, JsonSerializerOptions options)
    {
        writer.WriteStringValue($"{value.X},{value.Y},{value.Z}");
    }
}