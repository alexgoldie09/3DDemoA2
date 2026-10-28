using _3DDemoA2.Debugging;
using _3DDemoA2.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace _3DDemoA2.Scene
{
    /// <summary>
    /// The sky: a cube around the camera, textured from the inside with a cube map.
    /// It has its own shader and is drawn after the scene, at the far plane, so it always sits behind every model.
    ///
    /// Not an IRenderable: the scene is everything drawn with the model shader, while the skybox needs its own
    /// shader and depth test (the same reason DebugText is kept separate).
    /// </summary>
    internal class Skybox
    {
        #region Constants
        /// <summary>
        /// The texture unit the cube map is bound to (units 0 to 3 are the material maps, see Material.cs).
        /// The model shader samples it too, for reflections.
        /// </summary>
        public const int CUBEMAPUNIT = 4;

        // Shader location of the cube's corner positions (matches Skybox.vert)
        private const int POSITIONLOCATION = 0;

        // The cube's 8 corners, centred on the origin. Its size doesn't matter:
        // the shader only uses the direction to each corner, and places the cube at the far plane.
        private static readonly List<Vector3> Corners = new()
        {
            new(-1f, -1f, -1f), // 0
            new( 1f, -1f, -1f), // 1
            new( 1f,  1f, -1f), // 2
            new(-1f,  1f, -1f), // 3
            new(-1f, -1f,  1f), // 4
            new( 1f, -1f,  1f), // 5
            new( 1f,  1f,  1f), // 6
            new(-1f,  1f,  1f)  // 7
        };

        // Two triangles per face, wound counter-clockwise when seen from INSIDE the cube (the camera is inside),
        // so the inside faces are the front faces and backface culling keeps them
        private static readonly List<uint> Indices = new()
        {
            3, 0, 1,  1, 2, 3, // back (-Z)
            4, 0, 3,  3, 7, 4, // left (-X)
            1, 5, 6,  6, 2, 1, // right (+X)
            4, 7, 6,  6, 5, 4, // front (+Z)
            3, 2, 6,  6, 7, 3, // top (+Y)
            0, 4, 1,  1, 4, 5  // bottom (-Y)
        };
        #endregion

        #region Private Fields
        // GPU resources this class owns, deleted in Delete()
        private readonly ShaderProgram _shader;
        private readonly CubeMap _cubeMap;
        private readonly VAO _vao;
        private readonly VBO _positionVbo;
        private readonly IBO _ibo;
        #endregion

        /// <summary>
        /// Constructor that loads the cube map, uploads the cube and creates the skybox shader.
        /// </summary>
        /// <param name="faceFiles">
        /// Six files in the Textures folder, in OpenGL's face order:
        /// right (+X), left (-X), top (+Y), bottom (-Y), front (+Z), back (-Z).
        /// </param>
        public Skybox(string[] faceFiles)
        {
            // --- Cube map ---
            _cubeMap = new CubeMap(faceFiles, CUBEMAPUNIT);

            // Bound once here and left bound: nothing else uses this unit, so it stays in place for the whole run.
            // That also means the model shader's reflections have it from the very first frame.
            _cubeMap.Bind();

            // --- Cube ---
            // Positions only (no UVs or normals: the cube map is looked up by direction), so the cube is built
            // from the VAO, VBO and IBO classes directly rather than as a Mesh
            _vao = new VAO();
            _positionVbo = new VBO(Corners);
            _vao.LinkToVao(POSITIONLOCATION, 3, _positionVbo);

            // Create the IBO while the VAO is bound so the VAO remembers it
            _vao.Bind();
            _ibo = new IBO(Indices);
            _vao.Unbind();

            // --- Shader ---
            // Its own shader program; the sampler unit never changes, so it is set once here
            _shader = new ShaderProgram("Skybox.vert", "Skybox.frag");
            _shader.Bind();
            _shader.SetInt("SkyboxMap", CUBEMAPUNIT);

            DebugText.CheckGLError("Skybox setup");
        }

        #region Public API
        /// <summary>
        /// Uploads the camera's projection matrix (on load and whenever the window resizes).
        /// </summary>
        /// <param name="projection">The camera's projection matrix.</param>
        public void SetProjection(Matrix4 projection)
        {
            _shader.Bind();
            _shader.SetMatrix4("projection", projection);
        }

        /// <summary>
        /// Draws the sky. Call after the scene, so the pixels already covered by models are skipped by the depth test.
        /// </summary>
        /// <param name="view">The camera's view matrix (the shader ignores its translation).</param>
        public void Render(Matrix4 view)
        {
            // The shader puts the sky at a depth of exactly 1.0, which is also what the depth buffer is cleared to.
            // The default test (Less) would reject 1.0 < 1.0, so Less-or-equal is used while the sky is drawn.
            GL.DepthFunc(DepthFunction.Lequal);

            _shader.Bind();
            _shader.SetMatrix4("view", view);

            _vao.Bind();
            GL.DrawElements(PrimitiveType.Triangles, Indices.Count, DrawElementsType.UnsignedInt, 0);
            _vao.Unbind();

            // Back to the default for everything drawn next frame
            GL.DepthFunc(DepthFunction.Less);
        }

        /// <summary>
        /// Frees the skybox's GPU resources (shader, cube buffers and cube map).
        /// </summary>
        public void Delete()
        {
            _shader.Delete();
            _vao.Delete();
            _positionVbo.Delete();
            _ibo.Delete();
            _cubeMap.Delete();
        }
        #endregion
    }
}
