using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace _3DDemoA2
{
    /// <summary>
    /// Represents a camera in a 3D environment,
    /// allowing for movement and rotation based on user input.
    /// </summary>
    internal class Camera
    {
        #region Constants
        private const float SPEED = 8f; // Movement speed of the camera
        private const float SENSITIVITY = 0.1f; // Sensitivity of the mouse movement
        #endregion
        
        #region Public fields
        // Field of view of the camera
        public float FOV { get; set; } = 45f;
        
        // Direction coordinates of the camera
        public Vector3 Up { get; private set; } = Vector3.UnitY;
        public Vector3 Forward { get; private set; } = -Vector3.UnitZ;
        public Vector3 Right { get; private set; } = Vector3.UnitX;
        
        /// <summary>
        /// Gets or sets the position of the camera in 3D space.
        /// </summary>
        public Vector3 Position { get => _position; set => _position = value; }
        #endregion
        
        #region Private fields
        // Position of the camera
        private Vector3 _position;
        
        // Screen dimensions for aspect ratio calculations
        private float _screenWidth = 1280f;
        private float _screenHeight = 720f;
        
        // View Rotations
        private float _pitch;
        private float _yaw = -90f;

        // Mouse state: the last cursor position (for measuring movement) and whether there is one yet
        private Vector2 _lastMousePosition;
        private bool _firstMove = true;
        #endregion

        /// <summary>
        /// Constructor that initialises a new instance of the Camera class with the specified width, height, and position.
        /// </summary>
        /// <param name="width">The width of the screen.</param>
        /// <param name="height">The height of the screen.</param>
        /// <param name="position">The initial position of the camera.</param>
        public Camera(float width, float height, Vector3 position)
        {
            _screenWidth = width;
            _screenHeight = height;
            _position = position;
        }
        
        #region Public API
        /// <summary>
        /// Handles the input from the keyboard and mouse to control the camera's movement and rotation.
        /// </summary>
        /// <param name="kb">The current state of the keyboard.</param>
        /// <param name="ms">The current state of the mouse.</param>
        /// <param name="e">The frame event arguments.</param>
        /// <param name="captureMouse">Whether the mouse is captured for camera rotation.</param>
        public void InputController(KeyboardState kb, MouseState ms, FrameEventArgs e, bool captureMouse)
        {
            // Handle keyboard input for horizontal movement
            if (kb.IsKeyDown(Keys.W))
                _position += Forward * SPEED * (float)e.Time;
            if (kb.IsKeyDown(Keys.S))
                _position -= Forward * SPEED * (float)e.Time;
            
            // Handle keyboard input for lateral movement
            if (kb.IsKeyDown(Keys.A))
                _position -= Right * SPEED * (float)e.Time;
            if (kb.IsKeyDown(Keys.D))
                _position += Right * SPEED * (float)e.Time;
            
            // Handle keyboard input for vertical movement
            if (kb.IsKeyDown(Keys.Q))
                _position.Y += SPEED * (float)e.Time;
            if (kb.IsKeyDown(Keys.E))
                _position.Y -= SPEED * (float)e.Time;
            
            // Handle mouse input for camera rotation
            if (captureMouse)
            {
                if (_firstMove)
                {
                    _lastMousePosition = new Vector2(ms.X, ms.Y);
                    _firstMove = false;
                }
                else
                {
                    var deltaX = ms.X - _lastMousePosition.X;
                    var deltaY = ms.Y - _lastMousePosition.Y;
                    _lastMousePosition = new Vector2(ms.X, ms.Y);

                    _yaw += deltaX * SENSITIVITY;
                    _pitch -= deltaY * SENSITIVITY;
                }
            }
            
            UpdateVectors();
        }
        
        /// <summary>
        /// Updates the camera's position and orientation based on the input from the keyboard and mouse.
        /// </summary>
        /// <param name="kb">The current state of the keyboard.</param>
        /// <param name="ms">The current state of the mouse.</param>
        /// <param name="e">The frame event arguments.</param>
        /// <param name="captureMouse">Whether the mouse is captured for camera rotation.</param>
        public void Update(KeyboardState kb, MouseState ms, FrameEventArgs e, bool captureMouse)
            => InputController(kb, ms, e, captureMouse);

        /// <summary>
        /// Returns the view matrix of the camera.
        /// </summary>
        /// <returns>The view matrix.</returns>
        public Matrix4 GetViewMatrix()
            => Matrix4.LookAt(_position, _position + Forward, Up);

        /// <summary>
        /// Returns the projection matrix of the camera.
        /// </summary>
        /// <returns>The projection matrix.</returns>
        public Matrix4 GetProjectionMatrix()
            => Matrix4.CreatePerspectiveFieldOfView(
                MathHelper.DegreesToRadians(FOV),
                _screenWidth / _screenHeight, 
                0.1f, 
                100f);
        
        /// <summary>
        /// Updates the screen size used for the projection's aspect ratio.
        /// </summary>
        public void SetScreenSize(float width, float height)
        {
            // Ignore a zero-sized window (e.g. minimised) to avoid dividing by zero
            if (width <= 0 || height <= 0) return;

            _screenWidth = width;
            _screenHeight = height;
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// Updates the camera's forward, right, and up vectors based on the current pitch and yaw angles.
        /// </summary>
        private void UpdateVectors()
        {
            // Clamp the pitch angle to prevent the camera from flipping upside down
            _pitch = MathHelper.Clamp(_pitch, -89f, 89f);
                
            // Update the camera's forward vector based on the current pitch and yaw angles
            float pitch = MathHelper.DegreesToRadians(_pitch);
            float yaw = MathHelper.DegreesToRadians(_yaw);

            Forward = Vector3.Normalize(new Vector3(
                MathF.Cos(pitch) * MathF.Cos(yaw),
                MathF.Sin(pitch),
                MathF.Cos(pitch) * MathF.Sin(yaw)));
            
            // Normalize the forward vector to ensure it has a length of 1
            Forward = Vector3.Normalize(Forward);
            
            // Update the camera's right vector based on the forward vector and the world up vector
            Right = Vector3.Normalize(Vector3.Cross(Forward, Vector3.UnitY));
            
            // Update the camera's up vector based on the right and forward vectors
            Up = Vector3.Normalize(Vector3.Cross(Right, Forward));
        }
        #endregion
    }
}

