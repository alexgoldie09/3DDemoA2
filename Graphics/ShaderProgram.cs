using _3DDemoA2.Debugging;
using _3DDemoA2.Utilities;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace _3DDemoA2.Graphics
{
    /// <summary>
    /// Represents a shader program in OpenGL,
    /// which is used to manage and link vertex and fragment shaders for rendering.
    /// </summary>
    internal class ShaderProgram : IGraphicsResources
    {
        /// <summary>
        /// Gets the ID of the shader program.
        /// </summary>
        public int ID { get; }
        
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
        // Uniform setters apply to the currently bound program, so call Bind() before using them.

        /// <summary>
        /// Sets a 4x4 matrix uniform variable in the shader program.
        /// Uploaded transposed (row-vector convention), matching the vec4 * matrix order used in the shaders.
        /// </summary>
        /// <param name="name">The name of the uniform variable in the shader.</param>
        /// <param name="matrix">The 4x4 matrix to set.</param>
        public void SetMatrix4(string name, Matrix4 matrix) => GL.UniformMatrix4(GetLocation(name), true, ref matrix);
        
        /// <summary>
        /// Sets a 3x3 matrix uniform variable in the shader program (e.g. the normal matrix).
        /// Uploaded transposed like SetMatrix4, so shaders use the same vec3 * matrix order.
        /// </summary>
        /// <param name="name">The name of the uniform variable in the shader.</param>
        /// <param name="matrix">The 3x3 matrix to set.</param>
        public void SetMatrix3(string name, Matrix3 matrix) => GL.UniformMatrix3(GetLocation(name), true, ref matrix);

        /// <summary>
        /// Sets a 3-component vector uniform variable in the shader program (e.g. a camera position or light direction).
        /// </summary>
        /// <param name="name">The name of the uniform variable in the shader.</param>
        /// <param name="value">The vector to set.</param>
        public void SetVector3(string name, Vector3 value) => GL.Uniform3(GetLocation(name), value);

        /// <summary>
        /// Sets a float uniform variable in the shader program (e.g. a reflectivity amount).
        /// </summary>
        /// <param name="name">The name of the uniform variable in the shader.</param>
        /// <param name="value">The float value to set.</param>
        public void SetFloat(string name, float value) => GL.Uniform1(GetLocation(name), value);

        /// <summary>
        /// Sets an integer uniform variable in the shader program.
        /// Also used for bools and for sampler uniforms (the texture unit number).
        /// </summary>
        /// <param name="name">The name of the uniform variable in the shader.</param>
        /// <param name="value">The integer value to set.</param>
        public void SetInt(string name, int value) => GL.Uniform1(GetLocation(name), value);
        #endregion
        
        #region IGraphicsResources Functions
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
        /// Returns a uniform's location, looking it up only the first time and caching it after that.
        /// A location of -1 means the uniform doesn't exist, or the GLSL compiler removed it because it is unused.
        /// OpenGL silently ignores writes to -1, so it is logged once here to make typos easy to spot.
        /// </summary>
        /// <param name="name">The name of the uniform variable in the shader.</param>
        /// <returns>The uniform's location, or -1 if not found.</returns>
        private int GetLocation(string name)
        {
            if (_uniformLocations.TryGetValue(name, out int location))
                return location;

            location = GL.GetUniformLocation(ID, name);
            _uniformLocations[name] = location;

            if (location == -1)
                DebugText.Log($"Uniform '{name}' not found in shader {ID}", Color4.Orange, 6f);

            return location;
        }

        /// <summary>
        /// Loads the shader source code from a file in the Shaders folder.
        /// </summary>
        /// <param name="fileName">The shader file, relative to the Shaders folder.</param>
        /// <returns>The shader source code, or an empty string if the file could not be read.</returns>
        private static string LoadShaderSource(string fileName)
        {
            string path = AssetPaths.Shader(fileName);

            try
            {
                return File.ReadAllText(path);
            }
            // An empty source will then fail to compile, which is logged by the constructor
            catch (Exception e)
            {
                Console.WriteLine($"Error loading shader source from '{path}': {e.Message}");
                DebugText.Log($"Shader '{fileName}' could not be read (see console)", Color4.Red, 6f);
                return string.Empty;
            }
        }
        #endregion
    }
}