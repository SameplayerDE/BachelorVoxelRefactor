using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;
using Color = Microsoft.Xna.Framework.Color;

namespace VoxelRayCast
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        
        private Texture2D _computeTexture;
        private Texture3D _textureAtlas;

        private Effect _computeShader;
        private const int _computeGroupSize = 64;
        //private StructuredBuffer _rayResultBuffer;
        private StructuredBuffer _shaderMap;

        private RenderTarget2D _rayCastTarget;
        private int _rayCastTargetResolutionX = 1920 / 10;
        private int _rayCastTargetResolutionY = 1080 / 10;

        private const int _distance = 8;
        private const int _mapX = 16 * (_distance * 2 + 1);
        private const int _mapY = 16 * (_distance * 2 + 1);
        private const int _mapZ = 16 * (_distance * 2 + 1);

        private int[] _map1D;
        
        private Texture2D[] _textures;
        
        private Vector3 _position = new Vector3(-32, 128, -32);
        private Vector3 _rayPosition = new Vector3(0, 0f, 0);
        private Vector3 _rotation = new Vector3(MathHelper.ToRadians(15), MathHelper.ToRadians(45), 0);
        private Vector3 _rayRotation;
        private Vector3 _direction;
        private float _movementSpeed = 32.0f;
        
        private int _rayResolution = 1;

        private KeyboardState _prevState;
        private KeyboardState _currState;

        private MouseState _prevMouseState;
        private MouseState _currMouseState;
        private FastNoiseLite _noise;

        private bool _centerMouse = false;

        private Vector3 _currChunkPosition;
        private Vector3 _prevChunkPosition;
        
        private VertexBuffer _vertexBuffer;

        public Game1()
        {
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            Window.AllowUserResizing = true;

            _graphics = new GraphicsDeviceManager(this);
            _graphics.IsFullScreen = false;
            _graphics.GraphicsProfile = GraphicsProfile.HiDef;
            _noise = new FastNoiseLite();
            WorldGenerator.NoiseGenerator = _noise;
        }

        public void BuildCube()
        {
             VertexPositionColor[] vertices = new VertexPositionColor[6 * 6];

            float val = 1f;

            //Facing Negativ X
            vertices[6 * 0 + 0] = new VertexPositionColor(new Vector3(+0, +0, +val), Color.White);
            vertices[6 * 0 + 1] = new VertexPositionColor(new Vector3(+0, -val, +val), Color.White);
            vertices[6 * 0 + 2] = new VertexPositionColor(new Vector3(+0, -val, +0), Color.White);

            vertices[6 * 0 + 3] = new VertexPositionColor(new Vector3(+0, -val, +0), Color.White);
            vertices[6 * 0 + 4] = new VertexPositionColor(new Vector3(+0, +0, +0), Color.White);
            vertices[6 * 0 + 5] = new VertexPositionColor(new Vector3(+0, +0, +val), Color.White);

            //Facing Negativ Y
            vertices[6 * 1 + 0] = new VertexPositionColor(new Vector3(+0, -val, +val), Color.Red);
            vertices[6 * 1 + 1] = new VertexPositionColor(new Vector3(+val, -val, +val), Color.Red);
            vertices[6 * 1 + 2] = new VertexPositionColor(new Vector3(+val, -val, +0), Color.Red);

            vertices[6 * 1 + 3] = new VertexPositionColor(new Vector3(+val, -val, +0), Color.Red);
            vertices[6 * 1 + 4] = new VertexPositionColor(new Vector3(+0, -val, +0), Color.Red);
            vertices[6 * 1 + 5] = new VertexPositionColor(new Vector3(+0, -val, +val), Color.Red);

            //Facing Positiv X
            vertices[6 * 2 + 0] = new VertexPositionColor(new Vector3(+val, -val, +val), Color.Blue);
            vertices[6 * 2 + 1] = new VertexPositionColor(new Vector3(+val, +0, +val), Color.Blue);
            vertices[6 * 2 + 2] = new VertexPositionColor(new Vector3(+val, +0, +0), Color.Blue);

            vertices[6 * 2 + 3] = new VertexPositionColor(new Vector3(+val, +0, +0), Color.Blue);
            vertices[6 * 2 + 4] = new VertexPositionColor(new Vector3(+val, -val, +0), Color.Blue);
            vertices[6 * 2 + 5] = new VertexPositionColor(new Vector3(+val, -val, +val), Color.Blue);

            //Facing Positiv Y
            vertices[6 * 3 + 0] = new VertexPositionColor(new Vector3(+val, +0, +val), Color.Yellow);
            vertices[6 * 3 + 1] = new VertexPositionColor(new Vector3(+0, +0, +val), Color.Yellow);
            vertices[6 * 3 + 2] = new VertexPositionColor(new Vector3(+0, +0, +0), Color.Yellow);

            vertices[6 * 3 + 3] = new VertexPositionColor(new Vector3(+0, +0, +0), Color.Yellow);
            vertices[6 * 3 + 4] = new VertexPositionColor(new Vector3(+val, +0, +0), Color.Yellow);
            vertices[6 * 3 + 5] = new VertexPositionColor(new Vector3(+val, +0, +val), Color.Yellow);

            //Facing Positiv Z
            vertices[6 * 4 + 0] = new VertexPositionColor(new Vector3(+val, +0, +val), Color.Green);
            vertices[6 * 4 + 1] = new VertexPositionColor(new Vector3(+val, -val, +val), Color.Green);
            vertices[6 * 4 + 2] = new VertexPositionColor(new Vector3(+0, -val, +val), Color.Green);

            vertices[6 * 4 + 3] = new VertexPositionColor(new Vector3(+0, -val, +val), Color.Green);
            vertices[6 * 4 + 4] = new VertexPositionColor(new Vector3(+0, +0, +val), Color.Green);
            vertices[6 * 4 + 5] = new VertexPositionColor(new Vector3(+val, +0, +val), Color.Green);

            //Facing Negativ Z
            vertices[6 * 5 + 0] = new VertexPositionColor(new Vector3(+0, +0, +0), Color.Purple);
            vertices[6 * 5 + 1] = new VertexPositionColor(new Vector3(+0, -val, +0), Color.Purple);
            vertices[6 * 5 + 2] = new VertexPositionColor(new Vector3(+val, -val, +0), Color.Purple);

            vertices[6 * 5 + 3] = new VertexPositionColor(new Vector3(+val,  -val, +0), Color.Purple);
            vertices[6 * 5 + 4] = new VertexPositionColor(new Vector3(+val, +0, +0), Color.Purple);
            vertices[6 * 5 + 5] = new VertexPositionColor(new Vector3(+0, +0, +0), Color.Purple);

            _vertexBuffer = new VertexBuffer(GraphicsDevice, typeof(VertexPositionColor), 6 * 6, BufferUsage.WriteOnly);
            _vertexBuffer.SetData(vertices);
        }
        
        public void SetSeed(int seed = 101199)
        {
            _noise.SetSeed(seed);
            _noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2S);
        }

        public void SetResolutionDownScaleBy(int a = 0)
        {
            a = Math.Clamp(a, 1, 100);
            _rayCastTargetResolutionX = 1920 / a;
            _rayCastTargetResolutionY = 1080 / a;
        }

        private void WriteChunkToMap1D(Chunk chunk, int chunkOffsetX, int chunkOffsetY, int chunkOffsetZ)
        {
            for (int y = 0; y < Chunk.Size; y++)
            {
                for (int z = 0; z < Chunk.Size; z++)
                {
                    for (int x = 0; x < Chunk.Size; x++)
                    {
                        int worldX = chunkOffsetX * Chunk.Size + x;
                        int worldY = chunkOffsetY * Chunk.Size + y;
                        int worldZ = chunkOffsetZ * Chunk.Size + z;

                        int mapIndex = worldX + _mapX * (worldZ + _mapZ * worldY);
                        int chunkIndex = x + Chunk.Size * (z + Chunk.Size * y);

                        _map1D[mapIndex] = chunk.Data[chunkIndex];
                    }
                }
            }
        }
        
        protected override void Initialize()
        {
            
            AnsiConsole.Progress().Start(ctx =>
            {
                var task1 = ctx.AddTask("[green]Map generation[/]", autoStart: true);

                const int precalculate = 16;
                int totalChunks = precalculate * precalculate * precalculate;
                int current = 0;

                for (int y = 0; y < precalculate; y++)
                {
                    for (int z = 0; z < precalculate; z++)
                    {
                        for (int x = 0; x < precalculate; x++)
                        {
                            WorldGenerator.GetChunk(x, y, z); // 👈 Chunk wird gecached / generiert
                            current++;
                            task1.Value = (float)current / totalChunks * 100f;
                        }
                    }
                }
                task1.StopTask(); // optional
            });
            
            _map1D = new int[_mapX * _mapY * _mapZ];
            IsFixedTimeStep = false;
            _graphics.SynchronizeWithVerticalRetrace = false;
            TargetElapsedTime = TimeSpan.FromMilliseconds(16);
            
            _graphics.ApplyChanges();

            _textures = new Texture2D[7];
            _textures[0] = Content.Load<Texture2D>("dirt");
            _textures[1] = Content.Load<Texture2D>("oak_log");
            _textures[2] = Content.Load<Texture2D>("oak_planks");
            _textures[3] = Content.Load<Texture2D>("iron_block");
            _textures[4] = Content.Load<Texture2D>("gold_block");
            _textures[5] = Content.Load<Texture2D>("oak_log_top");
            _textures[6] = Content.Load<Texture2D>("cobblestone");

            _computeTexture = new Texture2D(GraphicsDevice, _rayCastTargetResolutionX, _rayCastTargetResolutionY, false, SurfaceFormat.Color, ShaderAccess.ReadWrite);
            _textureAtlas = new Texture3D(GraphicsDevice, 16, 16, 7, false, SurfaceFormat.Color, ShaderAccess.ReadWrite);

            _rayCastTarget = new RenderTarget2D(GraphicsDevice, _rayCastTargetResolutionX, _rayCastTargetResolutionY, false, GraphicsDevice.PresentationParameters.BackBufferFormat, DepthFormat.Depth24);

            _spriteBatch = new SpriteBatch(GraphicsDevice);
 
            _computeShader = Content.Load<Effect>("Ray3D");
            //_rayResultBuffer = new StructuredBuffer(GraphicsDevice, typeof(RayResult3D), _maxCount, BufferUsage.None, ShaderAccess.ReadWrite);
            _shaderMap = new StructuredBuffer(GraphicsDevice, typeof(int), _mapX * _mapY * _mapZ, BufferUsage.None, ShaderAccess.ReadWrite);
            
            var atlas = new Color[16 * 16 * _textures.Length];
            for (var i = 0; i < _textures.Length; i++)
            {
                var copy = new Color[16 * 16];
                _textures[i].GetData(copy);
                for (var y = 0; y < 16; y++)
                {
                    for (var x = 0; x < 16; x++)
                    {
                        var index3D = x + 16 * y + 16 * 16 * i;
                        var index2D = x + 16 * y;
                        atlas[index3D] = copy[index2D];
                    }
                }
            }

            _computeShader.Parameters["MapMaxX"].SetValue(_mapX);
            _computeShader.Parameters["MapMaxY"].SetValue(_mapY);
            _computeShader.Parameters["MapMaxZ"].SetValue(_mapZ);

            _textureAtlas.SetData(atlas);
            _shaderMap.SetData(_map1D);

            _computeShader.Parameters["Input"].SetValue(_textureAtlas);
            _computeShader.Parameters["InputW"].SetValue(_textureAtlas.Width);
            _computeShader.Parameters["InputH"].SetValue(_textureAtlas.Height);

            base.Initialize();
        }

        protected override void Update(GameTime gameTime)
        {
            if (!IsActive)
            {
                return;
            }

            var delta = (float)gameTime.ElapsedGameTime.TotalSeconds;

            _prevState = _currState;
            _currState = Keyboard.GetState();

            _prevMouseState = _currMouseState;
            _currMouseState = Mouse.GetState();

            var keyboard = _currState;

            if (keyboard.IsKeyDown(Keys.Escape))
            {
                Exit();
            }

            if (_centerMouse)
            {
                var screenCenter = GraphicsDevice.Viewport.Bounds.Center;
                Mouse.SetPosition(screenCenter.X, screenCenter.Y);

                if (_currState.IsKeyDown(Keys.Left))
                {
                    _rotation.Y += MathHelper.ToRadians(45f) * delta;
                }
                if (_currState.IsKeyDown(Keys.Right))
                {
                    _rotation.Y -= MathHelper.ToRadians(45f) * delta;
                }
                if (_currState.IsKeyDown(Keys.Up))
                {
                    _rotation.X -= MathHelper.ToRadians(45f) * delta;
                }
                if (_currState.IsKeyDown(Keys.Down))
                {
                    _rotation.X += MathHelper.ToRadians(45f) * delta;
                }
            }

            if (_currState.IsKeyDown(Keys.E) && _prevState.IsKeyUp(Keys.E))
            {
                _centerMouse = !_centerMouse;
            }
            
            _computeShader.Parameters["RotationMatrix"].SetValue(Matrix.CreateRotationX(_rotation.X) * Matrix.CreateRotationY(_rotation.Y) * Matrix.CreateRotationZ(_rotation.Z));

            var nRotation =
                Matrix.CreateRotationX(_rotation.X * 0) *
                Matrix.CreateRotationY(_rotation.Y) *
                Matrix.CreateRotationZ(_rotation.Z);

            _direction = Vector3.Transform(Vector3.Backward, nRotation);
            if (_direction.Length() != 0)
            {
                _direction.Normalize();
            }

            var movement = Vector3.Zero;

            if (keyboard.IsKeyDown(Keys.W))
            {
                movement += _direction * new Vector3(1, 0, 1) * _movementSpeed * delta;
            }
            if (keyboard.IsKeyDown(Keys.S))
            {
                movement -= _direction * new Vector3(1, 0, 1) * _movementSpeed * delta;
            }
            if (keyboard.IsKeyDown(Keys.D))
            {
                movement += Vector3.Cross(_direction * new Vector3(1, 0, 1), new Vector3(0, 1, 0)) * _movementSpeed * delta;
            }
            if (keyboard.IsKeyDown(Keys.A))
            {
                movement -= Vector3.Cross(_direction * new Vector3(1, 0, 1), new Vector3(0, 1, 0)) * _movementSpeed * delta;
            }

            if (keyboard.IsKeyDown(Keys.LeftShift))
            {
                movement += Vector3.Up * _movementSpeed * delta;
            }
            if (keyboard.IsKeyDown(Keys.LeftControl))
            {
                movement += Vector3.Down * _movementSpeed * delta;
            }
            
            if (movement.Length() != 0)
            {
                var next = _position + movement;

                if (!IsSolid(next))
                {
                    _position = next;
                }
                else
                {
                    if (!IsSolid(next.X, _position.Y, _position.Z))
                    {
                        _position = new Vector3(next.X, _position.Y, _position.Z);
                    }
                    if (!IsSolid(_position.X, next.Y, _position.Z))
                    {
                        _position = new Vector3(_position.X, next.Y, _position.Z);
                    }
                    if (!IsSolid(_position.X, _position.Y, next.Z))
                    {
                        _position = new Vector3(_position.X, _position.Y, next.Z);
                    }
                }

                _prevChunkPosition = _currChunkPosition;
                _currChunkPosition = ToChunkPosition(_position);

                if (_currChunkPosition != _prevChunkPosition)
                {
                    var baseChunkPos = ToChunkPosition(_position);
                    Parallel.For(-_distance, _distance + 1, dx =>
                    {
                        for (int dy = -_distance; dy <= _distance; dy++)
                        {
                            for (int dz = -_distance; dz <= _distance; dz++)
                            {
                                // Berechne die Chunk-Position relativ zur Basisposition
                                Vector3 offset = new Vector3(dx, dy, dz);
                                var chunkPos = baseChunkPos + offset;

                                // Hole den Chunk (eventuell mit Caching implementieren)
                                var chunk = WorldGenerator.GetChunk((int)chunkPos.X, (int)chunkPos.Y, (int)chunkPos.Z);

                                //var octree = WorldGenerator.BuildOctree(chunk);
                                
                                // Verarbeite den Chunk und schreibe ihn in das Map-Array
                                WriteChunkToMap1D(chunk, dx + _distance, dy + _distance, dz + _distance);
                            }
                        }
                    });

                    var chunkOrigin = _currChunkPosition - new Vector3(_distance);
                    var worldOffset = chunkOrigin * Chunk.Size;
                    _computeShader.Parameters["Offset"].SetValue(worldOffset);
                    _shaderMap.SetData(_map1D);
                }
            }
            
            if (!keyboard.IsKeyDown(Keys.Q))
            {
                _rayPosition = _position;
                _rayRotation = _rotation;

                _computeShader.Parameters["iTime"].SetValue((float)gameTime.TotalGameTime.TotalSeconds);
                ComputeRays();
            }

            if (keyboard.IsKeyDown(Keys.Space))
            {
                _computeShader.Parameters["LightPosition"].SetValue(_position);
            }
            
            //if (_position != _currentChunk && (_chunkLoaderTask == null || _chunkLoaderTask.IsCompleted))
            //{
            //    _chunkLoaderTask = Task.Run(() => LoadChunksAsync(_position, _chunkLoaderCts.Token));
            //}
            
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.SetRenderTarget(_rayCastTarget);
            GraphicsDevice.Clear(Color.Black);

            _spriteBatch.Begin(depthStencilState: DepthStencilState.Default, samplerState: SamplerState.PointWrap);
            _spriteBatch.Draw(_computeTexture, Vector2.Zero, Color.White);
            _spriteBatch.End();

            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Clear(Color.White);

            _spriteBatch.Begin(depthStencilState: DepthStencilState.Default, samplerState: SamplerState.PointWrap);
            _spriteBatch.Draw(_rayCastTarget, GraphicsDevice.Viewport.Bounds, Color.White);
            _spriteBatch.End();
            
            base.Draw(gameTime);
        }

        private int ToChunkCoord(float value)
        {
            return (int)(value >= 0 ? value / Chunk.Size : (value - Chunk.Size + 1) / Chunk.Size);
        }

        private Vector3 ToChunkPosition(Vector3 position)
        {
            var result = new Vector3(
                ToChunkCoord(position.X),
                ToChunkCoord(position.Y),
                ToChunkCoord(position.Z)
            );
            return result;
        }
        
        private Vector3 ToGrid(Vector3 position)
        {
            var (x, y, z) = position;
            if (x < 0)
            {
                x -= 1;
            }
            if (y < 0)
            {
                y -= 1;
            }
            if (z < 0)
            {
                z -= 1;
            }

            int gridX = (int)(x);
            int gridY = (int)(y);
            int gridZ = (int)(z);

            return new Vector3(gridX, gridY, gridZ);
        }

        private Vector3 ToGrid(float x, float y, float z)
        {
            return ToGrid(new Vector3(x, y, z));
        }

        private bool IsSolid(float x, float y, float z)
        {
            return false;
            var map = ToGrid(x, y, z);
            int gridX = (int)map.X;
            int gridY = (int)map.Y;
            int gridZ = (int)map.Z;
            if (gridY >= _mapY || gridY < 0)
            {
                return false;
            }
            if (gridZ >= _mapZ || gridZ < 0)
            {
                return false;
            }
            if (gridX >= _mapX || gridX < 0)
            {
                return false;
            }
            int index = gridX + _mapX * gridZ + _mapX * _mapZ * gridY;
            return _map1D[index] != 0;
        }

        private bool IsSolid(Vector3 position)
        {
            return IsSolid(position.X, position.Y, position.Z);
        }

        private void ComputeRays()
        {
            
            _computeShader.Parameters["Position"].SetValue(_rayPosition);
            _computeShader.Parameters["Rotation"].SetValue(_rayRotation);
            
            _computeShader.Parameters["Output"].SetValue(_computeTexture);
            _computeShader.Parameters["Width"].SetValue(_rayCastTargetResolutionX);
            _computeShader.Parameters["Height"].SetValue(_rayCastTargetResolutionY);

            //_computeShader.Parameters["Results"].SetValue(_rayResultBuffer);
            _computeShader.Parameters["CPUMap"].SetValue(_shaderMap);

            double count = (_rayCastTargetResolutionX / _rayResolution) * (_rayCastTargetResolutionY / _rayResolution);
            int groupCount = (int)Math.Ceiling(count / _computeGroupSize);

            foreach (var pass in _computeShader.CurrentTechnique.Passes)
            {
                pass.ApplyCompute();
                GraphicsDevice.DispatchCompute(groupCount, 1, 1);
            }
        }
    }
}
