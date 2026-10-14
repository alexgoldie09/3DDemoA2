using OpenTK.Mathematics;

namespace _3DDemoA2.World
{
    /// <summary>
    /// A single placed shape in the world (cube, pyramid, ...).
    /// Copies its shape's templates from ShapeData and moves them to its position.
    /// </summary>
    internal class Shape
    {
        #region Public Fields
        /// <summary>
        /// Position of the shape in world space.
        /// </summary>
        public Vector3 Position { get; private set; }

        /// <summary>
        /// Which shape this is.
        /// </summary>
        public ShapeType Type { get; private set; }

        /// <summary>
        /// World-space face data, keyed by face.
        /// </summary>
        private readonly Dictionary<Faces, FaceData> _faces = new();

        /// <summary>
        /// The faces this shape actually has.
        /// </summary>
        public IEnumerable<Faces> AvailableFaces => _faces.Keys;
        #endregion

        /// <summary>
        /// Constructor that initialises a new shape at the given position,
        /// copying its face templates from ShapeData and moving them to world space.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="position"></param>
        public Shape(ShapeType type, Vector3 position)
        {
            Type = type;
            Position = position;

            // Copy the face templates from ShapeData and offset them to this shape's position.
            foreach (var (face, template) in ShapeData.Get(type))
            {
                _faces[face] = new FaceData
                {
                    Vertices = OffsetVertices(template.Vertices),
                    UVs = template.UVs,        // shared, never modified
                    Indices = template.Indices // shared, never modified
                };
            }
        }
        
        #region Public API
        /// <summary>
        /// Gets a face's world-space data. Returns false if this shape has no such face.
        /// </summary>
        public bool TryGetFace(Faces face, out FaceData data) => _faces.TryGetValue(face, out data);
        #endregion
        
        #region Helper Methods
        /// <summary>
        /// Returns a new list of vertices moved to this shape's position.
        /// (A new list, so the shared templates in ShapeData are never changed.)
        /// </summary>
        private List<Vector3> OffsetVertices(List<Vector3> localVertices)
        {
            var worldVertices = new List<Vector3>(localVertices.Count);

            foreach (var vertex in localVertices)
                worldVertices.Add(vertex + Position);

            return worldVertices;
        }
        #endregion
    }
}