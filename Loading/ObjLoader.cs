using _3DDemoA2.Debugging;
using _3DDemoA2.Graphics;
using _3DDemoA2.Utilities;
using OpenTK.Mathematics;
using Corner = (int Position, int UV, int Normal);

namespace _3DDemoA2.Loading
{
    /// <summary>
    /// Loads a Wavefront .obj file into a MeshData object.
    ///
    /// The .obj format is simple and widely supported, but it has some quirks:
    /// - It can contain multiple named objects, which can be loaded as separate MeshData objects
    ///   or merged into one.
    ///   This loader supports both modes.
    /// - It can contain faces with more than three corners (quads or polygons).
    ///   This loader triangulates them using a fan method, but it logs a warning because the winding order may not be preserved.
    /// - It can contain corners with missing UVs or normals.
    ///   This loader fills in default values and logs a warning.
    /// </summary>
    internal static class ObjLoader
    {
        #region Constants
        // Maximum number of malformed-line warnings shown on screen per file, so a broken file can't flood the log.
        private const int MaxWarnings = 5;
        #endregion

        #region Public API
        /// <summary>
        /// Loads an .obj file from the Models folder as one mesh.
        /// </summary>
        /// <param name="fileName">The .obj file.</param>
        /// <returns>The mesh data, ready to be passed to a Mesh.</returns>
        public static MeshData Load(string fileName) => Parse(fileName, splitByObject: false)[fileName];

        /// <summary>
        /// Loads an .obj file from the Models folder as separate parts, one per named object in the file.
        /// </summary>
        /// <param name="fileName">The .obj file.</param>
        /// <returns>Each part's mesh data, keyed by its object name.</returns>
        public static Dictionary<string, MeshData> LoadParts(string fileName) => Parse(fileName, splitByObject: true);
        #endregion

        #region Helper Methods
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
            var vertexLookup = new Dictionary<Corner, uint>();
            var corners = new List<Corner>(4);

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
                            // Parse "v x y z" into a Vector3 and add it to the raw positions list.
                            positions.Add(ParseVector3(parts));
                            break;

                        case "vt":
                            // Parse "vt u v" into a Vector2 and add it to the raw UVs list.
                            // OBJ UVs are 0..1, but some exporters use 0..1 for the bottom-left corner and some use 0..1 for the top-left corner.
                            // This loader does not flip the V coordinate, so the model's UVs must match the texture's orientation.
                            uvs.Add(new Vector2(ParseFloat(parts[1]), ParseFloat(parts[2])));
                            break;

                        case "vn":
                            // Parse "vn x y z" into a Vector3 and add it to the raw normals list.
                            normals.Add(ParseVector3(parts));
                            break;

                        case "o":
                            // Start a new mesh for this named object, if splitting is enabled.
                            // Otherwise ignore the name and keep adding to the current mesh.
                            if (splitByObject)
                            {
                                current = new MeshData();
                                meshes[line.Substring(1).Trim()] = current;
                                vertexLookup = new Dictionary<Corner, uint>();
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

                            // Note any corners without UVs or normals, for the warnings at the end (AddCorner fills in defaults)
                            foreach (var corner in corners)
                            {
                                if (corner.UV < 0)
                                    missingUVs = true;
                                if (corner.Normal < 0)
                                    missingNormals = true;
                            }

                            // Keeps the original corner order, so the winding direction is preserved.
                            for (int i = 1; i + 1 < corners.Count; i++)
                            {
                                AddCorner(corners[0], current, vertexLookup, positions, uvs, normals);
                                AddCorner(corners[i], current, vertexLookup, positions, uvs, normals);
                                AddCorner(corners[i + 1], current, vertexLookup, positions, uvs, normals);
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
        }
        
        /// <summary>
        /// Adds one triangle corner to a mesh, reusing the existing vertex if this exact
        /// (position, UV, normal) combination has been seen before in that mesh.
        /// A missing UV or normal (index -1) is filled with a default.
        /// </summary>
        /// <param name="corner">The corner's 0-based indices into the raw lists (-1 = missing).</param>
        /// <param name="mesh">The mesh being built.</param>
        /// <param name="vertexLookup">The mesh's vertices so far, keyed by their index combination.</param>
        /// <param name="positions">Raw positions read from the file.</param>
        /// <param name="uvs">Raw UVs read from the file.</param>
        /// <param name="normals">Raw normals read from the file.</param>
        private static void AddCorner(Corner corner, 
            MeshData mesh,
            Dictionary<Corner, uint> vertexLookup,
            List<Vector3> positions, 
            List<Vector2> uvs, 
            List<Vector3> normals)
        {
            if (!vertexLookup.TryGetValue(corner, out uint index))
            {
                index = (uint)mesh.VertexCount;
                vertexLookup[corner] = index;

                mesh.Positions.Add(positions[corner.Position]);

                if (corner.UV >= 0)
                    mesh.UVs.Add(uvs[corner.UV]);
                else
                    mesh.UVs.Add(Vector2.Zero);

                if (corner.Normal >= 0)
                    mesh.Normals.Add(normals[corner.Normal]);
                else
                    mesh.Normals.Add(Vector3.UnitY);
            }

            mesh.Indices.Add(index);
        }
        
        /// <summary>
        /// Parses one face corner ("v", "v/vt", "v//vn" or "v/vt/vn") into 0-based indices.
        /// Indices are resolved against the lists read so far, which is what the OBJ format specifies.
        /// </summary>
        /// <param name="token">The face corner token to parse.</param>
        /// <param name="positionCount">The number of positions read so far.</param>
        /// <param name="uvCount">The number of UVs read so far.</param>
        /// <param name="normalCount">The number of normals read so far.</param>
        /// <returns>A Corner struct with 0-based indices.</returns>
        private static Corner ParseFaceVertex(
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
        /// <param name="text">The OBJ index as a string.</param>
        /// <param name="count">The number of elements in the list so far.</param>
        /// <param name="kind">A description of the kind of index (e.g., "position", "UV", "normal").</param>
        /// <returns>The 0-based list index.</returns>
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
        /// <param name="parts">The parts of the line split by spaces.</param>
        /// <returns>The parsed Vector3.</returns>
        private static Vector3 ParseVector3(string[] parts)
            => new(ParseFloat(parts[1]), ParseFloat(parts[2]), ParseFloat(parts[3]));

        /// <summary>
        /// Parses a float from a string.
        /// </summary>
        /// <param name="text">The string to parse.</param>
        /// <returns>The parsed float.</returns>
        private static float ParseFloat(string text)
            => float.Parse(text);        
        #endregion
    }
}
