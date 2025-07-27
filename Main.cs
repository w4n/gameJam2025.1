using Godot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using wancraft;

public partial class Main : Node
{
    [Export] private PackedScene _block;
    [Export] private FastNoiseLite _noise;
    [Export] private int _worldSize = 64;
    [Export] private int _chunkSize = 16;
    [Export] private int _worldHeight = 128;
    
    [Export] public CharacterBody3D Player { get; set; }
    
    [Export] private TextEdit _textX;
    [Export] private TextEdit _textY;
    [Export] private TextEdit _textZ;
    [Export] private Label _labelOutput;
    [Export] private Material _blockMaterial;

    private int _minHeight = 10;
    private int _maxHeight = 80;
    
    private bool[,,] _blockMap;

    private Vector2 _playerPosition = Vector2.Zero;
    
    private ConcurrentQueue<MeshInstance3D> _completedChunks = new();
    
    public override void _Ready()
    {
        
        //_noise.Seed = (int)GD.Randi() % 65536;
        //GenerateWorld();
        
        var timestamp = DateTime.Now;
        GenerateBlockMap();
        GD.Print($"Block generation took: {(DateTime.Now - timestamp).TotalMilliseconds} ms");
        //InitializeBlocks();

        Task.Run(() => GenerateChunk(0, 0));
        Task.Run(() => GenerateChunk(_chunkSize, 0));
        Task.Run(() => GenerateChunk(_chunkSize * 2, 0));
        Task.Run(() => GenerateChunk(0, _chunkSize));
        Task.Run(() => GenerateChunk(0, _chunkSize * 2));
        Task.Run(() => GenerateChunk(_chunkSize, _chunkSize));
        Task.Run(() => GenerateChunk(_chunkSize * 2, _chunkSize * 2));
        
        //GenerateGeometry();
    }

    public override void _Process(double delta)
    {
        
            
        //_labelOutput.Text = Engine.GetFramesPerSecond().ToString("000");
        
        //_labelOutput.Text = $"Player position: {Player.Position.X:F}:{Player.Position.Z:F}";
        
        /*if ((int)Player.Position.X / _chunkSize != (int)_playerPosition.X || (int)Player.Position.Z / _chunkSize != (int)_playerPosition.Y)
        {
            _playerPosition.X = (int)((int)Player.Position.X / _chunkSize);
            _playerPosition.Y = (int)((int)Player.Position.Z / _chunkSize);
            _labelOutput.Text = $"Player position: {_playerPosition.X}:{_playerPosition.Y}";
        }*/
        
        while (_completedChunks.TryDequeue(out var chunk))
            AddChild(chunk);
    }

    public override void _PhysicsProcess(double delta)
    {
        _labelOutput.Text = $"Player position: {_playerPosition.X:F}:{_playerPosition.Y:F}";
        
        if ((int)Player.Position.X / _chunkSize != (int)_playerPosition.X || (int)Player.Position.Z / _chunkSize != (int)_playerPosition.Y)
        {
            _playerPosition.X = (int)((int)Player.Position.X / _chunkSize);
            _playerPosition.Y = (int)((int)Player.Position.Z / _chunkSize);
            _labelOutput.Text = $"Player position: {_playerPosition.X:F}:{_playerPosition.Y:F}";
        }
            
        base._PhysicsProcess(delta);
    }

    private void GenerateChunk(int xOffset, int zOffset)
    {
        var surface = new SurfaceTool();
        surface.Begin(Mesh.PrimitiveType.Triangles);
        
        for (int x = 0 + xOffset; x < xOffset + _chunkSize; x++)
        {
            for (int z = 0 + zOffset; z < zOffset + _chunkSize; z++)
            {
                var firstBlock = true;

                for (int y = _worldHeight; y >= 0; y--)
                {
                    if (!IsBlockAt(x, y, z))
                        continue;

                    if (firstBlock)
                    {
                        firstBlock = false;
                        AddBlockToSurface(surface, x, y, z);
                    }
                    
                    if (!IsBlockVisible(x, y, z))
                        break;
                    
                    AddBlockToSurface(surface, x, y, z);
                }
            }
        }
        
        //surface.GenerateNormals();
        surface.GenerateTangents();
        
        var mesh = surface.Commit();

        var shape = mesh.CreateTrimeshShape();
        
        var meshInstance = new MeshInstance3D();

        meshInstance.Mesh = mesh;
        meshInstance.MaterialOverride = _blockMaterial;
        
        var staticBody = new StaticBody3D();
        var collisionShape = new CollisionShape3D();
        collisionShape.Shape = shape;
        
        staticBody.AddChild(collisionShape);
        
        meshInstance.AddChild(staticBody);
        
        _completedChunks.Enqueue(meshInstance);
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
        
        var uvs = new Vector2[]
        {
            new(0, 1), // Bottom-left
            new(1, 1), // Bottom-right
            new(1, 0), // Top-right
            new(0, 0)  // Top-left
        };
        
        // block above?
        if (!IsBlockAt(x, y + 1, z))
        {
            // Add top face
            AddQuad(surface, vertices[4], vertices[5], vertices[1], vertices[0], Vector3.Up, uvs);
        }
        
        // block below?
        if (!IsBlockAt(x, y - 1, z))
        {
            // Add bottom face
            AddQuad(surface, vertices[3], vertices[2], vertices[6], vertices[7], Vector3.Down, uvs);
        }
        
        // block left?
        if (!IsBlockAt(x - 1, y, z))
        {
            // Add left face
            AddQuad(surface, vertices[4], vertices[0], vertices[3], vertices[7], Vector3.Left, uvs);
        }
        
        // block right?
        if (!IsBlockAt(x + 1, y, z))
        {
            // Add left face
            AddQuad(surface, vertices[1], vertices[5], vertices[6], vertices[2], Vector3.Right, uvs);
        }
        
        // block in front?
        if (!IsBlockAt(x, y, z + 1))
        {
            // Add front face
            AddQuad(surface, vertices[0], vertices[1], vertices[2], vertices[3], Vector3.Back, uvs);
        }
        
        // block behind?
        if (!IsBlockAt(x, y, z - 1))
        {
            // Add back face
            AddQuad(surface, vertices[5], vertices[4], vertices[7], vertices[6], Vector3.Forward, uvs);
        }
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

    private void GenerateGeometry()
    {
        var st = new SurfaceTool();
        
        st.Begin(Mesh.PrimitiveType.Triangles);

        var half = 0.5f * 1f;
        
        var vertices = new Vector3[]
        {
            new(-half,  half,  half),  // 0: Top-left-front
            new( half,  half,  half),  // 1: Top-right-front
            new( half, -half,  half),  // 2: Bottom-right-front
            new(-half, -half,  half),  // 3: Bottom-left-front
            new(-half,  half, -half),  // 4: Top-left-back
            new( half,  half, -half),  // 5: Top-right-back
            new( half, -half, -half),  // 6: Bottom-right-back
            new(-half, -half, -half),  // 7: Bottom-left-back
        };
        
        // UV map
        var uvs = new Vector2[]
        {
            new Vector2(0, 1), // Bottom-left
            new Vector2(1, 1), // Bottom-right
            new Vector2(1, 0), // Top-right
            new Vector2(0, 0)  // Top-left
        };

        /*Vector3[] frontFace =
        {
            vertices[0], vertices[1], vertices[2],
            vertices[0], vertices[2], vertices[3],
        };
        
        Vector3[] backFace =
        {
            vertices[5], vertices[4], vertices[7],
            vertices[5], vertices[7], vertices[6],
        };
        
        Vector3[] topFace =
        {
            vertices[4], vertices[5], vertices[1],
            vertices[4], vertices[1], vertices[0],
        };
        
        Vector3[] bottomFace =
        {
            vertices[3], vertices[2], vertices[6],
            vertices[3], vertices[6], vertices[7],
        };
        
        Vector3[] leftFace =
        {
            vertices[4], vertices[0], vertices[3],
            vertices[4], vertices[3], vertices[7],
        };
        
        Vector3[] rightFace =
        {
            vertices[1], vertices[5], vertices[6],
            vertices[1], vertices[6], vertices[2],
        };*/
        
        AddQuad(st, vertices[0], vertices[1], vertices[2], vertices[3], Vector3.Back, uvs);
        AddQuad(st, vertices[5], vertices[4], vertices[7], vertices[6], Vector3.Forward, uvs);
        AddQuad(st, vertices[4], vertices[0], vertices[3], vertices[7], Vector3.Left, uvs);
        AddQuad(st, vertices[1], vertices[5], vertices[6], vertices[2], Vector3.Right, uvs);
        AddQuad(st, vertices[4], vertices[5], vertices[1], vertices[0], Vector3.Up, uvs);
        AddQuad(st, vertices[3], vertices[2], vertices[6], vertices[7], Vector3.Down, uvs);
        
        /*Vector3[] finalFacesVertices = new Vector3[frontFace.Length +  backFace.Length + topFace.Length + bottomFace.Length + leftFace.Length + rightFace.Length];
        
        frontFace.CopyTo(finalFacesVertices, 0);
        backFace.CopyTo(finalFacesVertices, frontFace.Length);
        topFace.CopyTo(finalFacesVertices, frontFace.Length * 2);
        bottomFace.CopyTo(finalFacesVertices, frontFace.Length * 3);
        leftFace.CopyTo(finalFacesVertices, frontFace.Length * 4);
        rightFace.CopyTo(finalFacesVertices, frontFace.Length * 5);*/

        /*foreach (var vertex in finalFacesVertices)
        {
            st.AddVertex(vertex);
        }*/
        
        //st.GenerateNormals();
        //st.GenerateTangents();

        var mesh = st.Commit();
        
        MeshInstance3D meshInstance = new MeshInstance3D();

        meshInstance.Mesh = mesh;
        meshInstance.MaterialOverride = _blockMaterial;
        
        AddChild(meshInstance);
    }
    
    private static void AddQuad(SurfaceTool surfaceTool, 
        Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3,
        Vector3 normal, Vector2[] uvs)
    {
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

    private void GenerateWorld()
    {
        var blockMap = new bool[_worldSize, _maxHeight - _minHeight, _worldSize]; 
        
        for (int x = 0; x < _worldSize; x++)
        {
            for (int z = 0; z < _worldSize; z++)
            {
                var noiseValue = _noise.GetNoise2D(x, z);
                //GD.Print(noiseValue);

                var blocksInStack = _maxHeight * noiseValue + _minHeight;
                //GD.Print(blocksInStack.ToString());
                /*switch (noiseValue)
                {
                    case < 0f: blocksInStack = 1; break;
                    case < .2f: blocksInStack = 2; break;
                    case < .4f: blocksInStack = 4; break;
                    case < 1f: blocksInStack = 8; break;
                }*/
                
                var newBlock = _block.Instantiate() as Node3D;
                newBlock.Position = new Vector3(x, (int)blocksInStack, z);
                AddChild(newBlock);
                
                /*for (int y = 0; y < blocksInStack; y++)
                {
                    blockMap[x, y, z] = true;
                    /*var newBlock = _block.Instantiate() as Node3D;
                    newBlock.Position = new Vector3(x, y, z);
                    AddChild(newBlock);#1#
                }*/
            }
        }
    }

    private void GenerateBlockMap()
    {
        _blockMap = new bool[_worldSize, _maxHeight + _minHeight, _worldSize];

        for (int x = 0; x < _worldSize; x++)
        {
            for (int z = 0; z < _worldSize; z++)
            {
                var noiseValue = _noise.GetNoise2D(x, z) * _maxHeight;
                var blocksInStack = noiseValue + _minHeight;
                
                for (int y = 0; y < blocksInStack; y++)
                    _blockMap[x, y, z] = true;
            }
        }
    }

    private void InitializeBlocks()
    {
        for (int x = 0; x < _worldSize; x++)
        {
            for (int z = 0; z < _worldSize; z++)
            {
                var firstBlock = true;
                
                for (int y = _worldSize; y >= 0; y--)
                {
                    if (!IsBlockAt(x, y, z))
                        continue;
                    
                    if (firstBlock)
                    {
                        firstBlock = false;
                        AddBlock(new Vector3(x, y, z));
                    }

                    if (!IsBlockVisible(x, y, z))
                        break;
                    
                    AddBlock(new Vector3(x, y, z));
                }
            }
        }
    }

    public void ShowBlockInfo(Vector3 pos)
    {
        var blockInfo = new StringBuilder();
        
        int x, y, z;

        x = (int)pos.X;
        y = (int)pos.Y;
        z = (int)pos.Z;
        
        blockInfo.AppendLine($"IsBlockVisible({x}, {y}, {z}) = {IsBlockVisible(x, y, z)}");
        blockInfo.AppendLine($"IsBlockAt(x, y--, z) = {IsBlockAt(x, y - 1, z)}");
        blockInfo.AppendLine($"IsBlockAt(x-- , y, z) = {IsBlockAt(x - 1, y, z)}");
        blockInfo.AppendLine($"IsBlockAt(x++, y, z) = {IsBlockAt(x + 1, y, z)}");
        blockInfo.AppendLine($"IsBlockAt(x, y, z - 1) = {IsBlockAt(x, y, z - 1)}");
        blockInfo.AppendLine($"IsBlockAt(x, y, z + 1) = {IsBlockAt(x, y, z + 1)}");
        
        _labelOutput.Text = blockInfo.ToString();
    }

    private void AddBlock(Vector3 position)
    {
        var newBlock = _block.Instantiate() as Node3D;
        
        if (newBlock == null)
            return;
        
        newBlock.Position = position;
        ((Block)newBlock).BlockController = this;
        AddChild(newBlock);
    }

    private bool IsBlockVisible(int x, int y, int z)
    {
        return !IsBlockAt(x, y - 1, z) ||
               !IsBlockAt(x - 1, y, z) || !IsBlockAt(x + 1, y, z) || 
               !IsBlockAt(x, y, z - 1) || !IsBlockAt(x, y, z + 1);
    }

    private bool IsBlockAt(int x, int y, int z)
    {
        if (x < 0 || x >= _worldSize || 
            y < 0 || y >= (_maxHeight + _minHeight) || 
            z < 0 || z >= _worldSize)
            return false;
        
        return _blockMap[x, y, z];
    }
}
