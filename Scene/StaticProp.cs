using _3DDemoA2.Graphics;
using _3DDemoA2.Loading;
using OpenTK.Mathematics;

namespace _3DDemoA2.Scene
{
    /// <summary>
    /// Represents a static prop in the scene: a 3D model that does not animate or move, drawn with one material
    /// built from up to four texture files (colour, normal, metallic, roughness). Any file left as null uses the
    /// material's neutral default instead.
    /// </summary>
    internal class StaticProp : IRenderable
    {
        #region Private Fields
        // The root of the static prop (its material lives on the root part)
        private readonly ModelPart _root;

        // Every texture this prop loaded, deleted exactly once in Delete()
        private readonly List<Texture> _textures = new();
        #endregion
        
        /// <summary>
        /// Constructor that loads the model and its textures and places it in the world.
        /// The textures are owned and deleted by this prop.
        /// </summary>
        /// <param name="name">Name used in log messages.</param>
        /// <param name="objFile">The .obj file.</param>
        /// <param name="position">World position of the model's origin.</param>
        /// <param name="colorMapFile">The colour (albedo) texture file, or null for plain white.</param>
        /// <param name="normalMapFile">The normal map file (OpenGL convention), or null for the mesh normals.</param>
        /// <param name="metallicMapFile">The metallic map file, or null for the material's Metallic value alone.</param>
        /// <param name="roughnessMapFile">The roughness map file, or null for the material's Roughness value alone.</param>
        /// <param name="yawDegrees">Rotation around the vertical (Y) axis, in degrees.</param>
        /// <param name="material">The material values to use (e.g. DetailScale); null = a new matte material.
        /// Any texture file given above replaces the matching map in it.</param>
        public StaticProp(string name, string objFile, Vector3 position,
            string? colorMapFile, string? normalMapFile, string? metallicMapFile, string? roughnessMapFile,
            float yawDegrees = 0f, Material? material = null)
        {
            if (material == null)
            {
                material = new Material(); // matte: roughness 1, not metallic

                // A metallic map is multiplied by the Metallic value, so it needs 1 to show
                // (the default 0 would cancel it out). Roughness already defaults to 1.
                if (metallicMapFile != null)
                    material.Metallic = 1f;
            }

            Texture? colorMap = LoadTexture(colorMapFile);
            if (colorMap != null)
                material.AlbedoMap = colorMap;

            Texture? normalMap = LoadTexture(normalMapFile);
            if (normalMap != null)
                material.NormalMap = normalMap;

            Texture? metallicMap = LoadTexture(metallicMapFile);
            if (metallicMap != null)
                material.MetallicMap = metallicMap;

            Texture? roughnessMap = LoadTexture(roughnessMapFile);
            if (roughnessMap != null)
                material.RoughnessMap = roughnessMap;

            _root = new ModelPart(name, new Mesh(ObjLoader.Load(objFile), name))
            {
                Position = position,
                Rotation = Quaternion.FromAxisAngle(Vector3.UnitY, MathHelper.DegreesToRadians(yawDegrees)),
                Material = material
            };
        }
        
        #region IRenderable Functions
        /// <summary>
        /// Draws the static prop. The shader is already bound, with the view, projection and lighting set.
        /// </summary>
        /// <param name="shader">The shader program to use for rendering.</param>
        public void Render(ShaderProgram shader) =>
            _root.Render(shader, Matrix4.Identity);

        /// <summary>
        /// Frees the static prop's GPU resources (meshes and textures).
        /// </summary>
        public void Delete()
        {
            _root.Mesh?.Delete();
            foreach (var texture in _textures)
                texture.Delete();
            _textures.Clear();
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// Loads a texture and keeps it for deleting later, or returns null when no file is given.
        /// </summary>
        /// <param name="file">The texture file in the Textures folder, or null.</param>
        /// <returns>The loaded texture, or null.</returns>
        private Texture? LoadTexture(string? file)
        {
            if (file == null)
                return null;

            var texture = new Texture(file);
            _textures.Add(texture);
            return texture;
        }
        #endregion
    }
}