using _3DDemoA2.Graphics;
using OpenTK.Mathematics;

namespace _3DDemoA2.Scene
{
    /// <summary>
    /// Represents a part of a 3D model, which can have its own geometry, position, rotation, scale, and child parts.
    /// </summary>
    internal class ModelPart
    {
        #region Public Fields
        /// <summary>
        /// The name of the model part, matching its object name in the file.
        /// </summary>
        public string Name { get; private set; }
        
        /// <summary>
        /// The geometry to draw, already centred on this part's pivot. Null for an empty node like a model's root.
        /// </summary>
        public Mesh? Mesh { get; private set; }
        
        /// <summary>
        /// The position of the model part's pivot relative to its parent's pivot, in local space.
        /// </summary>
        public Vector3 Position { get; set; } = Vector3.Zero;
        
        /// <summary>
        /// The rotation of the model part's pivot relative to its parent's pivot, in local space.
        /// A quaternion, so rotations combine through the hierarchy without gimbal lock.
        /// </summary>
        public Quaternion Rotation { get; set; } = Quaternion.Identity;
        
        /// <summary>
        /// Scale on each axis, relative to its parent.
        /// </summary>
        public Vector3 Scale { get; set; } = Vector3.One;
        
        /// <summary>
        /// The model parts that are children of this part, forming a hierarchy.
        /// </summary>
        public List<ModelPart> Children { get; } = new();
        #endregion

        /// <summary>
        /// Constructor that creates a part with an optional mesh.
        /// </summary>
        /// <param name="name">The part's name.</param>
        /// <param name="mesh">The part's geometry, or null for an empty node.</param>
        public ModelPart(string name, Mesh? mesh = null)
        {
            Name = name;
            Mesh = mesh;
        }
        
        #region Public API
        /// <summary>
        /// Adds a child part to this part, forming a hierarchy.
        /// </summary>
        /// <param name="child">The child part to add.</param>
        /// <returns>The added child part.</returns>
        public ModelPart AddChild(ModelPart child)
        {
            Children.Add(child);
            return child;
        }
        
        /// <summary>
        /// This part's transform relative to its parent: scale, then rotate around the pivot, then move into place.
        /// (Row-vector convention, so the matrices read left to right in the order they are applied.)
        /// </summary>
        public Matrix4 LocalMatrix => 
            Matrix4.CreateScale(Scale) * Matrix4.CreateFromQuaternion(Rotation) * Matrix4.CreateTranslation(Position);
        
        /// <summary>
        /// Draws this part and then all of its children.
        /// A child's world matrix is its local matrix followed by its parent's world matrix,
        /// which is what keeps every part attached however the parents move.
        /// </summary>
        /// <param name="shader">The bound model shader.</param>
        /// <param name="parentWorld">The parent's world matrix (Identity for a root part).</param>
        public void Render(ShaderProgram shader, Matrix4 parentWorld)
        {
            Matrix4 world = LocalMatrix * parentWorld;

            if (Mesh != null)
            {
                shader.SetMatrix4("model", world);
                shader.SetMatrix3("normalMatrix", CreateNormalMatrix(world));
                Mesh.Draw();
            }

            foreach (var child in Children)
                child.Render(shader, world);
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// Builds the normal matrix for a model matrix: the inverse-transpose of its upper 3x3.
        /// Normals must use this instead of the model matrix itself, otherwise non-uniform scaling
        /// would tilt them so they are no longer perpendicular to the surface.
        /// The translation row is dropped because normals are directions, not points.
        /// </summary>
        private static Matrix3 CreateNormalMatrix(Matrix4 model)
            => new Matrix3(Matrix4.Transpose(Matrix4.Invert(model)));
        #endregion

    }
}