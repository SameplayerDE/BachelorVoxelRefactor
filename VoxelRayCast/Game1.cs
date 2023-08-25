using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;

namespace VoxelRayCast
{
    public class Game1 : Game
    {

        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private Texture2D _pixel;
        private Texture2D _computeTexture;
        private Texture3D _textureAtlas;

        private SpriteFont _font;
        private Effect _effect;

        private List<RayResult3D> _results = new List<RayResult3D>();

        private Effect _computeShader;
        const int ComputeGroupSize = 64;
        private StructuredBuffer _rayResultBuffer;
        private StructuredBuffer _shaderMap;
        private StructuredBuffer _shaderMapC;
        private int _maxCount = 1_000_000;

        private RenderTarget2D _virtualScreen;
        private const int _virtualResolutionX = 240;
        private const int _virtualResolutionY = 240;

        private RenderTarget2D _rayCastTarget;
        private const int _rayCastTargetResolutionX = 1920 / 2;
        private const int _rayCastTargetResolutionY = 1080 / 2;

        private const int _mapX = 256;
        private const int _mapY = 256;
        private const int _mapZ = 256;

        private int[,,] _map;
        private int[] _map1D;
        private float[] _map1DC;

        private Matrix _world;
        private Matrix _view;
        private Matrix _projection;

        private int _selection = 1;

        private Texture2D[] _textures;

        //Player
        //private Vector3 _position = new Vector3(-50, 250, -50);
        private Vector3 _position = new Vector3(-32, 128, -32);
        private Vector3 _rayPosition = new Vector3(0, 0f, 0);
        private Vector3 _rotation = new Vector3(MathHelper.ToRadians(15), MathHelper.ToRadians(45), 0);
        private Vector3 _rayRotation;
        private Vector3 _direction;
        private float _movementSpeed = 32.0f;

        private float _xxx = 1.0f;
        private float _fov = 90;
        private float _rayCasterFOV = 360;
        private int _rayResolution = 1;
        private bool _requestRender = false;

        private KeyboardState _prevState;
        private KeyboardState _currState;

        private MouseState _prevMouseState;
        private MouseState _currMouseState;

        private bool _centerMouse = false;

        public Game1()
        {
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            Window.AllowUserResizing = true;

            _graphics = new GraphicsDeviceManager(this);
            _graphics.IsFullScreen = false;
            _graphics.GraphicsProfile = GraphicsProfile.HiDef;
        }

        protected override void Initialize()
        {
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

            _virtualScreen = new RenderTarget2D(GraphicsDevice, _virtualResolutionX, _virtualResolutionY, false, GraphicsDevice.PresentationParameters.BackBufferFormat, DepthFormat.Depth24);
            _rayCastTarget = new RenderTarget2D(GraphicsDevice, _rayCastTargetResolutionX, _rayCastTargetResolutionY, false, GraphicsDevice.PresentationParameters.BackBufferFormat, DepthFormat.Depth24);

            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _font = Content.Load<SpriteFont>("Font");
            _effect = Content.Load<Effect>("vpc");

            _computeShader = Content.Load<Effect>("Ray3D");
            var texture = Content.Load<Texture2D>("dirt");

            //_computeShader.Parameters["Input"].SetValue(texture);
            _computeShader.Parameters["InputW"].SetValue(texture.Width);
            _computeShader.Parameters["InputH"].SetValue(texture.Height);

            _pixel = new Texture2D(GraphicsDevice, 1, 1);
            _pixel.SetData(new Color[] { Color.White });

            _map = new int[_mapY, _mapZ, _mapX]; // y, z, x
            _rayResultBuffer = new StructuredBuffer(GraphicsDevice, typeof(RayResult3D), _maxCount, BufferUsage.None, ShaderAccess.ReadWrite);
            _shaderMap = new StructuredBuffer(GraphicsDevice, typeof(int), _mapX * _mapY * _mapZ, BufferUsage.None, ShaderAccess.ReadWrite);

            Random rand = new Random();
            _map1D = new int[_mapX * _mapZ * _mapY];
            // Create and configure FastNoise object
            FastNoiseLite noise = new FastNoiseLite();
            noise.SetSeed(101199);
            noise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);

            //float scale = 1f;
            //
            //for (int y = 0; y < _mapY; y++)
            //{
            //    for (int z = 0; z < _mapZ; z++)
            //    {
            //        for (int x = 0; x < _mapX; x++)
            //        {
            //
            //            //int id = _map[y, z, x];
            //            float nValue = noise.GetNoise(x * scale, z * scale) + 1;
            //            //nValue *= 30;
            //            //nValue += 10;
            //            //nValue = Math.Clamp(nValue, 0, _mapY);
            //            //
            //            //for (int y = 0; y < nValue; y++)
            //            //{
            //            //    int index = x + _mapX * z + _mapX * _mapZ * y;
            //            //    _map1D[index] = (int)nValue;
            //            //    _map[y, z, x] = (int)nValue;
            //            //}
            //            int index = x + _mapX * z + _mapX * _mapZ * y;
            //            _map1D[index] = id;
            //            //int index = x + _mapX * z + _mapX * _mapZ * y;
            //            //id = _map[y, z, x];
            //            //id = (int)nValue;
            //            //_map1D[index] = id > 0 ? 1 : 0;
            //        }
            //    }
            //}

            float scale = 1f;

            for (int y = 0; y < _mapY; y++)
            {
                for (int z = 0; z < _mapZ; z++)
                {
                    for (int x = 0; x < _mapX; x++)
                    {

                        //int id = _map[y, z, x];
                        float nValue = Math.Max(noise.GetNoise(x * scale, y * scale, z * scale), 0);
                        //nValue *= 30;
                        //nValue += 10;
                        //nValue = Math.Clamp(nValue, 0, _mapY);
                        //
                        if (nValue > 0f)
                        {
                            //_map[y, z, x] = (int)Math.Abs(Math.Abs(x - 128)) + (int)Math.Abs(Math.Abs(z - 128));
                            //_map[y, z, x] = y;
                            _map[y, z, x] = rand.Next(1, 5);
                        }
                        int id = _map[y, z, x];
                        int index = x + _mapX * z + _mapX * _mapZ * y;
                        _map1D[index] = id;
                        //for (int yx = 0; yx < nValue; yx++)
                        //{
                        //    int index = x + _mapX * z + _mapX * _mapZ * y;
                        //    _map1D[index] = (int)nValue;
                        //   _map[y, z, x] = (int)nValue;
                        //}

                        //int index = x + _mapX * z + _mapX * _mapZ * y;

                        //id = (int)nValue;
                        //_map1D[index] = id > 0 ? 1 : 0;
                    }
                }
            }


            Color[] atlas = new Color[16 * 16 * 7];
            for (int i = 0; i < 7; i++)
            {
                Color[] copy = new Color[16 * 16];
                _textures[i].GetData(copy);
                for (int y = 0; y < 16; y++)
                {
                    for (int x = 0; x < 16; x++)
                    {
                        int index3D = x + 16 * y + 16 * 16 * i;
                        int index2D = x + 16 * y;
                        atlas[index3D] = copy[index2D];
                    }
                }
            }

            _computeShader.Parameters["MapMaxX"].SetValue(_mapX);
            _computeShader.Parameters["MapMaxY"].SetValue(_mapY);
            _computeShader.Parameters["MapMaxZ"].SetValue(_mapZ);

            _textureAtlas.SetData(atlas);
            _shaderMap.SetData(_map1D);

            //_computeShader.Parameters["Input"].SetValue(_textureAtlas);

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

            //Random rand = new Random();
            //// Create and configure FastNoise object
            //FastNoiseLite noise = new FastNoiseLite();
            //noise.SetSeed(101199);
            //noise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
            //
            //for (int y = 0; y < _mapY; y++)
            //{
            //    for (int z = 0; z < _mapZ; z++)
            //    {
            //        for (int x = 0; x < _mapX; x++)
            //        {
            //
            //            //int id = _map[y, z, x];
            //            float nValue = Math.Max(noise.GetNoise(x + (float)gameTime.TotalGameTime.TotalSeconds, y + (float)gameTime.TotalGameTime.TotalSeconds, z + (float)gameTime.TotalGameTime.TotalSeconds), 0);
            //            //nValue *= 30;
            //            //nValue += 10;
            //            //nValue = Math.Clamp(nValue, 0, _mapY);
            //            //
            //            int index = x + _mapX * z + _mapX * _mapZ * y;
            //            if (nValue > 0f)
            //            {   
            //                _map1D[index] = 1;
            //            }
            //            else
            //            {
            //                _map1D[index] = 0;
            //            }
            //
            //            //for (int yx = 0; yx < nValue; yx++)
            //            //{
            //            //    int index = x + _mapX * z + _mapX * _mapZ * y;
            //            //    _map1D[index] = (int)nValue;
            //            //   _map[y, z, x] = (int)nValue;
            //            //}
            //
            //            //int index = x + _mapX * z + _mapX * _mapZ * y;
            //
            //            //id = (int)nValue;
            //            //_map1D[index] = id > 0 ? 1 : 0;
            //        }
            //    }
            //}
            //_shaderMap.SetData(_map1D);

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
                var mouse = Mouse.GetState();
                var screenCenter = GraphicsDevice.Viewport.Bounds.Center;
                var mouseDelta = (mouse.Position - screenCenter).ToVector2();

                if (mouseDelta.Length() != 0)
                {
                    mouseDelta /= 10f;
                    //mouseDelta.Normalize();
                }

                //_rotation.Y -= mouseDelta.X * delta;
                //_rotation.X += mouseDelta.Y * delta;

                /*int rotationYKeys = (keyboard.IsKeyDown(Keys.Left) ? -1 : 0) + (keyboard.IsKeyDown(Keys.Right) ? 1 : 0);
                int rotationXKeys = (keyboard.IsKeyDown(Keys.Down) ? -1 : 0) + (keyboard.IsKeyDown(Keys.Up) ? 1 : 0);

                _rotation.Y -= MathHelper.ToRadians(90f) * delta * rotationYKeys;
                _rotation.X -= MathHelper.ToRadians(90f) * delta * rotationXKeys;*/

                //if (_rotation.Y < 0)
                //{
                //    _rotation.Y += MathHelper.TwoPi;
                //}
                //else if (_rotation.Y >= MathHelper.TwoPi)
                //{
                //    _rotation.Y -= MathHelper.TwoPi;
                //}
                //
                //_rotation.X = Math.Clamp(_rotation.X, MathHelper.ToRadians(-89f), MathHelper.ToRadians(89f));

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

            //if (_currMouseState.LeftButton == ButtonState.Pressed && _prevMouseState.LeftButton == ButtonState.Released)
            //{
            //    var ray = DDACalculator.RunIteration3D(IsSolid, GetSolid, _rayPosition.X, _rayPosition.Y, _rayPosition.Z, _rayRotation.X, _rayRotation.Y, _rayRotation.Z, 0, 0, 0);
            //    if (ray.Hit == 1)
            //    {
            //
            //        BreakBlock((int)ray.MapPosition.X, (int)ray.MapPosition.Y, (int)ray.MapPosition.Z);
            //    }
            //
            //
            //}
            //_computeShader.Parameters["LightPosition"].SetValue(_position);
            if (_prevMouseState.ScrollWheelValue < _currMouseState.ScrollWheelValue)
            {
                _selection += 1;
            }

            if (_prevMouseState.ScrollWheelValue > _currMouseState.ScrollWheelValue)
            {
                _selection -= 1;
            }

            _selection = Math.Clamp(_selection, 1, 7);

            //_computeShader.Parameters["LightPosition"].SetValue(_position);

            //if (_currMouseState.RightButton == ButtonState.Pressed && _prevMouseState.RightButton == ButtonState.Released)
            //{
            //    var ray = DDACalculator.RunIteration3D(IsSolid, GetSolid, _rayPosition.X, _rayPosition.Y, _rayPosition.Z, _rayRotation.X, _rayRotation.Y, _rayRotation.Z, 0, 0, 0);
            //    if (ray.Hit == 1)
            //    {
            //        int x = 0;
            //        int y = 0;
            //        int z = 0;
            //
            //        if (ray.Side == (int)Side.X)
            //        {
            //            if (ray.SideOrientation == (int)SideOrientation.Positiv)
            //            {
            //                x += 1;
            //            }
            //            if (ray.SideOrientation == (int)SideOrientation.Negativ)
            //            {
            //                x -= 1;
            //            }
            //        }
            //
            //        if (ray.Side == (int)Side.Z)
            //        {
            //            if (ray.SideOrientation == (int)SideOrientation.Positiv)
            //            {
            //                z += 1;
            //            }
            //            if (ray.SideOrientation == (int)SideOrientation.Negativ)
            //            {
            //                z -= 1;
            //            }
            //        }
            //
            //        if (ray.Side == (int)Side.Y)
            //        {
            //            if (ray.SideOrientation == (int)SideOrientation.Positiv)
            //            {
            //                y += 1;
            //            }
            //            if (ray.SideOrientation == (int)SideOrientation.Negativ)
            //            {
            //                y -= 1;
            //            }
            //        }
            //
            //        PlaceBlock((int)ray.MapPosition.X + x, (int)ray.MapPosition.Y + y, (int)ray.MapPosition.Z + z, _selection);
            //    }
            //
            //    //_computeShader.Parameters["LightPosition"].SetValue(_position);
            //}

            _computeShader.Parameters["RotationMatrix"].SetValue(Matrix.CreateRotationX(_rotation.X) * Matrix.CreateRotationY(_rotation.Y) * Matrix.CreateRotationZ(_rotation.Z));

            var n_rotation =
                Matrix.CreateRotationX(_rotation.X * 0) *
                Matrix.CreateRotationY(_rotation.Y) *
                Matrix.CreateRotationZ(_rotation.Z);

            _direction = Vector3.Transform(Vector3.Backward, n_rotation);
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
            }

            _world = Matrix.Identity;
            _projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(_fov), GraphicsDevice.Viewport.AspectRatio, 0.01f, 100f);
            _view = Matrix.CreateLookAt(_position, _position + _direction, new Vector3(0, 1, 0));

            if (!keyboard.IsKeyDown(Keys.Q))
            {
                _rayPosition = _position;
                _rayRotation = _rotation;

                _computeShader.Parameters["iTime"].SetValue((float)gameTime.TotalGameTime.TotalSeconds);
                ComputeRays();

                //var results = new RayResult3D[_rayCastTargetResolutionX * _rayCastTargetResolutionY];
                //_rayResultBuffer.GetData(results, 0, _rayCastTargetResolutionX * _rayCastTargetResolutionY);
                //
                //_results.Clear();
                //_results.AddRange(results);
                //
                _requestRender = true;
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.SetRenderTarget(_rayCastTarget);
            GraphicsDevice.Clear(Color.Black);

            _spriteBatch.Begin(depthStencilState: DepthStencilState.Default, samplerState: SamplerState.PointWrap);
            _spriteBatch.Draw(_computeTexture, Vector2.Zero, Color.White);
            _spriteBatch.End();

            //GraphicsDevice.SetRenderTarget(_rayCastTarget);
            //GraphicsDevice.Clear(Color.White);
            //
            //for (int y = 0; y < _map.GetLength(0); y++)
            //{
            //    for (int z = 0; z < _map.GetLength(1); z++)
            //    {
            //        for (int x = 0; x < _map.GetLength(2); x++)
            //        {
            //            if (!IsSolid(x, y, z))
            //            {
            //                continue;
            //            }
            //            DrawCube(x, y + 1, z, 1f);
            //        }
            //    }
            //}

            //foreach (var result in _results)
            //{
            //    DrawLine(result.From - new Vector3(0, 0.05f, 0), result.To - new Vector3(0, 0.05f, 0));
            //}

            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Clear(Color.White);

            _spriteBatch.Begin(depthStencilState: DepthStencilState.Default, samplerState: SamplerState.PointWrap);

            //_spriteBatch.Draw(_virtualScreen, GraphicsDevice.Viewport.Bounds, Color.White);
            //_spriteBatch.Draw(_rayCastTarget, new Rectangle(0, 0, 256, 256), Color.White);
            _spriteBatch.Draw(_rayCastTarget, GraphicsDevice.Viewport.Bounds, Color.White);
            //_spriteBatch.Draw(_textures[_selection - 1], new Rectangle(0, 0, 64, 64), Color.White);

            //_spriteBatch.DrawString(_font, _selection + "", Vector2.Zero, Color.White);

            _spriteBatch.End();


            if (Keyboard.GetState().IsKeyDown(Keys.Space))
            {
                Stream stream = File.Create("image_" + DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss") + ".png");
                _rayCastTarget.SaveAsPng(stream, _rayCastTarget.Width, _rayCastTarget.Height);
                stream.Dispose();
                _rayCastTarget.Dispose();
                Exit();
            }
            base.Draw(gameTime);
        }

        private int GetSolid(float x, float y, float z)
        {
            var map = ToGrid(x, y, z);
            int gridX = (int)map.X;
            int gridY = (int)map.Y;
            int gridZ = (int)map.Z;


            if (gridY >= _mapY || gridY < 0)
            {
                return 0;
            }
            if (gridZ >= _mapZ || gridZ < 0)
            {
                return 0;
            }
            if (gridX >= _mapX || gridX < 0)
            {
                return 0;
            }
            int index = gridX + _mapX * gridZ + _mapX * _mapZ * gridY;
            return _map1D[index];
            return _map[gridY, gridZ, gridX];
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

        private void DrawLine(Vector3 from, Vector3 to)
        {
            _world = Matrix.Identity;
            _effect.Parameters["WorldViewProjection"].SetValue(_world * _view * _projection);
            foreach (EffectPass pass in _effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, new VertexPositionColor[]
                {
                    new VertexPositionColor(from, Color.CornflowerBlue),
                    new VertexPositionColor(to, Color.Black)
                }, 0, 1);
            }
        }

        private void BreakBlock(int x, int y, int z)
        {
            int index = x + _mapX * z + _mapX * _mapZ * y;
            _map1D[index] = 0;
            _shaderMap.SetData(_map1D);
        }

        private void PlaceBlock(int x, int y, int z, int id)
        {
            int index = x + _mapX * z + _mapX * _mapZ * y;
            _map1D[index] = id;
            _shaderMap.SetData(_map1D);
        }

        private void ComputeRays()
        {

            //_computeShader.Parameters["CPUMap"].SetValue(_sahderMap);
            _computeShader.Parameters["Position"].SetValue(_rayPosition);
            _computeShader.Parameters["Rotation"].SetValue(_rayRotation);

            //_computeShader.Parameters["LightDirection"].SetValue(_direction);

            _computeShader.Parameters["Output"].SetValue(_computeTexture);
            _computeShader.Parameters["Width"].SetValue(_rayCastTargetResolutionX);
            _computeShader.Parameters["Height"].SetValue(_rayCastTargetResolutionY);

            _computeShader.Parameters["Results"].SetValue(_rayResultBuffer);
            _computeShader.Parameters["CPUMap"].SetValue(_shaderMap);

            double count = (_rayCastTargetResolutionX / _rayResolution) * (_rayCastTargetResolutionY / _rayResolution);
            int groupCount = (int)Math.Ceiling((double)count / ComputeGroupSize);

            foreach (var pass in _computeShader.CurrentTechnique.Passes)
            {
                pass.ApplyCompute();
                GraphicsDevice.DispatchCompute(groupCount, 1, 1);
            }
        }

    }
}
