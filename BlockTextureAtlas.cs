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
        
        _blockFaceDictionary[BlockType.Sand] = new Dictionary<QuadFace, Vector2>
        {
            { QuadFace.Front, new Vector2(2f, 5f) },
            { QuadFace.Back, new Vector2(2f, 5f) },
            { QuadFace.Left, new Vector2(2f, 5f) },
            { QuadFace.Right, new Vector2(2f, 5f) },
            { QuadFace.Top, new Vector2(2f, 5f) },
            { QuadFace.Bottom, new Vector2(2f, 5f) }
        };
        
        _blockFaceDictionary[BlockType.GoldOre] = new Dictionary<QuadFace, Vector2>
        {
            { QuadFace.Front, new Vector2(0f, 4f) },
            { QuadFace.Back, new Vector2(0f, 4f) },
            { QuadFace.Left, new Vector2(0f, 4f) },
            { QuadFace.Right, new Vector2(0f, 4f) },
            { QuadFace.Top, new Vector2(0f, 4f) },
            { QuadFace.Bottom, new Vector2(0f, 4f) }
        };
        
        _blockFaceDictionary[BlockType.CoalOre] = new Dictionary<QuadFace, Vector2>
        {
            { QuadFace.Front, new Vector2(0f, 0f) },
            { QuadFace.Back, new Vector2(0f, 0f) },
            { QuadFace.Left, new Vector2(0f, 0f) },
            { QuadFace.Right, new Vector2(0f, 0f) },
            { QuadFace.Top, new Vector2(0f, 0f) },
            { QuadFace.Bottom, new Vector2(0f, 0f) }
        };
        
        _blockFaceDictionary[BlockType.IronOre] = new Dictionary<QuadFace, Vector2>
        {
            { QuadFace.Front, new Vector2(0f, 1f) },
            { QuadFace.Back, new Vector2(0f, 1f) },
            { QuadFace.Left, new Vector2(0f, 1f) },
            { QuadFace.Right, new Vector2(0f, 1f) },
            { QuadFace.Top, new Vector2(0f, 1f) },
            { QuadFace.Bottom, new Vector2(0f, 1f) }
        };
        
        _blockFaceDictionary[BlockType.Water] = new Dictionary<QuadFace, Vector2>
        {
            { QuadFace.Front, new Vector2(2f, 6f) },
            { QuadFace.Back, new Vector2(2f, 6f) },
            { QuadFace.Left, new Vector2(2f, 6f) },
            { QuadFace.Right, new Vector2(2f, 6f) },
            { QuadFace.Top, new Vector2(2f, 6f) },
            { QuadFace.Bottom, new Vector2(2f, 6f) }
        };
        
        _blockFaceDictionary[BlockType.Wood] = new Dictionary<QuadFace, Vector2>
        {
            { QuadFace.Front, new Vector2(5f, 1f) },
            { QuadFace.Back, new Vector2(5f, 1f) },
            { QuadFace.Left, new Vector2(5f, 1f) },
            { QuadFace.Right, new Vector2(5f, 1f) },
            { QuadFace.Top, new Vector2(6f, 1f) },
            { QuadFace.Bottom, new Vector2(6f, 1f) }
        };
        
        _blockFaceDictionary[BlockType.Leaves] = new Dictionary<QuadFace, Vector2>
        {
            { QuadFace.Front, new Vector2(7f, 0f) },
            { QuadFace.Back, new Vector2(7f, 0f) },
            { QuadFace.Left, new Vector2(7f, 0f) },
            { QuadFace.Right, new Vector2(7f, 0f) },
            { QuadFace.Top, new Vector2(7f, 0f) },
            { QuadFace.Bottom, new Vector2(7f, 0f) }
        };
    }

    public static BlockTextureAtlas Instance
    {
        get { return _instance ??= new BlockTextureAtlas(); }
    }

    public Dictionary<QuadFace, Vector2> this[BlockType blockType] => _blockFaceDictionary[blockType];
}