using System;
using System.Collections.Concurrent;
using Godot;

namespace Wancraft;

public partial class WorldGenerator : Node3D
{
    private readonly ConcurrentDictionary<Vector2I, BlockType[,,]> _cachedMaps = new();
    
    [Export] private int TerrainHeight = 128;
    
    [Export] private FastNoiseLite BiomeNoise;
    [Export] private FastNoiseLite CaveNoise;
    [Export] private FastNoiseLite ContinentNoise;
    [Export] private FastNoiseLite DetailNoise;
    [Export] private FastNoiseLite MountainNoise;
    [Export] private FastNoiseLite OreNoise;

    private FastNoiseLite _biomeNoise;
    private FastNoiseLite _caveNoise;
    private FastNoiseLite _continentNoise;
    private FastNoiseLite _detailNoise;
    private FastNoiseLite _mountainNoise;
    private FastNoiseLite _oreNoise;
    
    public BlockType[,,] GenerateBlockMap(int xOffset, int zOffset, int chunkSize)
    {
        var chunkCoords = new Vector2I(xOffset, zOffset);

        if (_cachedMaps.TryGetValue(chunkCoords, out var blockMap))
            return blockMap;
        
        blockMap = new BlockType[chunkSize, TerrainHeight, chunkSize];

        // Get a copy of FastNoiseLite since it is NOT thread-safe :(
        CloneNoiseInstances();

        for (var x = 0; x < chunkSize; x++)
        for (var z = 0; z < chunkSize; z++)
        {
            var worldX = x + xOffset;
            var worldZ = z + zOffset;

            var biome = DetermineBiome(worldX, worldZ, _biomeNoise);
            //var biome = BiomeType.Forest;
            
            var heightPercentage = GetTerrainHeight(worldX, worldZ, biome);
            var terrainHeight = (int)(heightPercentage * TerrainHeight);
            
            for (var y = 0; y < TerrainHeight; y++)
            {
                blockMap[x, y, z] = BlockType.Air;
                
                if (y <= TerrainHeight / 2 && biome == BiomeType.Ocean && y <= TerrainHeight / 2 + 5)
                {
                    // Water in ocean biomes
                    blockMap[x, y, z] = BlockType.Water;
                }
                else if (y < terrainHeight)
                {
                    
                    var caveValue = _caveNoise.GetNoise3D(worldX, y, worldZ);
                    if (caveValue > 0.55f && y < terrainHeight - 5 && y > 10)
                    {
                        blockMap[x, y, z] = BlockType.Air;
                        continue;
                    }
                    
                    var isSurface = y >= terrainHeight - 1;
                    
                    if (isSurface)
                    {
                        switch (biome)
                        {
                            case BiomeType.Desert:
                                blockMap[x, y, z] = BlockType.Sand;
                                break;
                            case BiomeType.Ocean:
                                // Water will be placed above this in a separate pass
                                blockMap[x, y, z] = BlockType.Sand;
                                break;
                            default:
                                blockMap[x, y, z] = BlockType.Grass;
                                break;
                        }
                    }
                    else if (y >= terrainHeight - 4)
                    {
                        // Sub-surface layer
                        blockMap[x, y, z] = biome == BiomeType.Desert ? BlockType.Sand : BlockType.Dirt;
                    }
                    else
                    {
                        // Deep underground
                        blockMap[x, y, z] = BlockType.Rock;
                        
                        var oreValue = (_oreNoise.GetNoise3D(worldX, y, worldZ) * -1) * 1.2f;
                        
                        if (oreValue > 0.8f && y < 20)
                            blockMap[x, y, z] = BlockType.GoldOre;
                        else if (oreValue > 0.75f && y < 40)
                            blockMap[x, y, z] = BlockType.IronOre;
                        else if (oreValue > 0.7f && y < 60)
                            blockMap[x, y, z] = BlockType.CoalOre;
                    }
                }
                
            }
            
            // Add lakes in non-ocean biomes
            if (biome != BiomeType.Ocean && biome != BiomeType.Mountains)
            {
                var lakeValue = _biomeNoise.GetNoise2D(worldX * 0.2f, worldZ * 0.2f);
                if (lakeValue > 0.8f)
                {
                    // Create a small lake
                    for (int y = terrainHeight; y < terrainHeight + 2; y++)
                    {
                        if (y < TerrainHeight)
                            blockMap[x, y, z] = BlockType.Water;
                    }
                    // Make the bottom of the lake sandy
                    blockMap[x, terrainHeight - 1, z] = BlockType.Sand;
                }
            }
        }
    
        _cachedMaps[chunkCoords] = blockMap;
        return blockMap;
    }
    
    private void GenerateTree(BlockType[,,] blockMap, int x, int y, int z, int chunkSize)
    {
        // Check if we have enough space for a tree
        if (x < 2 || x >= chunkSize - 2 || z < 2 || z >= chunkSize - 2 || y + 5 >= TerrainHeight)
            return;
        
        int trunkHeight = 4 + (int)(_continentNoise.GetNoise2D(x, z) * 2);
        for (int i = 0; i < trunkHeight; i++)
        {
            blockMap[x, y + i, z] = BlockType.Wood;
        }
    
        // Generate leaves
        for (int lx = -2; lx <= 2; lx++)
        for (int lz = -2; lz <= 2; lz++)
        for (int ly = 0; ly <= 2; ly++)
        {
            int leafX = x + lx;
            int leafY = y + trunkHeight - 1 + ly;
            int leafZ = z + lz;
        
            // Skip corners for a more natural look
            if ((Math.Abs(lx) == 2 && Math.Abs(lz) == 2) || leafY >= TerrainHeight)
                continue;
            
            if (blockMap[leafX, leafY, leafZ] == BlockType.Air)
                blockMap[leafX, leafY, leafZ] = BlockType.Leaves;
        }
    }


    private void CloneNoiseInstances()
    {
        _biomeNoise = GetLocalNoiseInstance(BiomeNoise);
        _caveNoise = GetLocalNoiseInstance(CaveNoise);
        _continentNoise = GetLocalNoiseInstance(ContinentNoise);
        _detailNoise = GetLocalNoiseInstance(DetailNoise);
        _mountainNoise = GetLocalNoiseInstance(MountainNoise);
        _oreNoise = GetLocalNoiseInstance(OreNoise);
    }
    
    private BiomeType DetermineBiome(float x, float z, FastNoiseLite biomeNoise)
    {
        var biomeValue = biomeNoise.GetNoise2D(x * 0.01f, z * 0.01f);

        return biomeValue switch
        {
            (< -0.6f) => BiomeType.Ocean,
            (< -0.2f) => BiomeType.Plains,
            (< 0.2f) => BiomeType.Forest,
            (< 0.6f) => BiomeType.Desert,
            _ => BiomeType.Mountains
        };
    }
    
    private float GetTerrainHeight(float x, float z, BiomeType biome)
    {
        var continentValue = _continentNoise.GetNoise2D(x, z) * 0.5f + 0.5f;
        var mountainValue = _mountainNoise.GetNoise2D(x, z) * 0.5f + 0.5f;
        var detailValue = _detailNoise.GetNoise2D(x, z) * 0.5f + 0.5f;
        
        return biome switch
        {
            BiomeType.Plains => continentValue * 0.3f + 0.2f,
            BiomeType.Mountains => continentValue * 0.3f + mountainValue * 0.7f + 0.3f,
            BiomeType.Forest => continentValue * 0.5f + detailValue * 0.1f + 0.3f,
            BiomeType.Desert => continentValue * 0.4f + detailValue * 0.05f + 0.25f,
            BiomeType.Ocean => continentValue * 0.2f + 0.05f,
            _ => continentValue * 0.3f + 0.2f
        };
    }

    private FastNoiseLite GetLocalNoiseInstance(FastNoiseLite noise) =>
        new()
        {
            NoiseType = noise.NoiseType,
            Seed = noise.Seed,
            Frequency = noise.Frequency,
            FractalType = noise.FractalType,
            FractalOctaves = noise.FractalOctaves,
            FractalLacunarity = noise.FractalLacunarity,
            FractalGain = noise.FractalGain,
            DomainWarpEnabled = noise.DomainWarpEnabled,
            DomainWarpType = noise.DomainWarpType,
            DomainWarpAmplitude = noise.DomainWarpAmplitude,
            DomainWarpFrequency = noise.DomainWarpFrequency,
            DomainWarpFractalType = noise.DomainWarpFractalType,
            DomainWarpFractalOctaves = noise.DomainWarpFractalOctaves,
            DomainWarpFractalLacunarity = noise.DomainWarpFractalLacunarity,
            DomainWarpFractalGain = noise.DomainWarpFractalGain,
            CellularReturnType = noise.CellularReturnType
        };
}