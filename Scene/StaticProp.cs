using _3DDemoA2.Graphics;
using _3DDemoA2.Loading;
using OpenTK.Mathematics;

namespace _3DDemoA2.Scene
{
    /// <summary>
    /// Represents a static prop in the scene, which is a 3D model with a single texture that does not animate or move.
    /// </summary>
    internal class StaticProp : IRenderable
    {
        #region Private Fields
        // The root and colour texture of the static prop (its material, which uses the texture, lives on the root part)
        private readonly ModelPart _root;
        private readonly Texture _colorMap;
        #endregion
        
        /// <summary>
        /// Constructor that loads the model and its texture and places it in the world.
        /// </summary>
        /// <param name="name">Name used in log messages.</param>
        /// <param name="objFile">The .obj file.</param>
        /// <param name="textureFile">The colour (albedo) texture file, owned and deleted by this prop.</param>
        /// <param name="position">World position of the model's origin.</param>
        /// <param name="yawDegrees">Rotation around the vertical (Y) axis, in degrees.</param>
        /// <param name="material">How the surface reacts to light; null = matte. Its AlbedoMap is set to this prop's
        /// texture; any other maps are owned by the caller.</param>
        public StaticProp(string name, string objFile, string textureFile, Vector3 position, float yawDegrees = 0f, Material? material = null)
        {
            _colorMap = new Texture(textureFile);

            if (material == null)
                material = new Material(); // matte: no detail maps, roughness 1

            material.AlbedoMap = _colorMap;

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
            _colorMap.Delete();
        }
        #endregion
    }
}