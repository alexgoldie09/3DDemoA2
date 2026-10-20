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
        // The root, texture, and specular strength of the static prop
        private readonly ModelPart _root;
        private readonly Texture _texture;
        private readonly float _specularStrength;
        #endregion
        
        /// <summary>
        /// Constructor that loads the model and its texture and places it in the world.
        /// </summary>
        /// <param name="name">Name used in log messages.</param>
        /// <param name="objFile">The .obj file.</param>
        /// <param name="textureFile">The texture file.</param>
        /// <param name="position">World position of the model's origin.</param>
        /// <param name="yawDegrees">Rotation around the vertical (Y) axis, in degrees.</param>
        /// <param name="specularStrength">The strength of the specular highlight.</param>
        public StaticProp(string name, string objFile, string textureFile, Vector3 position, float yawDegrees = 0f, float specularStrength = 0f)
        {
            _texture = new Texture(textureFile);
            _root = new ModelPart(name, new Mesh(ObjLoader.Load(objFile), name))
            {
                Position = position,
                Rotation = Quaternion.FromAxisAngle(Vector3.UnitY, MathHelper.DegreesToRadians(yawDegrees))
            };
            _specularStrength = specularStrength;
        }
        
        #region IRenderable Functions
        /// <summary>
        /// Draws the static prop. The shader is already bound, with the view, projection and lighting set.
        /// </summary>
        /// <param name="shader">The shader program to use for rendering.</param>
        public void Render(ShaderProgram shader)
        {
            // Material: matte by default (no highlight)
            shader.SetFloat("specularStrength", _specularStrength);
            shader.SetFloat("shininess", 32f);
            
            _texture.Bind(0);
            _root.Render(shader, Matrix4.Identity);
        }

        /// <summary>
        /// Frees the static prop's GPU resources (meshes and textures).
        /// </summary>
        public void Delete()
        {
            _root.Mesh?.Delete();
            _texture.Delete();
        }
        #endregion
    }
}