using OpenTK.Mathematics;

namespace _3DDemoA2.World
{
    #region Enums
    /// <summary>
    /// The faces a shape can have. A shape simply omits faces it doesn't have (e.g. a pyramid has no TOP).
    /// </summary>
    public enum Faces
    {
        FRONT,
        BACK,
        LEFT,
        RIGHT,
        TOP,
        BOTTOM
    }

    /// <summary>
    /// Every shape type that ShapeData knows how to build.
    /// </summary>
    public enum ShapeType
    {
        Cube,
        Pyramid
    }
    #endregion
    
    #region Structs
    /// <summary>
    /// Vertices, UVs and local indices for a single face.
    /// Used both for the local templates in ShapeData and for the world-space copies in Shape.
    /// </summary>
    public struct FaceData
    {
        public List<Vector3> Vertices;
        public List<Vector2> UVs;
        public List<uint> Indices;
    }
    #endregion

    /// <summary>
    /// Master definition of every shape's geometry, in local space centred on the origin (a 1x1x1 cell).
    /// Winding convention: vertices go clockwise when viewed from outside, starting from the top.
    /// </summary>
    public static class ShapeData
    {
        #region Static Fields
        // Static fields initialise top to bottom, so these helpers must stay above the shapes.

        private static readonly List<uint> QuadIndices = [0, 1, 2, 2, 3, 0];
        private static readonly List<uint> TriangleIndices = [0, 1, 2];

        private static readonly List<Vector2> QuadUVs = [new(0f, 1f), new(1f, 1f), new(1f, 0f), new(0f, 0f)];
        private static readonly List<Vector2> TriangleUVs = [new(0.5f, 1f), new(1f, 0f), new(0f, 0f)];

        private static readonly Vector3 Apex = new(0f, 0.5f, 0f);
        #endregion

        #region Vertex Builders
        /// <summary>
        /// Builds a 4-vertex face (top-left, top-right, bottom-right, bottom-left).
        /// </summary>
        private static FaceData Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d) => new()
        {
            Vertices = [a, b, c, d],
            UVs = QuadUVs,
            Indices = QuadIndices
        };

        /// <summary>
        /// Builds a 3-vertex face (top, bottom-right, bottom-left).
        /// </summary>
        private static FaceData Triangle(Vector3 a, Vector3 b, Vector3 c) => new()
        {
            Vertices = [a, b, c],
            UVs = TriangleUVs,
            Indices = TriangleIndices
        };
        #endregion

        #region Shape Definitions
        // ---------------- Cube ----------------
        // A cube has 6 faces, each a quad. The cube is centred on the origin, with corners at +/-0.5 in each axis.
        
        private static readonly Dictionary<Faces, FaceData> Cube = new()
        {
            { Faces.FRONT, Quad(new(-0.5f,  0.5f,  0.5f), new( 0.5f,  0.5f,  0.5f), new( 0.5f, -0.5f,  0.5f), new(-0.5f, -0.5f,  0.5f)) },
            { Faces.RIGHT, Quad(new( 0.5f,  0.5f,  0.5f), new( 0.5f,  0.5f, -0.5f), new( 0.5f, -0.5f, -0.5f), new( 0.5f, -0.5f,  0.5f)) },
            { Faces.BACK, Quad(new( 0.5f,  0.5f, -0.5f), new(-0.5f,  0.5f, -0.5f), new(-0.5f, -0.5f, -0.5f), new( 0.5f, -0.5f, -0.5f)) },
            { Faces.LEFT, Quad(new(-0.5f,  0.5f, -0.5f), new(-0.5f,  0.5f,  0.5f), new(-0.5f, -0.5f,  0.5f), new(-0.5f, -0.5f, -0.5f)) },
            { Faces.TOP, Quad(new(-0.5f,  0.5f, -0.5f), new( 0.5f,  0.5f, -0.5f), new( 0.5f,  0.5f,  0.5f), new(-0.5f,  0.5f,  0.5f)) },
            { Faces.BOTTOM, Quad(new(-0.5f, -0.5f,  0.5f), new( 0.5f, -0.5f,  0.5f), new( 0.5f, -0.5f, -0.5f), new(-0.5f, -0.5f, -0.5f)) }
        };

        // ---------------- Pyramid ----------------
        // Square base at y = -0.5, four triangular sides meeting at the apex. No TOP face.

        private static readonly Dictionary<Faces, FaceData> Pyramid = new()
        {
            { Faces.FRONT, Triangle(Apex, new( 0.5f, -0.5f,  0.5f), new(-0.5f, -0.5f,  0.5f)) },
            { Faces.RIGHT, Triangle(Apex, new( 0.5f, -0.5f, -0.5f), new( 0.5f, -0.5f,  0.5f)) },
            { Faces.BACK, Triangle(Apex, new(-0.5f, -0.5f, -0.5f), new( 0.5f, -0.5f, -0.5f)) },
            { Faces.LEFT, Triangle(Apex, new(-0.5f, -0.5f,  0.5f), new(-0.5f, -0.5f, -0.5f)) },
            { Faces.BOTTOM, Quad(new(-0.5f, -0.5f, 0.5f), new(0.5f, -0.5f, 0.5f), new(0.5f, -0.5f, -0.5f), new(-0.5f, -0.5f, -0.5f)) }
        };
        #endregion

        #region Public API
        /// <summary>
        /// Gets the local-space face templates for a shape type.
        /// </summary>
        public static IReadOnlyDictionary<Faces, FaceData> Get(ShapeType type) => type switch
        {
            ShapeType.Cube => Cube,
            ShapeType.Pyramid => Pyramid,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "No geometry defined for this shape type.")
        };
        #endregion
    }
}