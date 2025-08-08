using System;
using System.Collections.Generic;
using Godot;

namespace Wancraft;

/// <summary>
///     Represents a singleton texture atlas used to map block types and their respective
///     face textures to UV coordinates in a texture sheet.
/// </summary>
public class BlockTextureAtlas
{
    private static BlockTextureAtlas _instance;
    private readonly Dictionary<BlockType, Dictionary<QuadFace, Vector2>> _blockFaceDictionary = new();

    private BlockTextureAtlas()
    {
        foreach (var blockType in Enum.GetValues<BlockType>())
            _blockFaceDictionary.Add(blockType, new Dictionary<QuadFace, Vector2>());

        _blockFaceDictionary[BlockType.Dirt] = new Dictionary<QuadFace, Vector2>
        {
            { QuadFace.Front, new Vector2(3f, 1f) },
            { QuadFace.Back, new Vector2(3f, 1f) },
            { QuadFace.Left, new Vector2(3f, 1f) },
            { QuadFace.Right, new Vector2(3f, 1f) },
            { QuadFace.Top, new Vector2(3f, 1f) },
            { QuadFace.Bottom, new Vector2(3f, 1f) }
        };

        _blockFaceDictionary[BlockType.Grass] = new Dictionary<QuadFace, Vector2>
        {
            { QuadFace.Front, new Vector2(3f, 0f) },
            { QuadFace.Back, new Vector2(3f, 0f) },
            { QuadFace.Left, new Vector2(3f, 0f) },
            { QuadFace.Right, new Vector2(3f, 0f) },
            { QuadFace.Top, new Vector2(4f, 0f) },
            { QuadFace.Bottom, new Vector2(3f, 1f) }
        };

        _blockFaceDictionary[BlockType.Rock] = new Dictionary<QuadFace, Vector2>
        {
            { QuadFace.Front, new Vector2(1f, 0f) },
            { QuadFace.Back, new Vector2(1f, 0f) },
            { QuadFace.Left, new Vector2(1f, 0f) },
            { QuadFace.Right, new Vector2(1f, 0f) },
            { QuadFace.Top, new Vector2(1f, 0f) },
            { QuadFace.Bottom, new Vector2(1f, 0f) }
        };
    }

    public static BlockTextureAtlas Instance
    {
        get { return _instance ??= new BlockTextureAtlas(); }
    }

    public Dictionary<QuadFace, Vector2> this[BlockType blockType] => _blockFaceDictionary[blockType];
}