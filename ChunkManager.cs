using Godot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Wancraft;

public partial class ChunkManager : Node
{
    private int _maxThreads;

    [Export] public Player Player { get; set; }
    [Export] private WorldGenerator generator;
    [Export] public int BlockSize = 1;
    [Export] public int ChunkSize = 16;
    [Export] public int WorldHeight = 128;
    [Export] public int GenerationRadius = 5;
    [Export] public PackedScene ChunkScene;
    [Export] public int WorkerThreads = 4;
    
    private readonly ConcurrentDictionary<Vector2I, Chunk> _loadedChunks = new();
    private readonly ConcurrentDictionary<Vector2I, Chunk> _cachedChunks = new();
    
    private readonly ConcurrentQueue<Vector2I> _chunksToGenerate = new();
    private readonly ConcurrentQueue<Chunk> _chunksToFinalize = new();
    private readonly ConcurrentQueue<Chunk> _chunksToAddToScene = new();
    private readonly ConcurrentQueue<Chunk> _chunksToRemoveFromScene = new();
    
    private readonly SemaphoreSlim _physicsGenerationSemaphore = new(1, 1);
    private readonly SemaphoreSlim _generationSemaphore = new(1, 1);
    
    private PackedScene _chunkScene;
    
    public override void _Ready()
    {
        _maxThreads = OS.GetProcessorCount();
        _chunkScene = GD.Load<PackedScene>("res://Chunk.tscn");
        
        UpdateLoadedChunks(Vector2I.Zero, suppressSceneEntryAnimation: true);
    }

    public void OnBlockPlaced(Vector3I blockCoordinates)
    {
        var chunkCoordinates = GetChunkCoordinates(blockCoordinates);
        var localizedBlockCoordinates = GetLocalizedBlockCoordinates(chunkCoordinates, blockCoordinates);
        
#if DEBUG
        GD.Print($"Chunk coord:\tX: {chunkCoordinates.X}\tY: {chunkCoordinates.Y}");
        GD.Print($"Block (world):\tX: {blockCoordinates.X}\tY: {blockCoordinates.Y}\tZ: {blockCoordinates.Z}");
        GD.Print($"Block (chunk):\tX: {localizedBlockCoordinates.X}\tY: {blockCoordinates.Y}\tZ: {localizedBlockCoordinates.Z}");
#endif
        
        if (_loadedChunks.TryGetValue(chunkCoordinates, out var chunk))
            chunk.PlaceBlock(localizedBlockCoordinates, BlockType.Dirt);
    }
    
    public void OnBlockMined(Vector3I blockCoordinates)
    {
        var chunkCoordinates = GetChunkCoordinates(blockCoordinates);
        var localizedBlockCoordinates = GetLocalizedBlockCoordinates(chunkCoordinates, blockCoordinates);
        
#if DEBUG
        GD.Print($"Chunk coord:\tX: {chunkCoordinates.X}\tY: {chunkCoordinates.Y}");
        GD.Print($"Block (world):\tX: {blockCoordinates.X}\tY: {blockCoordinates.Y}\tZ: {blockCoordinates.Z}");
        GD.Print($"Block (chunk):\tX: {localizedBlockCoordinates.X}\tY: {blockCoordinates.Y}\tZ: {localizedBlockCoordinates.Z}");
#endif
        
        if (_loadedChunks.TryGetValue(chunkCoordinates, out var chunk))
            chunk.RemoveBlock(localizedBlockCoordinates);
    }

    private Vector3I GetLocalizedBlockCoordinates(Vector2I chunkCoordinates, Vector3I blockCoordinates)
    {
        var x = blockCoordinates.X > 0 
            ? blockCoordinates.X - chunkCoordinates.X * ChunkSize 
            : (chunkCoordinates.X * ChunkSize + blockCoordinates.X * -1) * -1;
        
        var z = blockCoordinates.Z > 0
            ? blockCoordinates.Z - chunkCoordinates.Y * ChunkSize
            : (chunkCoordinates.Y * ChunkSize + blockCoordinates.Z * -1) * -1;
        
        return new Vector3I(x, blockCoordinates.Y, z);
    }
    
    private Vector2I GetChunkCoordinates(Vector3I blockCoordinates)
    {
        var chunkXCoordinate = blockCoordinates.X >= 0
            ? (int)MathF.Floor((blockCoordinates.X) / (float)ChunkSize)
            : (int)MathF.Ceiling((blockCoordinates.X + 1) / (float)ChunkSize);
        
        var chunkYCoordinate = blockCoordinates.Z >= 0
            ? (int)MathF.Floor((blockCoordinates.Z) / (float)ChunkSize)
            : (int)MathF.Ceiling((blockCoordinates.Z +1) / (float)ChunkSize);

        if (blockCoordinates.X < 0)
            // Negative offset of -1 since we begin negative chunks at -1, while the first positive chunk is at 0
            chunkXCoordinate--;
        
        if (blockCoordinates.Z < 0)
            // Negative offset of -1 since we begin negative chunks at -1, while the first positive chunk is at 0
            chunkYCoordinate--;
        
        return new Vector2I(chunkXCoordinate, chunkYCoordinate);
    }

    public override void _PhysicsProcess(double delta)
    {
        int addedCount = 0;
        int removedCount = 0;
        
        // Adding chunks here instead of _Process results in smoother transitions
        while (_chunksToAddToScene.TryDequeue(out var chunk))
        {   
            if (!chunk.Finalized) chunk.FinalizeChunk();
            AddChild(chunk);
            addedCount++;
        }
        
        while (_chunksToRemoveFromScene.TryDequeue(out var chunk))
        {
            RemoveChild(chunk);
            removedCount++;
            GD.Print($"Removed chunk at position: {chunk.Position}");
        }

        if (addedCount > 0 || removedCount > 0)
            GD.Print($"Added: {addedCount}, Removed: {removedCount}, Total children: {GetChildCount()}");
        
        GetPlayerPosition();
    }

    private Vector2I _lastPlayerChunkPosition = Vector2I.Zero;
    
    private void GetPlayerPosition()
    {
        if ((int)Player.Position.X / ChunkSize != _lastPlayerChunkPosition.X ||
            (int)Player.Position.Z / ChunkSize != _lastPlayerChunkPosition.Y)
        {
            _lastPlayerChunkPosition.X = (int)Player.Position.X / ChunkSize;
            _lastPlayerChunkPosition.Y = (int)Player.Position.Z / ChunkSize;
            
            UpdateLoadedChunks(_lastPlayerChunkPosition);
        }
    }
    
    private void UpdateLoadedChunks(Vector2I playerPosition, bool suppressSceneEntryAnimation = false)
    {
        var chunksToLoad = GetChunksInRangeRadius(playerPosition);
        //GD.Print($"{DateTime.Now}: Loaded {chunksToLoad.Count} chunks");
        
        HashSet<Vector2I> loadedChunks;
        lock (_loadedChunks)
        {
            loadedChunks = new HashSet<Vector2I>(_loadedChunks.Keys);
        }
        var chunksToUnload = loadedChunks.Except(GetChunksInRangeRadiusPlusTwo(playerPosition)).ToList();

        foreach (var chunkPosition in chunksToUnload)
        {
            if (!_loadedChunks.TryRemove(chunkPosition, out var chunk)) 
                continue;
            
            if (_cachedChunks.TryAdd(chunkPosition, chunk))
                _chunksToRemoveFromScene.Enqueue(chunk);
        }
        
        foreach (var chunkPosition in chunksToLoad.Where(chunkPosition => !_loadedChunks.ContainsKey(chunkPosition)))
        {
            if (_cachedChunks.TryGetValue(chunkPosition, out var chunk))
            {
                if (_loadedChunks.TryAdd(chunkPosition, chunk) && _cachedChunks.Remove(chunkPosition, out var _))
                    _chunksToAddToScene.Enqueue(chunk);
                continue;
            }
            
            _chunksToGenerate.Enqueue(chunkPosition);
        }
        
         if (_chunksToGenerate.Count > 0)
              _ = Task.Run(async () => await GenerateChunks(suppressSceneEntryAnimation)); 

    }
    
    private async Task GenerateChunks(bool suppressSceneEntryAnimation = false)
    {
        // Collect all chunks to generate into a list
        var tasks = new List<Task>();
    
        // Collect all chunks to generate
        var chunksToProcess = new List<Vector2I>();
        while (_chunksToGenerate.TryDequeue(out var chunkPosition))
        {
            chunksToProcess.Add(chunkPosition);
        }

        foreach (var chunkCoordinates in chunksToProcess)
        {
            var task = Task.Run(async () =>
            {
                await _generationSemaphore.WaitAsync();
                try
                {
                    LoadChunk(chunkCoordinates, suppressSceneEntryAnimation);
                }
                finally
                {
                    _generationSemaphore.Release();
                }
            });
            tasks.Add(task);
        }
        
        await Task.WhenAll(tasks);
        
        /*while (_chunksToGenerate.Count > 0)
        {
            if (_chunksToGenerate.TryDequeue(out var chunkCoordinates))
                LoadChunk(chunkCoordinates);
        }*/
    }

    private void LoadChunk(Vector2I chunkPosition, bool suppressSceneEntryAnimation = false)
    {
        // Check again to prevent duplicates
        if (_loadedChunks.ContainsKey(chunkPosition))
            return;
        
        var chunk = (Chunk)_chunkScene.Instantiate();
        
        if (_loadedChunks.TryAdd(chunkPosition, chunk))
        {
            GD.Print($"Generated new chunk at: {chunkPosition}");
        }
        else
        {
            // Duplicate detected - clean up
            chunk.QueueFree();
            GD.Print($"Duplicate chunk generation prevented at: {chunkPosition}");
        }

        chunk.SuppressFirstSceneEntryAnimation = suppressSceneEntryAnimation;
        chunk.ChunkSize = ChunkSize;
        chunk.TerrainHeight = WorldHeight;
        chunk.WorldGenerator = generator;
        chunk.Position = new Vector3(chunkPosition.X * ChunkSize, 0, chunkPosition.Y * ChunkSize);
        chunk.GenerateChunk(chunkPosition);
        
        // Todo: Maybe split this step into it's own physics generation thread
        if (!chunk.Finalized)
            chunk.FinalizeChunk();
        
        _chunksToAddToScene.Enqueue(chunk);
    }

    private HashSet<Vector2I> GetChunksInRangeRadius(Vector2I playerPosition)
    {
        var chunks = new HashSet<Vector2I>();
        
        for (int x = playerPosition.X - GenerationRadius; x < playerPosition.X + GenerationRadius; x++)
        for (int y = playerPosition.Y - GenerationRadius; y < playerPosition.Y + GenerationRadius; y++)
        {
            var chunkPosition = new Vector2I(x, y);
            
            var distance = MathF.Sqrt((playerPosition.Y - chunkPosition.Y) * (playerPosition.Y - chunkPosition.Y) 
                           + (playerPosition.X - chunkPosition.X) * (playerPosition.X - chunkPosition.X));
            
            if (distance <= GenerationRadius)
                chunks.Add(chunkPosition);
        }
        return chunks;
    }
    
    private HashSet<Vector2I> GetChunksInRangeRadiusPlusTwo(Vector2I playerPosition)
    {
        var chunks = new HashSet<Vector2I>();
        var cullRadius = GenerationRadius + 2;
        
        for (int x = playerPosition.X - 1 - GenerationRadius; x < playerPosition.X + 1 + GenerationRadius; x++)
        for (int y = playerPosition.Y - 1 - GenerationRadius; y < playerPosition.Y + 1 + GenerationRadius; y++)
        {
            var chunkPosition = new Vector2I(x, y);
            
            var distance = MathF.Sqrt((playerPosition.Y - chunkPosition.Y) * (playerPosition.Y - chunkPosition.Y) 
                                      + (playerPosition.X - chunkPosition.X) * (playerPosition.X - chunkPosition.X));
            
            if (distance <= cullRadius)
                chunks.Add(chunkPosition);
        }
        return chunks;
    }

    private HashSet<Vector2I> GetChunksInRangeSquare(Vector2I playerPosition)
    {
        var chunks = new HashSet<Vector2I>();
        
        for (int x = playerPosition.X - GenerationRadius; x < playerPosition.X + GenerationRadius; x++)
        for (int y = playerPosition.Y - GenerationRadius; y < playerPosition.Y + GenerationRadius; y++)
        {
            chunks.Add(new Vector2I(x, y));  
        }
        
        return chunks;
    }
    
    private HashSet<Vector2I> GetChunksInRangeSquarePlusTwo(Vector2I playerPosition)
    {
        var chunks = new HashSet<Vector2I>();
        
        for (int x = playerPosition.X - 1 - GenerationRadius; x < playerPosition.X + 1 + GenerationRadius; x++)
        for (int y = playerPosition.Y - 1 - GenerationRadius; y < playerPosition.Y + 1 + GenerationRadius; y++)
        {
            chunks.Add(new Vector2I(x, y));  
        }
        
        return chunks;
    }
    
    
}
