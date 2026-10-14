using _3DDemoA2.Debugging;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace _3DDemoA2.Graphics
{
    /// <summary>
    /// Represents a shader program in OpenGL,
    /// which is used to manage and link vertex and fragment shaders for rendering.
    /// </summary>
    internal class ShaderProgram
    {
        /// <summary>
        /// Gets the ID of the shader program.
        /// </summary>
        public int ID { get; private set; }
        
        /// <summary>
        /// A dictionary to cache uniform locations for efficient access.
        /// </summary>
        private readonly Dictionary<string, int> _uniformLocations = new();
        
        /// <summary>
        /// Constructor that initialises a new instance of the ShaderProgram class.
        /// </summary>
        /// <param name="vertexShaderFilepath">The file path to the vertex shader.</param>
        /// <param name="fragmentShaderFilePath">The file path to the fragment shader.</param>
        public ShaderProgram(string vertexShaderFilepath, string fragmentShaderFilePath)
        {
            // Create the shader program
            ID = GL.CreateProgram();
            
            // Create vertex shader
            var vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, LoadShaderSource(vertexShaderFilepath));
            GL.CompileShader(vertexShader);
            GL.GetShader(vertexShader, ShaderParameter.CompileStatus, out int vStatus);
            if (vStatus != (int)All.True)
            {
                Console.WriteLine($"Vertex shader compile error: {GL.GetShaderInfoLog(vertexShader)}");
                DebugText.Log($"'{vertexShaderFilepath}' failed to compile (see console)", Color4.Red, 6f);
            }
            
            // Create fragment shader
            var fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, LoadShaderSource(fragmentShaderFilePath));
            GL.CompileShader(fragmentShader);
            GL.GetShader(fragmentShader, ShaderParameter.CompileStatus, out int fStatus);
            if (fStatus != (int)All.True)
            {
                Console.WriteLine($"Fragment shader compile error: {GL.GetShaderInfoLog(fragmentShader)}");
                DebugText.Log($"'{fragmentShaderFilePath}' failed to compile (see console)", Color4.Red, 6f);
            }
            
            // Attach the shaders to the shader program
            GL.AttachShader(ID, vertexShader);
            GL.AttachShader(ID, fragmentShader);
            
            // Link the shader program to finalize it
            GL.LinkProgram(ID);
            GL.GetProgram(ID, GetProgramParameterName.LinkStatus, out int lStatus);
            if (lStatus != (int)All.True)
            {
                Console.WriteLine($"Shader program link error: {GL.GetProgramInfoLog(ID)}");
                DebugText.Log($"Shader program failed to link (see console)", Color4.Red, 6f);
            }
            
            // Clean up by detaching and deleting the individual shaders after linking
            GL.DetachShader(ID, vertexShader);
            GL.DetachShader(ID, fragmentShader);
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);
        }
        
        #region Public API
        /// <summary>
        /// Sets a 4x4 matrix uniform variable in the shader program.
        /// </summary>
        /// <param name="name">The name of the uniform variable in the shader.</param>
        /// <param name="matrix">The 4x4 matrix to set.</param>
        public void SetMatrix4(string name, Matrix4 matrix)
        {
            // Cache the uniform location to avoid repeated lookups
            if (!_uniformLocations.TryGetValue(name, out int location))
            {
                location = GL.GetUniformLocation(ID, name);
                _uniformLocations[name] = location;
                
                
                if (location == -1)
                    DebugText.Log($"Uniform '{name}' not found in shader {ID}", Color4.Orange, 6f);
            }
            GL.UniformMatrix4(location, true, ref matrix);
        }
        
        /// <summary>
        /// Sets an integer uniform variable in the shader program.
        /// </summary>
        /// <param name="name">The name of the uniform variable in the shader.</param>
        /// <param name="value">The integer value to set.</param>
        public void SetInt(string name, int value)
        {
            if (!_uniformLocations.TryGetValue(name, out int location))
            {
                location = GL.GetUniformLocation(ID, name);
                _uniformLocations[name] = location;
                
                if (location == -1)
                    DebugText.Log($"Uniform '{name}' not found in shader {ID}", Color4.Orange, 6f);
            }
            GL.Uniform1(location, value);
        }
        
        /// <summary>
        /// Binds the shader program for use in rendering.
        /// </summary>
        public void Bind() => GL.UseProgram(ID);
        /// <summary>
        /// Unbinds the shader program, reverting to the default OpenGL state.
        /// </summary>
        public void Unbind() => GL.UseProgram(0);
        /// <summary>
        /// Deletes the shader program from the GPU, freeing up resources.
        /// </summary>
        public void Delete() => GL.DeleteProgram(ID);
        #endregion
        
        #region Helper Methods
        /// <summary>
        /// Loads the shader source code from a file located in the "Shaders" directory.
        /// </summary>
        /// <param name="filePath">The relative path to the shader file.</param>
        /// <returns>The shader source code as a string.</returns>
        private string LoadShaderSource(string filePath)
        {
            // Load shader source code from a file located in the "Shaders" directory
            var shaderSource = string.Empty;

            // Try to read the shader source code from the specified file
            try
            {
                // Read the shader source code from the specified file
                using var resource = new StreamReader("../../../Shaders/" + filePath);
                
                // Read the entire contents of the shader file into a string
                shaderSource = resource.ReadToEnd();
            }
            // Catch any exceptions that occur during file reading and log an error message
            catch (Exception e)
            {
                Console.WriteLine($"Error loading shader source from file '{filePath}': {e.Message}");
            }
            
            return shaderSource;
        }
        #endregion
    }
}