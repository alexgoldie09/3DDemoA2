namespace _3DDemoA2.Graphics
{
    /// <summary>
    /// Defines a common interface for graphics resources (VBO, VAO, IBO, Texture, or ShaderProgram) in OpenGL.
    /// </summary>
    public interface IGraphicsResources
    {
        /// <summary>
        /// Gets or sets the ID of the graphics resource.
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// Binds the graphics resource to the current OpenGL context.
        /// </summary>
        /// <param name="unit"></param>
        public void Bind(int unit = 0);

        /// <summary>
        /// Unbinds the graphics resource from the current OpenGL context.
        /// </summary>
        /// <param name="unit"></param>
        public void Unbind(int unit = 0);

        /// <summary>
        /// Deletes the graphics resource from the GPU.
        /// </summary>
        public void Delete();
    }
}