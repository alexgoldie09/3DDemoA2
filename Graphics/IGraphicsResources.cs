namespace _3DDemoA2.Graphics
{
    /// <summary>
    /// Defines a common interface for graphics resources (VBO, VAO, IBO, Texture, or ShaderProgram) in OpenGL.
    /// </summary>
    public interface IGraphicsResources
    {
        /// <summary>
        /// Gets the ID of the graphics resource.
        /// </summary>
        public int ID { get; }

        /// <summary>
        /// Binds the graphics resource to the current OpenGL context.
        /// </summary>
        public void Bind();

        /// <summary>
        /// Unbinds the graphics resource from the current OpenGL context.
        /// </summary>
        public void Unbind();

        /// <summary>
        /// Deletes the graphics resource from the GPU.
        /// </summary>
        public void Delete();
    }
}