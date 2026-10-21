using _3DDemoA2.Debugging;
using _3DDemoA2.Graphics;
using _3DDemoA2.Loading;
using OpenTK.Mathematics;

namespace _3DDemoA2.Scene
{
    /// <summary>
    /// The animated cannon, built from the three parts in Cannon.obj: the Mount, Wheels and Barrel.
    ///
    /// Every part was modelled around the same pivot, the centre of the axle, so the Wheels and Barrel
    /// sit at zero offset from the Mount and only the Mount is lifted above the root.
    /// </summary>
    internal class Cannon : IRenderable
    {
        #region Constants
        // The axle centre which is the pivot all three parts rotate around
        private static readonly Vector3 AxlePivot = new(0f, 0.58f, 0f);

        // The wheels touch the ground (y = 0) and are centred on the axle, so the radius is the axle height
        private const float WHEELRADIUS = 0.58f;

        // Barrel elevation limits, in degrees (positive raises the muzzle)
        private const float MINELEVATION = -20f;
        private const float MAXELEVATION = 20f;

        // Movement speeds
        private const float ROLLSPEED = 2f; // metres per second
        private const float TURNSPEED = 60f; // degrees per second
        private const float ELEVATESPEED = 30f; // degrees per second
        
        // How many times the tiling metal maps repeat across the cannon's atlas
        private const float METALDETAILSCALE = 2f;

        // Roughness for the metal when no roughness map is given (fairly polished)
        private const float METALROUGHNESS = 0.35f;
        #endregion
        
        #region Public Fields
        /// <summary>
        /// Gets the current elevation of the cannon barrel in degrees,
        /// clamped between MINELEVATION and MAXELEVATION.
        /// </summary>
        public float Elevation { get; private set; }
        
        /// <summary>
        /// Gets the current heading of the cannon in degrees,
        /// where 0 is the model's rest pose, with the muzzle pointing along the negative Z-axis.
        /// </summary>
        public float Heading { get; private set; }

        /// <summary>
        /// Gets the current position of the cannon in world space,
        /// which is the point on the ground under the axle.
        /// </summary>
        public Vector3 Position => _root.Position;
        #endregion
        
        #region Private Fields
        // Hierarchy of the cannon parts, with the root at the axle pivot
        private readonly ModelPart _root;
        private readonly ModelPart _barrel;
        private readonly ModelPart _wheels;
        
        // GPU resources this class owns, deleted exactly once in Delete()
        private readonly List<Mesh> _meshes = new();
        private readonly List<Texture> _textures = new();

        // How far the root may move from the centre of the world, to keep it on the island
        private readonly float _maxDistanceFromCentre;

        // Current wheel spin, in radians
        private float _wheelAngle;
        #endregion

        /// <summary>
        /// Constructor that loads the cannon's parts, re-centres each on the axle and builds the hierarchy.
        /// The parameters follow the same order as StaticProp's; any texture file left as null uses the
        /// material's neutral default (no map). The normal, metallic and roughness maps are for the metal parts.
        /// </summary>
        /// <param name="name">Name used for the root part and in log messages.</param>
        /// <param name="objFile">The .obj file, which must contain parts named Mount, Wheels and Barrel.</param>
        /// <param name="position">Starting position on the ground.</param>
        /// <param name="colorMapFile">The colour (albedo) atlas, shared by every part, or null for plain white.</param>
        /// <param name="normalMapFile">The metal's normal map (OpenGL convention), or null for the mesh normals.</param>
        /// <param name="metallicMapFile">The metal's metallic map, or null for fully metal everywhere.</param>
        /// <param name="roughnessMapFile">The metal's roughness map, or null for an even, fairly polished finish.</param>
        /// <param name="maxDistanceFromCentre">How far from the island's centre the cannon may roll.</param>
        public Cannon(string name, string objFile, Vector3 position,
            string? colorMapFile, string? normalMapFile, string? metallicMapFile, string? roughnessMapFile,
            float maxDistanceFromCentre)
        {
            _maxDistanceFromCentre = maxDistanceFromCentre;

            var parts = ObjLoader.LoadParts(objFile);

            _root = new ModelPart(name) { Position = position };
            ModelPart mount = _root.AddChild(CreatePart(parts, "Mount"));
            mount.Position = AxlePivot;
            // Children draw in the order they are added: the Barrel goes straight after the Mount because they share
            // the metal material (so it is applied once for both), then the Wheels with their own rubber material
            _barrel = mount.AddChild(CreatePart(parts, "Barrel"));
            _wheels = mount.AddChild(CreatePart(parts, "Wheels"));

            // --- Materials ---
            // Scratched metal for the whole cannon, set on the root so every part inherits it.
            // The maps decide metal vs dirt and smooth vs scratched per pixel.
            // The colour atlas is shared by both materials below, which works because both use it as the albedo map (unit 0)
            Texture? colorMap = LoadTexture(colorMapFile, Material.ALBEDOMAPUNIT);
            Texture? roughnessMap = LoadTexture(roughnessMapFile, Material.ROUGHNESSMAPUNIT);

            // With a roughness map, a value of 1 lets the map decide; without one, an even polished finish
            float roughness = METALROUGHNESS;
            if (roughnessMap != null)
                roughness = 1f;

            _root.Material = new Material
            {
                AlbedoMap = colorMap,
                NormalMap = LoadTexture(normalMapFile, Material.NORMALMAPUNIT),
                MetallicMap = LoadTexture(metallicMapFile, Material.METALLICMAPUNIT),
                RoughnessMap = roughnessMap,
                Metallic = 1f,
                Roughness = roughness,
                DetailScale = METALDETAILSCALE
            };

            // Rubber wheels override it: same colour atlas but no detail maps, not metallic,
            // mostly rough (a faint, broad sheen)
            _wheels.Material = new Material { AlbedoMap = colorMap, Metallic = 0f, Roughness = 0.9f };
        }
        
        #region Public API
        /// <summary>
        /// Rolls the cannon forwards (towards the muzzle) or backwards, spinning the wheels to match.
        /// Stops at the edge of the island instead of rolling off it.
        /// </summary>
        /// <param name="direction">1 = forwards, -1 = backwards, 0 = no movement.</param>
        /// <param name="deltaTime">Seconds since the last frame.</param>
        public void Roll(float direction, float deltaTime)
        {
            if (direction == 0f)
                return;
            
            float distance = direction * ROLLSPEED * deltaTime;
            
            // Forward is the way the muzzle points (-Z in the model), turned by the current heading
            Vector3 forward = Vector3.Transform(-Vector3.UnitZ, _root.Rotation);
            Vector3 next = _root.Position + forward * distance;

            // Limit: stay within a circle on the island (height is ignored)
            if (next.Xz.Length > _maxDistanceFromCentre)
                return;

            _root.Position = next;
            
            // Wheels roll without slipping: the arc they turn through equals the distance travelled.
            // Rolling towards -Z turns the top of the wheel towards -Z, which is a negative rotation around X.
            _wheelAngle = (_wheelAngle - distance / WHEELRADIUS) % MathHelper.TwoPi;
            _wheels.Rotation = Quaternion.FromAxisAngle(Vector3.UnitX, _wheelAngle);
        }

        /// <summary>
        /// Turns the whole cannon left or right around the vertical axis.
        /// </summary>
        /// <param name="direction">1 = left (anticlockwise from above), -1 = right, 0 = no turn.</param>
        /// <param name="deltaTime">Seconds since the last frame.</param>
        public void Turn(float direction, float deltaTime)
        {
            if (direction == 0f)
                return;
            
            Heading = (Heading + direction * TURNSPEED * deltaTime) % 360f;
            _root.Rotation = Quaternion.FromAxisAngle(Vector3.UnitY, MathHelper.DegreesToRadians(Heading));
        }

        /// <summary>
        /// Raises or lowers the barrel, clamped to its elevation limits.
        /// </summary>
        /// <param name="direction">1 = raise the muzzle, -1 = lower it, 0 = no change.</param>
        /// <param name="deltaTime">Seconds since the last frame.</param>
        public void Elevate(float direction, float deltaTime)
        {
            if (direction == 0f)
                return;
            
            Elevation = Math.Clamp(Elevation + direction * ELEVATESPEED * deltaTime, MINELEVATION, MAXELEVATION);
            
            // With the muzzle towards -Z, a positive rotation around X lifts it
            _barrel.Rotation = Quaternion.FromAxisAngle(Vector3.UnitX, MathHelper.DegreesToRadians(Elevation));
        }
        #endregion
        
        #region Public API
        /// <summary>
        /// Draws the cannon. The shader is already bound, with the view, projection and lighting set.
        /// </summary>
        /// <param name="shader">The shader program to use for rendering.</param>
        public void Render(ShaderProgram shader)
        {
            // Each part's material binds its own maps (including the colour atlas) as it draws
            _root.Render(shader, Matrix4.Identity);
        }

        /// <summary>
        /// Frees the cannon's GPU resources (meshes and textures).
        /// </summary>
        public void Delete()
        {
            foreach (var mesh in _meshes)
                mesh.Delete();
            _meshes.Clear();

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
        /// <param name="unit">The texture unit for the map slot it fills (e.g. Material.NORMALMAPUNIT).</param>
        /// <returns>The loaded texture, or null.</returns>
        private Texture? LoadTexture(string? file, int unit)
        {
            if (file == null)
                return null;

            var texture = new Texture(file, unit);
            _textures.Add(texture);
            return texture;
        }

        /// <summary>
        /// Creates a part from the loaded data, moving its vertices so the axle pivot becomes its local origin.
        /// A missing part is logged and becomes an empty node, so the rest of the cannon still draws.
        /// </summary>
        /// <param name="parts">The parts loaded from the cannon model.</param>
        /// <param name="name">The part's object name.</param>
        /// <returns>The new part.</returns>
        private ModelPart CreatePart(Dictionary<string, MeshData> parts, string name)
        {
            if (!parts.TryGetValue(name, out MeshData? data))
            {
                DebugText.Log($"{_root.Name} has no part named '{name}'", Color4.Red, 8f);
                return new ModelPart(name);
            }

            data.Translate(-AxlePivot);
            var mesh = new Mesh(data, name);
            _meshes.Add(mesh);
            return new ModelPart(name, mesh);
        }
        #endregion
    }
}