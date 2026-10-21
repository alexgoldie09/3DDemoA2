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
        #endregion
        
        #region Public Fields
        /// <summary>
        /// Gets the current elevation of the cannon barrel in degrees,
        /// clamped between MINELEVATION and MAXELEVATION.
        /// </summary>
        public float Elevation { get; private set; }
        
        /// <summary>
        /// Gets the current heading of the cannon in degrees,
        /// where 0 points along the positive Z-axis.
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
        private readonly Texture _colorMap;
        private readonly Texture _metalNormalMap;
        private readonly Texture _metalMetallicMap;
        private readonly Texture _metalRoughnessMap;

        // How far the root may move from the centre of the world, to keep it on the island
        private readonly float _maxDistanceFromCentre;

        // Current wheel spin, in radians
        private float _wheelAngle;
        #endregion

        /// <summary>
        /// Constructor that loads the cannon's parts, re-centres each on the axle and builds the hierarchy.
        /// </summary>
        /// <param name="position">Starting position on the ground.</param>
        /// <param name="maxDistanceFromCentre">How far from the island's centre the cannon may roll.</param>
        public Cannon(Vector3 position, float maxDistanceFromCentre)
        {
            _maxDistanceFromCentre = maxDistanceFromCentre;

            var parts = ObjLoader.LoadParts("Cannon.obj");

            _root = new ModelPart("Cannon") { Position = position };
            ModelPart mount = _root.AddChild(CreatePart(parts, "Mount"));
            mount.Position = AxlePivot;
            _wheels = mount.AddChild(CreatePart(parts, "Wheels"));
            _barrel = mount.AddChild(CreatePart(parts, "Barrel"));

            // --- Materials ---
            // Scratched metal for the whole cannon, set on the root so every part inherits it.
            // The maps decide metal vs dirt and smooth vs scratched per pixel.
            _colorMap = new Texture("CannonColorTexture.png");
            _metalNormalMap = new Texture("MetalNormalGLTexture.jpg");
            _metalMetallicMap = new Texture("MetalMetallicTexture.jpg");
            _metalRoughnessMap = new Texture("MetalRoughnessTexture.jpg");
            _root.Material = new Material
            {
                AlbedoMap = _colorMap,
                NormalMap = _metalNormalMap,
                MetallicMap = _metalMetallicMap,
                RoughnessMap = _metalRoughnessMap,
                Metallic = 1f,
                Roughness = 1f,
                DetailScale = METALDETAILSCALE
            };

            // Rubber wheels override it: same colour atlas but no detail maps, not metallic,
            // mostly rough (a faint, broad sheen)
            _wheels.Material = new Material { AlbedoMap = _colorMap, Metallic = 0f, Roughness = 0.9f };
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
        
        #region IRenderable Functions
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
        /// Frees the cannon's GPU resources (meshes and texture).
        /// </summary>
        public void Delete()
        {
            foreach (var mesh in _meshes)
                mesh.Delete();
            _meshes.Clear();
            _colorMap.Delete();
            _metalNormalMap.Delete();
            _metalMetallicMap.Delete();
            _metalRoughnessMap.Delete();
        }
        #endregion
        
        #region Helper Methods
        /// <summary>
        /// Creates a part from the loaded data, moving its vertices so the axle pivot becomes its local origin.
        /// A missing part is logged and becomes an empty node, so the rest of the cannon still draws.
        /// </summary>
        /// <param name="parts">The parts loaded from Cannon.obj.</param>
        /// <param name="name">The part's object name.</param>
        /// <returns>The new part.</returns>
        private ModelPart CreatePart(Dictionary<string, MeshData> parts, string name)
        {
            if (!parts.TryGetValue(name, out MeshData? data))
            {
                DebugText.Log($"Cannon.obj has no part named '{name}'", Color4.Red, 8f);
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