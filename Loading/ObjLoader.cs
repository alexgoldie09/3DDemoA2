using _3DDemoA2.Debugging;
using _3DDemoA2.Graphics;
using _3DDemoA2.Utilities;
using OpenTK.Mathematics;

namespace _3DDemoA2.Loading
{
    /// <summary>
    /// Loads a wavefront .obj file from the Models folder into MeshData.
    ///
    /// How it works:
    ///  1. Read the file line by line, skipping blank lines and # comments.
    ///  2. Collect the raw attribute lists: "v" (positions), "vt" (UVs) and "vn" (normals).
    ///  3. Each "f" (face) line lists corners as "v/vt/vn" index triplets. OBJ indices are 1-based,
    ///     and each attribute has its own index, which OpenGL can't use: it needs ONE index per vertex.
    ///  4. So every unique (v, vt, vn) combination becomes one output vertex. A dictionary remembers
    ///     combinations already seen, so shared corners reuse the same vertex (indexed drawing).
    ///  5. Faces with more than 3 corners are split into a triangle fan as a fallback.
    ///
    /// Supported: v, vt, vn, f (v, v/vt, v//vn, v/vt/vn, and negative/relative indices), and o (named objects,
    /// used by LoadParts to keep each part of a multi-part model separate).
    /// Ignored: g, s, mtllib, usemtl and anything else (materials are bound in code instead).
    /// Winding is kept as exported (Blender exports counter-clockwise), matching GL.FrontFace(Ccw) in Game.
    /// </summary>
    internal static class ObjLoader
    {
        /// <summary>
        /// Maximum number of malformed-line warnings shown on screen per file, so a broken file can't flood the log.
        /// </summary>
        private const int MaxWarnings = 5;

        /// <summary>
        /// Loads an .obj file from the Models folder as one mesh.
        /// Problems are logged, and a missing file returns empty data.
        /// </summary>
        /// <param name="fileName">The .obj file.</param>
        /// <returns>The mesh data, ready to be passed to a Mesh.</returns>
        public static MeshData Load(string fileName) => Parse(fileName, splitByObject: false)[fileName];

        /// <summary>
        /// Loads an .obj file from the Models folder as separate parts, one per named object in the file.
        /// This is useful for multi-part models like vehicles, so that each part can be given its own transform.
        /// </summary>
        /// <param name="fileName">The .obj file.</param>
        /// <returns>Each part's mesh data, keyed by its object name.</returns>
        public static Dictionary<string, MeshData> LoadParts(string fileName) => Parse(fileName, splitByObject: true);

        /// <summary>
        /// Reads the file and builds the mesh data.
        /// </summary>
        /// <param name="fileName">The .obj file.</param>
        /// <param name="splitByObject">True to start a new MeshData at every "o" line; false to merge everything.</param>
        /// <returns>The mesh data keyed by object name (just the file name when not splitting).</returns>
        private static Dictionary<string, MeshData> Parse(string fileName, bool splitByObject)
        {
            // Faces go into current. It starts as a default part named after the file, which collects
            // everything when not splitting (or any faces that appear before the first "o" line).
            var meshes = new Dictionary<string, MeshData>();
            var current = new MeshData();
            meshes[fileName] = current;

            string path = AssetPaths.Model(fileName);

            string[] lines;
            try
            {
                lines = File.ReadAllLines(path);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error loading model '{path}': {e.Message}");
                DebugText.Log($"Model '{fileName}' could not be read (see console)", Color4.Red, 8f);
                return meshes;
            }

            // Raw attribute lists, exactly as listed in the file
            var positions = new List<Vector3>();
            var uvs = new List<Vector2>();
            var normals = new List<Vector3>();

            // (v, vt, vn) combination. Used to de-duplicate vertices and build the index buffer.
            var vertexLookup = new Dictionary<(int Position, int UV, int Normal), uint>();
            var corners = new List<(int Position, int UV, int Normal)>(4);

            // Stats for the summary and warnings
            int triangleCount = 0;
            int polygonCount = 0;
            int warningCount = 0;
            bool missingUVs = false;
            bool missingNormals = false;

            for (int lineNumber = 0; lineNumber < lines.Length; lineNumber++)
            {
                string line = lines[lineNumber].Trim();
                if (line.Length == 0 || line[0] == '#')
                    continue;

                // Split on any whitespace, dropping empty entries (handles double spaces and tabs)
                string[] parts = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

                try
                {
                    switch (parts[0])
                    {
                        case "v":
                            positions.Add(ParseVector3(parts));
                            break;

                        case "vt":
                            uvs.Add(new Vector2(ParseFloat(parts[1]), ParseFloat(parts[2])));
                            break;

                        case "vn":
                            normals.Add(ParseVector3(parts));
                            break;

                        case "o":
                            // A new named object. When splitting, its faces go into their own MeshData,
                            // with its own de-duplication so its indices start from 0.
                            // The raw v/vt/vn lists are NOT reset: OBJ indices count from the top of the whole file.
                            if (splitByObject)
                            {
                                current = new MeshData();
                                meshes[line.Substring(1).Trim()] = current;
                                vertexLookup = new Dictionary<(int Position, int UV, int Normal), uint>();
                            }
                            break;

                        case "f":
                            // Parse every corner first, so a bad corner rejects the whole face
                            // instead of leaving half a face in the mesh
                            corners.Clear();
                            for (int i = 1; i < parts.Length; i++)
                                corners.Add(ParseFaceVertex(parts[i], positions.Count, uvs.Count, normals.Count));

                            if (corners.Count < 3)
                                throw new FormatException($"face has only {corners.Count} corners");
                            if (corners.Count > 3)
                                polygonCount++;
                            
                            // Keeps the original corner order, so the winding direction is preserved.
                            for (int i = 1; i + 1 < corners.Count; i++)
                            {
                                AddCorner(corners[0]);
                                AddCorner(corners[i]);
                                AddCorner(corners[i + 1]);
                                triangleCount++;
                            }
                            break;
                    }
                }
                catch (Exception e) when (e is FormatException or OverflowException or IndexOutOfRangeException)
                {
                    if (warningCount++ < MaxWarnings)
                        DebugText.Log($"{fileName} line {lineNumber + 1}: {e.Message}", Color4.Orange, 8f);
                }
            }

            // Drop the default part if every face belonged to a named object
            if (splitByObject && meshes.Count > 1 && meshes[fileName].VertexCount == 0)
                meshes.Remove(fileName);

            // --- Report ---
            int vertexTotal = 0, indexTotal = 0;
            foreach (var mesh in meshes.Values)
            {
                vertexTotal += mesh.VertexCount;
                indexTotal += mesh.IndexCount;
            }
            string partList = splitByObject ? $" in {meshes.Count} parts ({string.Join(", ", meshes.Keys)})" : "";
            string summary = $"{fileName}: {positions.Count} v, {uvs.Count} vt, {normals.Count} vn, " +
                             $"{triangleCount} tris: {vertexTotal} vertices, {indexTotal} indices{partList}";
            Console.WriteLine(summary);
            DebugText.Log(summary, Color4.LightGreen, 6f);

            if (warningCount > MaxWarnings)
                DebugText.Log($"{fileName}: {warningCount - MaxWarnings} more bad lines not shown", Color4.Orange, 8f);
            if (polygonCount > 0)
                DebugText.Log($"{fileName}: {polygonCount} faces were not triangles (fan-split; export Triangulated)", Color4.Orange, 8f);
            if (missingUVs)
                DebugText.Log($"{fileName}: some corners have no UVs (export with UV Coordinates)", Color4.Orange, 8f);
            if (missingNormals)
                DebugText.Log($"{fileName}: some corners have no normals (export with Normals)", Color4.Orange, 8f);

            return meshes;

            // Adds one triangle corner to the output, reusing the existing vertex if this exact
            // (position, UV, normal) combination has been seen before.
            void AddCorner((int Position, int UV, int Normal) corner)
            {
                if (!vertexLookup.TryGetValue(corner, out uint index))
                {
                    index = (uint)current.VertexCount;
                    vertexLookup[corner] = index;

                    current.Positions.Add(positions[corner.Position]);

                    if (corner.UV >= 0)
                        current.UVs.Add(uvs[corner.UV]);
                    else
                    {
                        current.UVs.Add(Vector2.Zero);
                        missingUVs = true;
                    }

                    if (corner.Normal >= 0)
                        current.Normals.Add(normals[corner.Normal]);
                    else
                    {
                        current.Normals.Add(Vector3.UnitY);
                        missingNormals = true;
                    }
                }

                current.Indices.Add(index);
            }
        }

        #region Helper Methods
        /// <summary>
        /// Parses one face corner ("v", "v/vt", "v//vn" or "v/vt/vn") into 0-based indices.
        /// Indices are resolved against the lists read so far, which is what the OBJ format specifies.
        /// </summary>
        private static (int Position, int UV, int Normal) ParseFaceVertex(
            string token, int positionCount, int uvCount, int normalCount)
        {
            string[] fields = token.Split('/');

            int position = ResolveIndex(fields[0], positionCount, "position");
            int uv = fields.Length > 1 && fields[1].Length > 0 ? ResolveIndex(fields[1], uvCount, "UV") : -1;
            int normal = fields.Length > 2 && fields[2].Length > 0 ? ResolveIndex(fields[2], normalCount, "normal") : -1;

            return (position, uv, normal);
        }

        /// <summary>
        /// Converts an OBJ index to a 0-based list index. Positive indices are 1-based (1 = first);
        /// negative indices count back from the end of the list so far (-1 = most recent). 0 is invalid.
        /// </summary>
        private static int ResolveIndex(string text, int count, string kind)
        {
            int objIndex = int.Parse(text);
            int index = objIndex > 0 ? objIndex - 1 : count + objIndex;

            if (objIndex == 0 || index < 0 || index >= count)
                throw new FormatException($"{kind} index {objIndex} is out of range (only {count} defined)");

            return index;
        }

        /// <summary>
        /// Parses "keyword x y z" into a Vector3.
        /// </summary>
        private static Vector3 ParseVector3(string[] parts)
            => new(ParseFloat(parts[1]), ParseFloat(parts[2]), ParseFloat(parts[3]));

        /// <summary>
        /// Parses a float from a string.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private static float ParseFloat(string text)
            => float.Parse(text);        
        #endregion
    }
}
