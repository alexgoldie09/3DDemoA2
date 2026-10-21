using OpenTK.Mathematics;

namespace _3DDemoA2.Graphics
{
    /// <summary>
    /// How a surface looks and reacts to light: optional albedo (colour), normal, metallic and roughness maps,
    /// plus a value for each.
    /// Each value is multiplied by its map (the same "factor x texture" convention the glTF format uses).
    /// A missing map is replaced by a neutral 1x1 default texture, so the value is then used on its own.
    /// So one atlas can be fully metal on one part while another part, like the wheels, just uses plain values.
    ///
    /// This is not physically based rendering: the values feed the existing Blinn-Phong lighting
    /// (see Model.frag), with roughness controlling the highlight and metallic tinting it.
    /// A material does not own its textures meaning whoever created them deletes them.
    /// </summary>
    internal class Material
    {
        #region Texture Unit Constants
        // One texture unit per map. Unit 4 onwards is left free (e.g. for the skybox cube map)
        public const int ALBEDOMAPUNIT = 0;
        public const int NORMALMAPUNIT = 1;
        public const int METALLICMAPUNIT = 2;
        public const int ROUGHNESSMAPUNIT = 3;
        #endregion

        #region Default Textures
        // Bound in place of a missing map, so every sampler always has a texture (macOS warns about empty units).
        // White leaves a value unchanged (value x 1); flat blue (128, 128, 255) unpacks to about (0, 0, 1),
        // a normal pointing straight out of the surface, so the mesh normal is left as it is.
        // Created on first use (they need the OpenGL context), shared by every material, freed by DeleteDefaults.
        private static Texture? _whiteTexture;
        private static Texture? _flatNormalTexture;
        #endregion

        #region Redundant State Tracking
        // The material applied most recently. Its maps are still bound and its values still uploaded,
        // so applying it again straight away would only repeat the same OpenGL calls.
        private static Material? _lastApplied;

        /// <summary>
        /// How many materials were actually applied (bound and uploaded) since the last reset.
        /// </summary>
        public static int AppliedCount { get; private set; }

        /// <summary>
        /// How many applies were skipped since the last reset, because that material was already current.
        /// </summary>
        public static int SkippedCount { get; private set; }
        #endregion

        #region Public Fields
        /// <summary>
        /// The surface colour (albedo) map, e.g. a model's baked atlas, or null to use Colour alone.
        /// Sampled with the model's own UVs (not DetailScale), since it is laid out to match the model.
        /// </summary>
        public Texture? AlbedoMap { get; set; }

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
        /// Colour tint, multiplying the albedo map if there is one (white = the map's colours unchanged).
        /// </summary>
        public Vector3 Colour { get; set; } = Vector3.One;

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
        /// How many times the detail maps (normal, metallic, roughness) repeat across the UVs.
        /// Tiling detail maps on an atlas need more than 1,
        /// otherwise one copy is stretched over the whole model.
        /// </summary>
        public float DetailScale { get; set; } = 1f;
        #endregion

        #region Public API
        /// <summary>
        /// Binds this material's maps to their texture units and uploads its values,
        /// unless it is already the current material (then nothing needs to change).
        /// Note: changing a material's values between two draws in the same frame won't be picked up
        /// for the second draw; call ResetLastApplied first if that's ever needed.
        /// </summary>
        /// <param name="shader">The bound model shader.</param>
        public void Apply(ShaderProgram shader)
        {
            if (_lastApplied == this)
            {
                SkippedCount++;
                return;
            }

            _lastApplied = this;
            AppliedCount++;

            if (_whiteTexture == null)
                _whiteTexture = new Texture(255, 255, 255);
            if (_flatNormalTexture == null)
                _flatNormalTexture = new Texture(128, 128, 255);

            BindOrDefault(AlbedoMap, _whiteTexture, ALBEDOMAPUNIT);
            BindOrDefault(NormalMap, _flatNormalTexture, NORMALMAPUNIT);
            BindOrDefault(MetallicMap, _whiteTexture, METALLICMAPUNIT);
            BindOrDefault(RoughnessMap, _whiteTexture, ROUGHNESSMAPUNIT);

            shader.SetVector3("albedoFactor", Colour);
            shader.SetFloat("metallicFactor", Metallic);
            shader.SetFloat("roughnessFactor", Roughness);
            shader.SetFloat("normalStrength", NormalStrength);
            shader.SetFloat("detailScale", DetailScale);
        }

        /// <summary>
        /// Forgets the current material and resets the counts. Call at the start of each frame's scene drawing:
        /// other drawing in between (e.g. DebugText binding its font to unit 0) can replace what was bound.
        /// </summary>
        public static void ResetLastApplied()
        {
            _lastApplied = null;
            AppliedCount = 0;
            SkippedCount = 0;
        }

        /// <summary>
        /// Frees the shared default textures. Call once when closing, after the scene is deleted.
        /// </summary>
        public static void DeleteDefaults()
        {
            _lastApplied = null;

            if (_whiteTexture != null)
            {
                _whiteTexture.Delete();
                _whiteTexture = null;
            }

            if (_flatNormalTexture != null)
            {
                _flatNormalTexture.Delete();
                _flatNormalTexture = null;
            }
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// Binds a map to its texture unit, or the default texture when the material has no such map.
        /// </summary>
        /// <param name="map">The material's map, or null.</param>
        /// <param name="fallback">The neutral default to bind instead.</param>
        /// <param name="unit">The texture unit.</param>
        private static void BindOrDefault(Texture? map, Texture fallback, int unit)
        {
            if (map != null)
                map.Bind(unit);
            else
                fallback.Bind(unit);
        }
        #endregion
    }
}
