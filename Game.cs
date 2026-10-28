using System.Diagnostics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using _3DDemoA2.Graphics;
using _3DDemoA2.Scene;
using _3DDemoA2.Debugging;

namespace _3DDemoA2
{
    /// <summary>
    /// Represents the main game class that handles rendering, input, and game logic.
    /// </summary>
    internal class Game : GameWindow
    {
        // width and height of screen
        private int _width;
        private int _height;

        // title of the game
        private readonly string _title;

        // Render pipeline vars
        private ShaderProgram? _modelShader;
        private bool _useBackfaceCulling;

        // Scene: everything drawn with the model shader, through the common IRenderable interface
        private readonly List<IRenderable> _scene = new();

        // The animated cannon (also in _scene) but kept separately so keyboard input can drive it
        private Cannon? _cannon;

        // The sky: drawn after the scene with its own shader, so it is kept outside _scene (see Skybox.cs)
        private Skybox? _skybox;

        // How far the cannon may roll from the island's centre: the island's flat edges are 8.56 m out,
        // minus about 1.6 m so the cannon's furthest point (barrel tip / wheel) stays on the grass
        private const float CANNONROLLLIMIT = 6.9f;

        // Debug view: what the model shader outputs to the screen (see DebugView.cs, shared with Model.frag)
        private static readonly int DebugViewCount = Enum.GetValues<DebugView>().Length;
        private DebugView _debugView;
        
        // Lighting toggle: off shows the raw texture colour, for comparing lit vs unlit
        private bool _useLighting;

        // Frame timing readout, averaged over half a second so the numbers are readable
        // CPU render time is how long this program spends issuing a frame's work, which is the part code changes affect.
        private const double TIMINGSAMPLESECONDS = 0.5;
        private readonly Stopwatch _renderTimer = new();
        private double _sampleElapsed;
        private double _sampleFrameTime;
        private double _sampleRenderTime;
        private int _sampleFrames;
        private double _averageFrameMs;
        private double _averageRenderMs;

        // Camera vars
        private Camera? _camera;
        private readonly Vector3 _initialCameraPosition = new (0f, 2f, 25f); // in front of the island, just above the grass

        /// <summary>
        /// Constructor that initialises a new instance of the Game class with the specified width, height, and title.
        /// </summary>
        /// <param name="width">The width of the game window.</param>
        /// <param name="height">The height of the game window.</param>
        /// <param name="title">The title of the game window.</param>
        /// <param name="useBackfaceCulling">Whether to enable backface culling for rendering.</param>
        /// <param name="useLighting">Whether to enable lighting for rendering.</param>
        public Game(int width, int height, string title, bool useBackfaceCulling = true, bool useLighting = true)
            : base(GameWindowSettings.Default, NativeWindowSettings.Default)
        {
            _width = width;
            _height = height;
            _useBackfaceCulling = useBackfaceCulling;
            _useLighting = useLighting;
            _title = title;

            // Set the window size and title
            Title = _title;

            // Center the window
            CenterWindow(new Vector2i(_width, _height));
        }

        #region OpenGL Overrides

        /// <summary>
        /// Called once when the window is created.
        /// </summary>
        protected override void OnLoad()
        {
            base.OnLoad();

            // --- Debugging Setup ---
            // Initialise the debug text system with the window width and height
            DebugText.Initialize(_width, _height);
            DebugText.TextScale = 1.4f;

            // --- Render state ---
            // Set the clear color to SkyBlue, enable depth testing and face culling (if enabled)
            GL.ClearColor(Color4.SkyBlue);
            GL.Enable(EnableCap.DepthTest);
            if (_useBackfaceCulling)
                GL.Enable(EnableCap.CullFace);

            // Front faces are wound counter-clockwise (OpenGL's default, and what Blender exports),
            // so the back face of every triangle is culled when culling is on
            GL.FrontFace(FrontFaceDirection.Ccw);

            // Set the VSync mode to On to synchronize the frame rate with the monitor's refresh rate
            VSync = VSyncMode.On;

            // --- GPU resources ---
            // Create the shader program used for every textured model
            _modelShader = new ShaderProgram("Model.vert", "Model.frag");

            // --- Uniforms that never change ---
            // Uniforms belong to the program and keep their value until changed, so values that stay the same
            // for the whole run are uploaded once here instead of every frame.
            _modelShader.Bind();

            // Lighting: a single directional light (the sun) and ambient light
            _modelShader.SetVector3("lightDir", new Vector3(-1.4f, -1f, -0.3f)); // sun from above, move a bit to one side
            _modelShader.SetVector3("lightColor", new Vector3(1f, 0.97f, 0.9f)); // warm daylight
            _modelShader.SetFloat("ambientStrength", 0.5f);

            // Texture units: each material map has its own unit (albedo 0, normal 1, metallic 2, roughness 3)
            _modelShader.SetInt("AlbedoMap", Material.ALBEDOMAPUNIT);
            _modelShader.SetInt("NormalMap", Material.NORMALMAPUNIT);
            _modelShader.SetInt("MetallicMap", Material.METALLICMAPUNIT);
            _modelShader.SetInt("RoughnessMap", Material.ROUGHNESSMAPUNIT);

            // The sky cube map is on its own unit, for metal reflections
            _modelShader.SetInt("SkyboxMap", Skybox.CUBEMAPUNIT);

            // Load the scene. All three models were exported already sitting on y = 0,
            // so only sideways offsets are needed to place them on the island's grass without overlapping.
            // Each model names its four texture files (null = no map: the material's neutral default is used).
            _scene.Add(new StaticProp("Island", "Island.obj", Vector3.Zero,
                colorMapFile: "IslandColorTexture.png",
                normalMapFile: null,
                metallicMapFile: null,
                roughnessMapFile: null));

            _scene.Add(new StaticProp("Alien", "TestAlien.obj", new Vector3(4f, 0f, 0f),
                colorMapFile: "AlienColorTexture.png",
                normalMapFile: "AlienNormalTexture.png",
                metallicMapFile: "AlienMetallicTexture.png",
                roughnessMapFile: "AlienRoughnessTexture.png"));

            // The cannon is kept in its own field as well (for the keyboard controls), so it is created first
            // and then added, rather than looked up by its position in the list
            _cannon = new Cannon("Cannon", "Cannon.obj", new Vector3(-3f, 0f, 0f),
                colorMapFile: "CannonColorTexture.png",
                normalMapFile: "MetalNormalGLTexture.jpg",
                metallicMapFile: "MetalMetallicTexture.jpg",
                roughnessMapFile: "MetalRoughnessTexture.jpg",
                maxDistanceFromCentre: CANNONROLLLIMIT);
            _scene.Add(_cannon);

            // --- Skybox ---
            // Six faces in OpenGL's cube map target order: +X, -X, +Y, -Y, +Z, -Z.
            _skybox = new Skybox(["px.png", "nx.png", "py.png", "ny.png", "pz.png", "nz.png"]);

            // --- Camera & input ---
            // Create the camera with the specified width, height, and initial position
            _camera = new Camera(_width, _height, _initialCameraPosition);

            // Upload the camera's projection matrix to the model shader, which depends on the window size
            UploadProjection();

            // Grab and hide the cursor for mouse-look
            CursorState = CursorState.Grabbed;
        }

        /// <summary>
        /// Called once when the window is closed.
        /// </summary>
        protected override void OnUnload()
        {
            base.OnUnload();

            // Delete the OpenGL resources to free up GPU memory
            foreach (var renderable in _scene)
                renderable.Delete();
            _scene.Clear();
            Material.DeleteDefaults();
            _skybox?.Delete();
            _modelShader?.Delete();
            // Shutdown the debug text system
            DebugText.Shutdown();
        }

        /// <summary>
        /// Called when the window is resized.
        /// </summary>
        /// <param name="e">The resize event arguments.</param>
        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);

            // Update height and widths
            _width = e.Width;
            _height = e.Height;

            // Keep the camera's aspect ratio in sync with the window, and re-upload the projection that depends on it
            _camera?.SetScreenSize(_width, _height);
            UploadProjection();

            // Change the title of the window
            Title = $"{_title} - {_width}x{_height}";

            // Update the debug text system with the new window size
            DebugText.SetScreenSize(_width, _height);
        }

        /// <summary>
        /// Called when the framebuffer (actual pixel size) changes.
        /// On Retina/High DPI screens this is larger than the window size, so the viewport must use it.
        /// </summary>
        protected override void OnFramebufferResize(FramebufferResizeEventArgs e)
        {
            base.OnFramebufferResize(e);
            GL.Viewport(0, 0, e.Width, e.Height);
        }

        /// <summary>
        /// Called every frame to update the rendering and drawing.
        /// </summary>
        protected override void OnRenderFrame(FrameEventArgs args)
        {
            // Time the work this frame issues (stopped before SwapBuffers, which is where VSync waits)
            _renderTimer.Restart();

            // Clear the color buffer and depth buffer
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            if (_modelShader != null && _camera != null)
            {
                // Bind the shader first: uniform uploads apply to the currently bound program
                _modelShader.Bind();

                // Only what can change between frames is uploaded here: the camera and the debug toggles
                // (the light, environment, texture units and projection are set in OnLoad / OnResize)
                Matrix4 view = _camera.GetViewMatrix(); // shared by the scene and the skybox
                _modelShader.SetMatrix4("view", view);
                _modelShader.SetVector3("cameraPos", _camera.Position);
                _modelShader.SetInt("DebugView", (int)_debugView);
                _modelShader.SetInt("UseLighting", _useLighting ? 1 : 0);
                
                // Wireframe: rasterise only the triangle edges
                // Culling still applies, so with culling ON only the edges of front-facing triangles are drawn
                if (_debugView == DebugView.Wireframe)
                    GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line);
                
                // --- Scene ---
                // Reset the last applied material so every object applies its own materials (binding their maps)
                Material.ResetLastApplied();

                // Each object applies its own materials (binding their maps) and uploads its own model matrices
                foreach (var renderable in _scene)
                    renderable.Render(_modelShader);
                
                // Back to filled polygons, so the sky and debug text aren't drawn as lines
                GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);

                // --- Skybox ---
                // Drawn last (before the text) at the far plane: pixels already covered by the scene fail the
                // depth test, so the cube map is only sampled where the sky is actually visible
                _skybox?.Render(view);
            }

            // Render the debug text on top of the 3D scene
            DebugText.Render();

            // Stop timing before the swap, and add this frame to the averages
            _renderTimer.Stop();
            UpdateTimingAverages(args.Time, _renderTimer.Elapsed.TotalMilliseconds);

            // Swap the front and back buffers to display the rendered frame
            Context.SwapBuffers();

            base.OnRenderFrame(args);
        }

        /// <summary>
        /// Called every frame to update the game logic.
        /// </summary>
        /// <param name="args">The frame event arguments.</param>
        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            MouseState ms = MouseState; // Get the current mouse state
            KeyboardState kb = KeyboardState; // Get the current keyboard state

            base.OnUpdateFrame(args);

            float deltaTime = (float)args.Time;

            // Update the debug text system with the elapsed time for this frame
            DebugText.Update(deltaTime);

            // Close the program when the Escape key is pressed
            if (kb.IsKeyPressed(Keys.Escape))
            {
                Close();
                return;
            }

            // Toggle lighting on/off when the Tab key is pressed
            if (kb.IsKeyPressed(Keys.Tab))
                _useLighting = !_useLighting;
            
            // Update the camera based on keyboard and mouse input, capturing the mouse when the window is focused
            _camera?.Update(kb, ms, args, captureMouse: true);

            // Toggle backface culling when the C key is pressed
            if (kb.IsKeyPressed(Keys.C))
            {
                _useBackfaceCulling = !_useBackfaceCulling;
                if (_useBackfaceCulling)
                    GL.Enable(EnableCap.CullFace);
                else
                    GL.Disable(EnableCap.CullFace);
            }

            // Cannon controls: arrows roll and turn, Shift + Up/Down raises and lowers the barrel
            if (_cannon != null)
            {
                bool shift = kb.IsKeyDown(Keys.LeftShift) || kb.IsKeyDown(Keys.RightShift);
                float upDown = (kb.IsKeyDown(Keys.Down) ? 1f : 0f) - (kb.IsKeyDown(Keys.Up) ? 1f : 0f);
                float leftRight = (kb.IsKeyDown(Keys.Left) ? 1f : 0f) - (kb.IsKeyDown(Keys.Right) ? 1f : 0f);

                if (shift)
                    _cannon.Elevate(upDown, deltaTime);
                else
                    _cannon.Roll(upDown, deltaTime);

                _cannon.Turn(leftRight, deltaTime);
            }

            // Cycle the debug view (texture - normals - UVs - wireframe) when the V key is pressed
            if (kb.IsKeyPressed(Keys.V))
                _debugView = (DebugView)(((int)_debugView + 1) % DebugViewCount);

            // Debug readouts (keyed, so they update in place instead of stacking)
            DebugText.Log($"Frame: {_averageFrameMs:0.0} ms ({1000.0 / Math.Max(_averageFrameMs, 0.001):0} FPS), " +
                          $"CPU render: {_averageRenderMs:0.00} ms",
                Color4.White, 0.5f, key: 1);
            DebugText.Log($"Press C for Backface Culling: {(_useBackfaceCulling ? "ON" : "OFF")}", Color4.Yellow, 0.5f, key: 2);
            DebugText.Log($"Press V for Debug View: {_debugView}", Color4.MediumVioletRed, 0.5f, key: 5);
            DebugText.Log($"Press Tab to Toggle Lighting", Color4.DarkSlateBlue, 0.5f, key: 3);
            DebugText.Log($"Press Esc to Quit", Color4.Magenta, 0.5f, key: 4);
            DebugText.Log($"WASD: Camera Front/Back/Left/Right, Q/E: Camera Up/Down", Color4.DarkGreen, 0.5f, key: 6);
            DebugText.Log($"Arrows: Roll/Turn Cannon, Shift + Up/Down: Barrel", Color4.DarkGreen, 0.5f, key: 7);
            if (_cannon != null)
                DebugText.Log($"Barrel: {_cannon.Elevation:0} deg, Heading: {_cannon.Heading:0} deg",
                    Color4.DarkGreen, 0.5f, key: 8);
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// Uploads the camera's projection matrix to the model and skybox shaders (on load and whenever the window resizes).
        /// </summary>
        private void UploadProjection()
        {
            if (_camera == null)
                return;

            Matrix4 projection = _camera.GetProjectionMatrix();

            if (_modelShader != null)
            {
                _modelShader.Bind();
                _modelShader.SetMatrix4("projection", projection);
            }

            _skybox?.SetProjection(projection);
        }

        /// <summary>
        /// Adds one frame's timings to the running totals, and every half second turns them into averages
        /// (a single frame's numbers jump around too much to read).
        /// </summary>
        /// <param name="frameSeconds">Time since the previous frame, in seconds.</param>
        /// <param name="renderMilliseconds">CPU time spent issuing this frame, in milliseconds.</param>
        private void UpdateTimingAverages(double frameSeconds, double renderMilliseconds)
        {
            _sampleElapsed += frameSeconds;
            _sampleFrameTime += frameSeconds * 1000.0;
            _sampleRenderTime += renderMilliseconds;
            _sampleFrames++;

            if (_sampleElapsed < TIMINGSAMPLESECONDS)
                return;

            _averageFrameMs = _sampleFrameTime / _sampleFrames;
            _averageRenderMs = _sampleRenderTime / _sampleFrames;

            _sampleElapsed = 0;
            _sampleFrameTime = 0;
            _sampleRenderTime = 0;
            _sampleFrames = 0;
        }
        #endregion
    }
}
