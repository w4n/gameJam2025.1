using System;
using System.Collections.Generic;
using Godot;
using Wancraft;

public partial class Chunk : Node3D
{
    [Export] public int ChunkSize { get; set; } = 16;
    [Export] public int TerrainHeight { get; set; } = 128;
    
    [Export] public WorldGenerator WorldGenerator { get; set; }
    
    [Export] private AnimationPlayer _player;
    [Export] private CollisionShape3D _collisionShape;
    [Export] private MeshInstance3D _meshInstance;
    
    public bool SuppressFirstSceneEntryAnimation { get; set; }
    public bool SuppressUpdates { get; set; }
    public bool Finalized { get; set; }
    public StandardMaterial3D Material { get; set; }
    
    public bool FlaggedForRemoval { get; set; }
    public DateTime RemoveAfter { get; set; } = DateTime.MaxValue;
    
    private BlockType[,,] _originalBlockMap;
    private BlockType[,,] _blockMap;
    private ArrayMesh _chunkMesh;
    
    public Vector2I ChunkCoordinates { get; private set; }
    
    private Vector2 _textureAtlasSize = new Vector2(9, 7);
    public Dictionary<Vector3I, BlockType> PlayerBlocks { get; private set; } = new();
    
    public override void _EnterTree()
    {
        if (SuppressFirstSceneEntryAnimation)
            SuppressFirstSceneEntryAnimation = false;
        else
            _player.Play("fade_in");
        
        base._EnterTree();
    }

    public void UpdateChunk()
    {
        foreach (var (coords, blockType) in PlayerBlocks)
            _blockMap[coords.X, coords.Y, coords.Z] = blockType;
        
        GenerateChunk(ChunkCoordinates);
        FinalizeChunk();
    }

    public void RemoveBlock(Vector3I blockCoordinates)
    {
        if (!PlayerInventory.Instance.TryAddBlock(_blockMap[blockCoordinates.X, blockCoordinates.Y, blockCoordinates.Z]))
            return;
        
        _blockMap[blockCoordinates.X, blockCoordinates.Y, blockCoordinates.Z] = BlockType.Air;
        
        // Regenerate mesh and collision shape
        GenerateChunk(ChunkCoordinates);
        FinalizeChunk();
        
        UpdatePlayerBlocks(blockCoordinates, BlockType.Air);
    }

    public void PlaceBlock(Vector3I blockCoordinates, BlockType blockType)
    {
        _blockMap[blockCoordinates.X, blockCoordinates.Y, blockCoordinates.Z] = blockType;
        
        // Regenerate mesh and collision shape
        GenerateChunk(ChunkCoordinates);
        FinalizeChunk();
        
        UpdatePlayerBlocks(blockCoordinates, blockType);
    }
    
    private void UpdatePlayerBlocks(Vector3I blockCoordinates, BlockType blockType)
    {
        if (_originalBlockMap[blockCoordinates.X, blockCoordinates.Y, blockCoordinates.Z] == blockType)
        {   
            GD.Print($"Remove player block {blockCoordinates.X}, {blockCoordinates.Y}, {blockCoordinates.Z}");
            PlayerBlocks.Remove(blockCoordinates);
        }
        else
        {
            GD.Print($"Add player block {blockCoordinates.X}, {blockCoordinates.Y}, {blockCoordinates.Z}");
            //var myBlockCoordinates = blockCoordinates.ToMyVector3();
            if (PlayerBlocks.TryAdd(blockCoordinates, blockType))
                PlayerBlocks[blockCoordinates] = blockType;
        }
    }
    
    public void GenerateChunk(Vector2I coordinates)
    {
        var timestamp = DateTime.Now;

        if (_blockMap == null)
        {
            _blockMap = WorldGenerator.GenerateBlockMap(coordinates.X * ChunkSize, coordinates.Y * ChunkSize, ChunkSize);
            _originalBlockMap = (BlockType[,,])_blockMap.Clone();

            if (PlayerBlocks.Count > 0)
            {
                foreach (var (blockCoordinates, playerBlock) in PlayerBlocks)
                    _blockMap[blockCoordinates.X, blockCoordinates.Y, blockCoordinates.Z] = playerBlock;
            }
        }
        
        ChunkCoordinates  = coordinates;
        
        var surface = new SurfaceTool();
        surface.Begin(Mesh.PrimitiveType.Triangles);
        
        for (var x = 0; x < ChunkSize; x++)
        for (var z = 0; z < ChunkSize; z++)
        for (var y = 0; y < TerrainHeight; y++)
        {
            if (!IsBlockAt(x, y, z))
                continue;

            if (IsBlockVisible(x, y, z))
                AddBlockToSurface(surface, x, y, z);
        }

        surface.SetMaterial(Material);
        _chunkMesh = surface.Commit();
        _meshInstance.Mesh = _chunkMesh;
        
        var generationTime = DateTime.Now - timestamp;
    }

    /// <summary>
    ///     Finalizes the chunk by applying its collision mesh and marking it as finalized.
    /// </summary>
    /// <remarks>
    ///     This method is NOT thread-safe, due to the use of Godot's <see cref="ArrayMesh.CreateTrimeshShape"/> API.
    /// </remarks>
    public void FinalizeChunk()
    {
        _collisionShape.Shape = _chunkMesh.CreateTrimeshShape();
        Finalized = true;
    }
    
    private bool IsBlockVisible(int x, int y, int z)
    {
        return !IsBlockAt(x, y - 1, z) || !IsBlockAt(x, y + 1, z) ||
               !IsBlockAt(x - 1, y, z) || !IsBlockAt(x + 1, y, z) || 
               !IsBlockAt(x, y, z - 1) || !IsBlockAt(x, y, z + 1);
    }
    
    private bool IsBlockAt(int x, int y, int z)
    {
        if (x < 0 || x >= ChunkSize || 
            y < 0 || y >= TerrainHeight || 
            z < 0 || z >= ChunkSize)
            return false;
        
        return _blockMap[x, y, z] != BlockType.Air;
    }

    private Dictionary<QuadFace, Vector2> GetBlockUVs(int x, int y, int z)
    {
        return BlockTextureAtlas.Instance[_blockMap[x, y, z]];
    }
    
    /*
     *       4 +------------+ 5
     *-       /|           /|
     *-      / |          / |
     *-     /  |         /  |
     *   0 +---|--------+ 1 |
     *     | 7 +--------|---+ 6
     *     |  /         |  /
     *     | /          | /      y z
     *     |/           |/       |/
     *   3 +------------+ 2    x-+
     */
    
    private void AddBlockToSurface(SurfaceTool surface, int x, int y, int z)
    {
        var half = 0.5f * 1f;
        
        var vertices = new Vector3[]
        {
            new(-half + x,  half + y,  half + z),  // 0: Top-left-front
            new( half + x,  half + y,  half + z),  // 1: Top-right-front
            new( half + x, -half + y,  half + z),  // 2: Bottom-right-front
            new(-half + x, -half + y,  half + z),  // 3: Bottom-left-front
            new(-half + x,  half + y, -half + z),  // 4: Top-left-back
            new( half + x,  half + y, -half + z),  // 5: Top-right-back
            new( half + x, -half + y, -half + z),  // 6: Bottom-right-back
            new(-half + x, -half + y, -half + z),  // 7: Bottom-left-back
        };
        
        var blockUVs = GetBlockUVs(x, y, z);
        
        // block above?
        if (!IsBlockAt(x, y + 1, z))
            // Add top face
            AddQuad(surface, vertices[4], vertices[5], vertices[1], vertices[0], Vector3.Up, blockUVs[QuadFace.Top]);
        
        // block below?
        if (!IsBlockAt(x, y - 1, z))
            // Add bottom face
            AddQuad(surface, vertices[3], vertices[2], vertices[6], vertices[7], Vector3.Down, blockUVs[QuadFace.Bottom]);
        
        // block left?
        if (!IsBlockAt(x - 1, y, z))
            // Add left face
            AddQuad(surface, vertices[4], vertices[0], vertices[3], vertices[7], Vector3.Left, blockUVs[QuadFace.Left]);
        
        // block right?
        if (!IsBlockAt(x + 1, y, z))
            // Add left face
            AddQuad(surface, vertices[1], vertices[5], vertices[6], vertices[2], Vector3.Right, blockUVs[QuadFace.Right]);
        
        // block in front?
        if (!IsBlockAt(x, y, z + 1))
            // Add front face
            AddQuad(surface, vertices[0], vertices[1], vertices[2], vertices[3], Vector3.Back, blockUVs[QuadFace.Front]);
        
        // block behind?
        if (!IsBlockAt(x, y, z - 1))
            // Add back face
            AddQuad(surface, vertices[5], vertices[4], vertices[7], vertices[6], Vector3.Forward, blockUVs[QuadFace.Back]);
        
        //surface.AddTriangleFan([vertices[0], vertices[1], vertices[2]], );
    }
    
    private void AddQuad(SurfaceTool surfaceTool, 
        Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3,
        Vector3 normal, Vector2 offset)
    {
        var uvOffset = offset / _textureAtlasSize;
        var uvHeight = 1.0f / _textureAtlasSize.Y;
        var uvWidth = 1.0f / _textureAtlasSize.X;
        
        var uvs = new Vector2[]
        {
            new Vector2(0, 0) + uvOffset,               // Top-left
            new Vector2(uvWidth, 0) + uvOffset,         // Top-right
            new Vector2(uvWidth, uvHeight) + uvOffset,  // Bottom-right
            new Vector2(0, uvHeight) + uvOffset,        // Bottom-left
        };
        
        // First triangle: v0, v1, v2
        surfaceTool.SetNormal(normal);
        surfaceTool.SetUV(uvs[0]);
        surfaceTool.AddVertex(v0);
        
        surfaceTool.SetNormal(normal);
        surfaceTool.SetUV(uvs[1]);
        surfaceTool.AddVertex(v1);
        
        surfaceTool.SetNormal(normal);
        surfaceTool.SetUV(uvs[2]);
        surfaceTool.AddVertex(v2);
        
        // Second triangle: v0, v2, v3
        surfaceTool.SetNormal(normal);
        surfaceTool.SetUV(uvs[0]);
        surfaceTool.AddVertex(v0);
        
        surfaceTool.SetNormal(normal);
        surfaceTool.SetUV(uvs[2]);
        surfaceTool.AddVertex(v2);
        
        surfaceTool.SetNormal(normal);
        surfaceTool.SetUV(uvs[3]);
        surfaceTool.AddVertex(v3);
    }
}
