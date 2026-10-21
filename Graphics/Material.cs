namespace _3DDemoA2.Graphics
{
    /// <summary>
    /// How a surface reacts to light: optional normal, metallic and roughness maps, plus a value for each.
    /// Each value is multiplied by its map when the map is present, or used on its own when it isn't
    /// (the same "factor x texture" convention the glTF format uses). So one atlas can be fully metal
    /// on one part while another part, like the wheels, just uses plain values with no maps.
    ///
    /// This is not physically based rendering: the values feed the existing Blinn-Phong lighting
    /// (see Model.frag), with roughness controlling the highlight and metallic tinting it.
    /// A material does not own its textures meaning whoever created them deletes them.
    /// </summary>
    internal class Material
    {
        #region Texture Unit Constants
        // Unit 0 holds the colour texture (bound by each model)
        // Unit 4 onwards is left free (e.g. for the skybox cube map)
        public const int NORMALMAPUNIT = 1;
        public const int METALLICMAPUNIT = 2;
        public const int ROUGHNESSMAPUNIT = 3;
        #endregion

        #region Public Fields
        /// <summary>
        /// Tangent-space normal map (OpenGL convention, green = +Y), or null to use the mesh normals.
        /// </summary>
        public Texture? NormalMap { get; set; }

        /// <summary>
        /// Greyscale metallic map (white = metal), or null to use Metallic alone.
        /// </summary>
        public Texture? MetallicMap { get; set; }

        /// <summary>
        /// Greyscale roughness map (black = smooth, white = rough), or null to use Roughness alone.
        /// </summary>
        public Texture? RoughnessMap { get; set; }

        /// <summary>
        /// How metallic the surface is, 0 to 1 (multiplies the metallic map if there is one).
        /// </summary>
        public float Metallic { get; set; } = 0f;

        /// <summary>
        /// How rough the surface is, 0 to 1 (multiplies the roughness map if there is one).
        /// 1 = fully matte, no highlight.
        /// </summary>
        public float Roughness { get; set; } = 1f;

        /// <summary>
        /// How strongly the normal map bends the surface: 0 = flat, 1 = as authored.
        /// </summary>
        public float NormalStrength { get; set; } = 1f;

        /// <summary>
        /// How many times the maps repeat across the UVs. Tiling detail maps on an atlas need more than 1,
        /// otherwise one copy is stretched over the whole model.
        /// </summary>
        public float DetailScale { get; set; } = 1f;
        #endregion

        #region Public API
        /// <summary>
        /// Binds this material's maps to their texture units and uploads its values.
        /// </summary>
        /// <param name="shader">The bound model shader.</param>
        public void Apply(ShaderProgram shader)
        {
            NormalMap?.Bind(NORMALMAPUNIT);
            MetallicMap?.Bind(METALLICMAPUNIT);
            RoughnessMap?.Bind(ROUGHNESSMAPUNIT);

            shader.SetInt("useNormalMap", NormalMap != null ? 1 : 0);
            shader.SetInt("useMetallicMap", MetallicMap != null ? 1 : 0);
            shader.SetInt("useRoughnessMap", RoughnessMap != null ? 1 : 0);

            shader.SetFloat("metallicFactor", Metallic);
            shader.SetFloat("roughnessFactor", Roughness);
            shader.SetFloat("normalStrength", NormalStrength);
            shader.SetFloat("detailScale", DetailScale);
        }
        #endregion
    }
}
