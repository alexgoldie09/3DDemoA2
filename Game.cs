using System.Diagnostics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using _3DDemoA2.Graphics;
using _3DDemoA2.World;
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
        
        // rotation vars
        private float _rotationSpeed = 60f; // degrees per second
        private Vector3 _rotDir = Vector3.UnitY; // rotation direction for the pyramid
        
        // scale vars
        private float _elapsedTime = 0f; // elapsed time since the last frame, used for smooth animations
        private float _amplitudeAmt = 0.3f; // amplitude of the pulsing scale effect for the cube
        private float _scaleSpeed = 2f; // speed of the pulsing scale effect for the cube
        private float _scaleBase = 1f; // base scale of the cube
        
        // Render pipeline vars
        private Chunk? _pyramid;
        private Chunk? _cube;
        private ShaderProgram? _shaderProgram;
        private bool _useBackfaceCulling;
        
        // Camera vars
        private Camera? _camera; 
        private readonly Vector3 _initialCameraPosition = new Vector3(1f, 0f, 2.5f);
        
        // Mouse cursor state
        private bool _isCursorOn;

        /// <summary>
        /// Constructor that initialises a new instance of the Game class with the specified width, height, and title.
        /// </summary>
        /// <param name="width">The width of the game window.</param>
        /// <param name="height">The height of the game window.</param>
        /// <param name="title">The title of the game window.</param>
        /// <param name="useBackfaceCulling">Whether to enable backface culling for rendering.</param>
        public Game(int width, int height, string title, bool useBackfaceCulling = true)
            : base(GameWindowSettings.Default, NativeWindowSettings.Default)
        {
            _width = width;
            _height = height;
            _useBackfaceCulling = useBackfaceCulling;
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
            DebugText.TextScale = 2f; // Set the scale of the debug text

            // --- Render state ---
            // Set the clear color to Dark Magenta, enable depth testing and face culling (if enabled)
            GL.ClearColor(Color4.DarkMagenta);
            GL.Enable(EnableCap.DepthTest);
            if (_useBackfaceCulling)
                GL.Enable(EnableCap.CullFace);

            // Set the front face winding order to clockwise
            // The indices are wound clockwise, so clockwise triangles are the front faces
            GL.FrontFace(FrontFaceDirection.Cw);

            // Set the VSync mode to On to synchronize the frame rate with the monitor's refresh rate
            VSync = VSyncMode.On;

            // --- GPU resources ---
            // Create the shader program and load the vertex and fragment shaders
            _shaderProgram = new ShaderProgram("Default.vert", "Default.frag");

            // Create the pyramid chunk with position, shapes, and textures
            try
            {
                _pyramid = new Chunk(Vector3.Zero, ShapeType.Pyramid, "HieroglyphArt.jpeg");
                DebugText.Log("Pyramid created successfully.", Color4.LightGreen, 6f);
            }
            catch (Exception e)
            {
                DebugText.Log($"Pyramid failed: {e.Message}", Color4.Red, 6f);
            }
            
            // Create the cube chunk with position, shapes, and colours (no texture)
            try
            {
                _cube = new Chunk(new Vector3(2f, 0f, 0f), ShapeType.Cube);
                DebugText.Log("Cube created successfully.", Color4.LightGreen, 6f);
            }
            catch (Exception e)
            {
                DebugText.Log($"Failed to create cube: {e.Message}", Color4.Red, 6f);
            }
            
            // --- Camera & input ---
            // Create the camera with the specified width, height, and initial position
            _camera = new Camera(_width, _height, _initialCameraPosition);
            
            // Set the initial cursor state to grabbed and hide the cursor
            CursorState = CursorState.Grabbed;
            _isCursorOn = false;
        }

        /// <summary>
        /// Called once when the window is closed.
        /// </summary>
        protected override void OnUnload()
        {
            base.OnUnload();
            
            // Delete the OpenGL resources to free up GPU memory
            _pyramid?.Delete();
            _cube?.Delete();
            _shaderProgram?.Delete();
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
            
            if (_shaderProgram != null)
            {
                // Bind the shader first: uniform uploads apply to the currently bound program
                _shaderProgram.Bind();
 
                // View and projection are shared by every object, so set them once per frame
                if (_camera != null)
                {
                    _shaderProgram.SetMatrix4("view", _camera.GetViewMatrix());
                    _shaderProgram.SetMatrix4("projection", _camera.GetProjectionMatrix());
                }
                
                // Each chunk sets its own model matrix inside Render
                _pyramid?.Render(_shaderProgram);
                _cube?.Render(_shaderProgram);
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
            
            // Close the program when the Escape key is pressed (OnUnload then frees all GPU resources)
            if (kb.IsKeyPressed(Keys.Escape))
            {
                Close();
                return;
            }

            // Toggle cursor visibility and lock state when the Tab key is pressed
            if (kb.IsKeyPressed(Keys.Tab))
            {
                _isCursorOn = !_isCursorOn;
                CursorState = _isCursorOn ? CursorState.Normal : CursorState.Grabbed;

                if (!_isCursorOn) // just switched back to Grabbed
                    _camera?.ResetMouseState();
            }
            
            // Update the camera based on keyboard and mouse input, capturing the mouse when the cursor is not visible
            _camera?.Update(kb, ms, args, captureMouse: !_isCursorOn);

            // Toggle backface culling when the C key is pressed
            if (kb.IsKeyPressed(Keys.C))
            {
                _useBackfaceCulling = !_useBackfaceCulling;
                if (_useBackfaceCulling)
                    GL.Enable(EnableCap.CullFace);
                else
                    GL.Disable(EnableCap.CullFace);
            }
            
            // Debug readouts (keyed, so they update in place instead of stacking)
            DebugText.Log($"FPS: {1.0 / deltaTime:0}", Color4.White, 0.5f, key: 1);
            DebugText.Log($"Press C for Backface Culling: {(_useBackfaceCulling ? "ON" : "OFF")}", Color4.Yellow, 0.5f, key: 2);
            DebugText.Log($"Press Tab to Toggle Cursor", Color4.Cyan, 0.5f, key: 3);
            DebugText.Log($"Press Esc to Quit", Color4.Magenta, 0.5f, key: 4);
            
            // Update the elapsed time
            _elapsedTime += deltaTime;

            // Rotate the pyramid around its Y-axis based on the elapsed time and rotation speed
            if (_pyramid != null)
                _pyramid.Rotation = Quaternion.FromAxisAngle(_rotDir, MathHelper.DegreesToRadians(_elapsedTime * _rotationSpeed));
            
            // Apply a pulsing scale effect to the cube based on the elapsed time, scale speed, and amplitude
            if (_cube != null)
                _cube.Scale = new Vector3(_scaleBase + MathF.Sin(_elapsedTime * _scaleSpeed) * _amplitudeAmt);
        }
        #endregion
    }
}