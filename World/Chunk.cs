using _3DDemoA2.Debugging;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using _3DDemoA2.Graphics;

namespace _3DDemoA2.World
{
    /// <summary>
    /// A section of the world that collects shapes, combines them into a single mesh and renders it.
    /// Currently holds a single pyramid.
    /// </summary>
    internal class Chunk
    {
        #region Public Fields
        // Position in world space.
        public Vector3 Position { get; set; }

        // Orientation of the chunk. Identity = no rotation.
        public Quaternion Rotation { get; set; } = Quaternion.Identity;

        // Scale on each axis. (1, 1, 1) is the default size.
        public Vector3 Scale { get; set; } = Vector3.One;
        #endregion

        #region Private Fields
        // Shapes contained in this chunk.
        private readonly List<Shape> _shapes = new();

        // CPU-side mesh data, built from the shapes' faces.
        private readonly List<Vector3> _vertices = new();
        private readonly List<Vector2> _uvs = new();
        private readonly List<Vector3> _colors = new();
        private readonly List<uint> _indices = new();

        // GPU-side objects.
        private VAO? _vao;
        private VBO? _vertexVBO;
        private VBO? _uvVBO;
        private VBO? _colorVBO;
        private IBO? _ibo;
        private Texture? _texture;
        
        // What this chunk is made of.
        private readonly ShapeType _shapeType;
        private readonly string _textureFile;
        
        // No texture file = use vertex colours instead
        private bool UsesVertexColor => string.IsNullOrEmpty(_textureFile);
        #endregion

        /// <summary>
        /// Constructor that initialises a new chunk at the given position, generates its shapes, builds its mesh and uploads it to the GPU.
        /// </summary>
        /// <param name="position">The position of the chunk in the world.</param>
        /// <param name="shapeType">The shape to build.</param>
        /// <param name="textureFile">The texture file (in the Textures folder) to draw it with.</param>
        public Chunk(Vector3 position, ShapeType shapeType, string textureFile = "")
        {
            Position = position;
            _shapeType = shapeType;
            _textureFile = textureFile;
 
            GenerateShapes();
            BuildMesh();
            UploadMesh();
        }
        
        #region Public API
        /// <summary>
        /// Renders the chunk using the given shader program.
        /// </summary>
        /// <param name="shaderProgram"></param>
        public void Render(ShaderProgram shaderProgram)
        {
            // Set the model matrix uniform for this chunk's position, rotation and scale.
            shaderProgram.SetMatrix4("model", GetModelMatrix());
 
            // Bind the VAO and texture, and draw the chunk's mesh.
            _vao?.Bind();
            _texture?.Bind();
            shaderProgram.SetInt("UseVertexColor", UsesVertexColor ? 1 : 0);
            GL.DrawElements(PrimitiveType.Triangles, _indices.Count, DrawElementsType.UnsignedInt, 0);
        }

        /// <summary>
        /// Frees the chunk's GPU resources.
        /// </summary>
        public void Delete()
        {
            _vao?.Delete();
            _vertexVBO?.Delete();
            _uvVBO?.Delete();
            _colorVBO?.Delete();
            _ibo?.Delete();
            _texture?.Delete();
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// Decides what shapes live in this chunk.
        /// Builds the shapes and adds them to the chunk's shape list.
        /// </summary>
        private void GenerateShapes()
        {
            // For now, just a single shape at the chunk's origin.
            _shapes.Add(new Shape(_shapeType, Vector3.Zero));

            // Later, e.g. a grid of cubes:
            // for (int x = 0; x < SIZE; x++)
            //     for (int z = 0; z < SIZE; z++)
            //         _shapes.Add(new Shape(ShapeType.Cube, Position + new Vector3(x, 0, z)));
        }

        /// <summary>
        /// Combines every face of every shape into the chunk's mesh lists.
        /// </summary>
        private void BuildMesh()
        {
            foreach (var shape in _shapes)
            {
                foreach (var face in shape.AvailableFaces)
                {
                    // Neighbour face culling goes here later
                    // (skip the face if the neighbouring shape fully covers it).
                    if (shape.TryGetFace(face, out var faceData))
                        AddFace(faceData);
                }
            }
        }

        /// <summary>
        /// Appends a face's vertices and UVs, offsetting its local indices by the current vertex count.
        /// Works for quads, triangles or any other face shape.
        /// </summary>
        private void AddFace(FaceData face)
        {
            uint baseIndex = (uint)_vertices.Count;

            _vertices.AddRange(face.Vertices);
            _uvs.AddRange(face.UVs);
            
            // Colour each vertex from its position: -0.5..0.5 becomes 0..1 RGB
            foreach (var vertex in face.Vertices)
                _colors.Add(vertex + new Vector3(0.5f));

            // Append the face's indices, offset by the base index of this face's vertices.
            foreach (var index in face.Indices)
                _indices.Add(baseIndex + index);
        }

        /// <summary>
        /// Sends the mesh to the GPU and loads the texture.
        /// </summary>
        private void UploadMesh()
        {
            if (_vertices.Count != _uvs.Count || _vertices.Count != _colors.Count)
                DebugText.Log($"{_shapeType}: attribute count mismatch " +
                              $"(pos {_vertices.Count}, uv {_uvs.Count}, col {_colors.Count})", Color4.Red, 6f);
            
            _vao = new VAO();
 
            // location 0 = position (vec3)
            _vertexVBO = new VBO(_vertices);
            _vao.LinkToVao(0, 3, _vertexVBO);

            // location 1 = UV (vec2)
            _uvVBO = new VBO(_uvs);
            _vao.LinkToVao(1, 2, _uvVBO);

            // location 2 = colour (vec3)
            _colorVBO = new VBO(_colors);
            _vao.LinkToVao(2, 3, _colorVBO);
 
            // Create the IBO while the VAO is bound so the VAO remembers it.
            _vao.Bind();
            _ibo = new IBO(_indices);
            _vao.Unbind();
 
            if (!UsesVertexColor)
                _texture = new Texture(_textureFile);
            
            DebugText.Log($"{_shapeType} uploaded: {_vertices.Count} vertices, {_indices.Count} indices",
                Color4.LightGreen, 6f);
            DebugText.CheckGLError($"{_shapeType} upload");
        }
        
        /// <summary>
        /// Returns the model matrix for this chunk, combining its scale, rotation and translation.
        /// </summary>
        /// <returns></returns>
        private Matrix4 GetModelMatrix()
            => Matrix4.CreateScale(Scale)
               * Matrix4.CreateFromQuaternion(Rotation)
               * Matrix4.CreateTranslation(Position);
        #endregion
    }
}