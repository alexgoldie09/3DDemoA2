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

        // How far the cannon may roll from the island's centre: the island's flat edges are 8.56 m out,
        // minus about 1.6 m so the cannon's furthest point (barrel tip / wheel) stays on the grass
        private const float CANNONROLLLIMIT = 6.9f;

        // Debug view: what the model shader outputs to the screen, for checking normals, UVs, wireframe and the material maps.
        // The order must match the DebugView values in Model.frag (wireframe stays at 3)
        private static readonly string[] DebugViewNames =
            { "Texture", "Normals", "UVs", "Wireframe", "Metallic", "Roughness" };
        private int _debugView;
        
        // Lighting toggle: off shows the raw texture colour, for comparing lit vs unlit
        private bool _useLighting;

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

            // Load the scene. All three models were exported already sitting on y = 0,
            // so only sideways offsets are needed to place them on the island's grass without overlapping.
            // Each model names its four texture files (null = no map: the material's neutral default is used).
            _scene.Add(new StaticProp("Island", "Island.obj", Vector3.Zero, // grass top at y = 0
                colorMapFile: "IslandColorTexture.png",
                normalMapFile: null,
                metallicMapFile: null,
                roughnessMapFile: null));

            _scene.Add(new StaticProp("Alien", "TestAlien.obj", new Vector3(4f, 0f, 0f),
                colorMapFile: "AlienColorTexture.png",
                normalMapFile: null,
                metallicMapFile: null,
                roughnessMapFile: null));

            _scene.Add(new Cannon("Cannon", "Cannon.obj", new Vector3(-3f, 0f, 0f),
                colorMapFile: "CannonColorTexture.png",
                normalMapFile: "MetalNormalGLTexture.jpg",
                metallicMapFile: "MetalMetallicTexture.jpg",
                roughnessMapFile: "MetalRoughnessTexture.jpg",
                maxDistanceFromCentre: CANNONROLLLIMIT));
            
            _cannon = _scene[2] as Cannon;

            // --- Camera & input ---
            // Create the camera with the specified width, height, and initial position
            _camera = new Camera(_width, _height, _initialCameraPosition);

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

            // Keep the camera's aspect ratio in sync with the window
            _camera?.SetScreenSize(_width, _height);

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
            // Clear the color buffer and depth buffer
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            if (_modelShader != null && _camera != null)
            {
                // Bind the shader first: uniform uploads apply to the currently bound program
                _modelShader.Bind();

                // View, projection and debug view matrices: shared by every object, so set them once per frame
                _modelShader.SetMatrix4("view", _camera.GetViewMatrix());
                _modelShader.SetMatrix4("projection", _camera.GetProjectionMatrix());
                _modelShader.SetInt("DebugView", _debugView);
                _modelShader.SetInt("UseLighting", _useLighting ? 1 : 0);
                
                // Wireframe: rasterise only the triangle edges
                // Culling still applies, so with culling ON only the edges of front-facing triangles are drawn
                if (_debugView == 3)
                    GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line);
                
                // Lighting: a single directional light (the sun) and ambient light
                _modelShader.SetVector3("lightDir", new Vector3(-1.4f, -1f, -0.3f)); // sun from above, move a bit to one side
                _modelShader.SetVector3("lightColor", new Vector3(1f, 0.97f, 0.9f)); // warm daylight
                _modelShader.SetFloat("ambientStrength", 0.5f);
                _modelShader.SetVector3("cameraPos", _camera.Position);

                // Environment for reflections: a stand-in for the skybox, a gradient from sky (up) through a
                // pale horizon to grass (down). The sky matches the clear colour so metals reflect the background.
                _modelShader.SetVector3("skyColour", new Vector3(Color4.SkyBlue.R, Color4.SkyBlue.G, Color4.SkyBlue.B));
                _modelShader.SetVector3("horizonColour", new Vector3(0.85f, 0.9f, 0.95f));
                _modelShader.SetVector3("groundColour", new Vector3(0.3f, 0.38f, 0.22f));

                // Texture units: each material map has its own unit (albedo 0, normal 1, metallic 2, roughness 3)
                _modelShader.SetInt("AlbedoMap", Material.ALBEDOMAPUNIT);
                _modelShader.SetInt("NormalMap", Material.NORMALMAPUNIT);
                _modelShader.SetInt("MetallicMap", Material.METALLICMAPUNIT);
                _modelShader.SetInt("RoughnessMap", Material.ROUGHNESSMAPUNIT);

                // --- Scene ---
                // Each object applies its own materials (binding their maps) and uploads its own model matrices
                foreach (var renderable in _scene)
                    renderable.Render(_modelShader);
                
                // Back to filled polygons, so the debug text (and anything drawn later) isn't drawn as lines
                GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);
            }

            // Render the debug text on top of the 3D scene
            DebugText.Render();

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
                float upDown = (kb.IsKeyDown(Keys.Up) ? 0f : 1f) - (kb.IsKeyDown(Keys.Down) ? 0f : 1f);
                float leftRight = (kb.IsKeyDown(Keys.Left) ? 1f : 0f) - (kb.IsKeyDown(Keys.Right) ? 1f : 0f);

                if (shift)
                    _cannon.Elevate(upDown, deltaTime);
                else
                    _cannon.Roll(upDown, deltaTime);

                _cannon.Turn(leftRight, deltaTime);
            }

            // Cycle the debug view (texture - normals - UVs - wireframe) when the V key is pressed
            if (kb.IsKeyPressed(Keys.V))
                _debugView = (_debugView + 1) % DebugViewNames.Length;

            // Debug readouts (keyed, so they update in place instead of stacking)
            DebugText.Log($"FPS: {1.0 / deltaTime:0}", Color4.White, 0.5f, key: 1);
            DebugText.Log($"Press C for Backface Culling: {(_useBackfaceCulling ? "ON" : "OFF")}", Color4.Yellow, 0.5f, key: 2);
            DebugText.Log($"Press V for Debug View: {DebugViewNames[_debugView]}", Color4.MediumVioletRed, 0.5f, key: 5);
            DebugText.Log($"Press Tab to Toggle Lighting", Color4.DarkSlateBlue, 0.5f, key: 3);
            DebugText.Log($"Press Esc to Quit", Color4.Magenta, 0.5f, key: 4);
            DebugText.Log($"WASD: Camera Front/Back/Left/Right, Q/E: Camera Up/Down", Color4.DarkGreen, 0.5f, key: 6);
            DebugText.Log($"Arrows: Roll/Turn Cannon, Shift + Up/Down: Barrel", Color4.DarkGreen, 0.5f, key: 7);
            if (_cannon != null)
                DebugText.Log($"Barrel: {_cannon.Elevation:0} deg, Heading: {_cannon.Heading:0} deg",
                    Color4.DarkGreen, 0.5f, key: 8);
        }
        #endregion
    }
}
