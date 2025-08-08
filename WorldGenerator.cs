using System.Collections.Concurrent;
using Godot;

namespace Wancraft;

public partial class WorldGenerator : Node3D
{
    private readonly ConcurrentDictionary<Vector2I, BlockType[,,]> _cachedMaps = new();
    
    [Export] private int TerrainHeight = 128;
    
    [Export] private FastNoiseLite TerrainNoise;

    public BlockType[,,] GenerateBlockMap(int xOffset, int zOffset, int chunkSize)
    {
        var chunkCoords = new Vector2I(xOffset, zOffset);

        if (_cachedMaps.TryGetValue(chunkCoords, out var blockMap))
            return blockMap;
        blockMap = new BlockType[chunkSize, TerrainHeight, chunkSize];

        // Get a copy of FastNoiseLite since it is NOT thread-safe :(
        var localNoise = GetLocalNoiseInstance();
        var halfTerrainHeight = TerrainHeight / 2;

        for (var x = 0; x < chunkSize; x++)
        for (var z = 0; z < chunkSize; z++)
        {
            var noiseValue = localNoise.GetNoise2D(x + xOffset, z + zOffset);
            var blocksInStack = (int)(noiseValue * halfTerrainHeight + halfTerrainHeight);
            var dirtHeight = blocksInStack - (int)(noiseValue * 12f + 12f);

            for (var y = 0; y < blocksInStack; y++)
                if (y >= dirtHeight && y < blocksInStack - 1)
                    blockMap[x, y, z] = BlockType.Dirt;
                else if (y < dirtHeight)
                    blockMap[x, y, z] = BlockType.Rock;
                else
                    blockMap[x, y, z] = BlockType.Grass;
        }

        return blockMap;
    }


    private FastNoiseLite GetLocalNoiseInstance() =>
        new()
        {
            NoiseType = TerrainNoise.NoiseType,
            Seed = TerrainNoise.Seed,
            Frequency = TerrainNoise.Frequency,
            FractalType = TerrainNoise.FractalType,
            FractalOctaves = TerrainNoise.FractalOctaves,
            FractalLacunarity = TerrainNoise.FractalLacunarity,
            FractalGain = TerrainNoise.FractalGain,
            DomainWarpEnabled = TerrainNoise.DomainWarpEnabled,
            DomainWarpType = TerrainNoise.DomainWarpType,
            DomainWarpAmplitude = TerrainNoise.DomainWarpAmplitude,
            DomainWarpFrequency = TerrainNoise.DomainWarpFrequency,
            DomainWarpFractalType = TerrainNoise.DomainWarpFractalType,
            DomainWarpFractalOctaves = TerrainNoise.DomainWarpFractalOctaves,
            DomainWarpFractalLacunarity = TerrainNoise.DomainWarpFractalLacunarity,
            DomainWarpFractalGain = TerrainNoise.DomainWarpFractalGain
        };
}