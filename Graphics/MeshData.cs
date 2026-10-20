using _3DDemoA2.Debugging;
using OpenTK.Mathematics;

namespace _3DDemoA2.Graphics
{
    /// <summary>
    /// CPU-side mesh data, ready to be uploaded to the GPU by a Mesh.
    /// One entry per unique vertex in each attribute list (positions, UVs and normals line up by index),
    /// plus an index list where every three indices form one triangle.
    /// Filled by the ObjLoader, but anything that produces these lists can create a Mesh.
    /// 
    /// Adapted from the Vertex struct and Mesh data members (Position, Normal, TexCoords + indices)
    /// in de Vries (n.d.), "Mesh", LearnOpenGL, stored here as separate lists to match this project's one-VBO-per-attribute layout.
    /// </summary>
    internal class MeshData
    {
        /// <summary>
        /// Vertex positions in the model's local space.
        /// </summary>
        public List<Vector3> Positions { get; } = new();
        
        /// <summary>
        /// Texture coordinates for each vertex, used for mapping textures onto the mesh (one per vertex).
        /// </summary>
        public List<Vector2> UVs { get; } = new();
        
        /// <summary>
        /// Normals for each vertex, used for lighting calculations (one per vertex).
        /// </summary>
        public List<Vector3> Normals { get; } = new();
        
        /// <summary>
        /// Indices defining the order in which vertices are connected to form triangles (three indices per triangle).
        /// </summary>
        public List<uint> Indices { get; } = new();
        
        /// <summary>
        /// The number of unique vertices in the mesh.
        /// </summary>
        public int VertexCount => Positions.Count;
        
        /// <summary>
        /// The number of indices in the mesh, which is typically three per triangle.
        /// </summary>
        public int IndexCount => Indices.Count;

        /// <summary>
        /// Moves every vertex by the same offset, effectively translating the entire mesh in 3D space.
        /// </summary>
        /// <param name="offset">The amount to move every vertex by.</param>
        public void Translate(Vector3 offset)
        {
            for(int i = 0; i < Positions.Count; i++)
                Positions[i] += offset;
        }

        /// <summary>
        /// Validates the mesh data to ensure it is well-formed and ready for rendering. Checks include:
        /// - Non-empty vertex and index lists.
        /// - Matching counts of positions, UVs, and normals.
        /// - Index count being a multiple of 3 (for triangles).
        /// - All indices being within the valid range of vertex indices.
        /// </summary>
        /// <param name="name">A name for the mesh</param>
        /// <returns>True if the mesh is valid; otherwise, false.</returns>
        public bool Validate(string name)
        {
            if (VertexCount == 0 || IndexCount == 0)
            {
                DebugText.Log($"{name}: mesh is empty", Color4.Red, 8f);
                return false;
            }
            
            if (UVs.Count != VertexCount || Normals.Count != VertexCount)
            {
                DebugText.Log($"{name}: attribute count mismatch " +
                              $"(pos {VertexCount}, uv {UVs.Count}, normal {Normals.Count})", Color4.Red, 8f);
                return false;
            }
            
            if (IndexCount % 3 != 0)
            {
                DebugText.Log($"{name}: index count {IndexCount} is not a multiple of 3", Color4.Red, 8f);
                return false;
            }

            foreach (uint index in Indices)
            {
                if (index >= VertexCount)
                {
                    DebugText.Log($"{name}: index {index} is out of range (vertex count {VertexCount})", Color4.Red, 8f);
                    return false;
                }
            }
            
            return true;
        }

    }
}