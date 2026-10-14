using _3DDemoA2.Debugging;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using StbImageSharp;

namespace _3DDemoA2.Graphics
{
    /// <summary>
    /// Represents a texture in OpenGL, allowing for loading, binding, and managing texture resources.
    /// </summary>
    internal class Texture
    {
        /// <summary>
        /// Gets the ID of the texture object in OpenGL.
        /// </summary>
        public int ID { get; private set; }
        
        /// <summary>
        /// Constructor that initialises a new instance of the Texture class and loads a texture from the specified file.
        /// </summary>
        /// <param name="filename">The path to the texture file.</param>
        public Texture(string filename)
        {
            if (string.IsNullOrEmpty(filename))
            {
                DebugText.Log("Texture filename is null or empty.", Color4.Red, 6f);
                return;
            }
            
            // Generate a new texture ID and bind it to the OpenGL context
            ID = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, ID);
            
            // Set texture parameters for wrapping and filtering
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

            // Load the image using StbImageSharp
            StbImage.stbi_set_flip_vertically_on_load(1);

            // Use a try-catch block to handle potential exceptions during file reading and image loading
            try
            {
                using var stream = File.OpenRead("../../../Textures/" + filename);
                ImageResult image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
                if (image == null)
                {
                    DebugText.Log($"Failed to load texture '{filename}': ImageResult is null.", Color4.Red, 6f);
                    return;
                }

                // Upload the image data to the GPU
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, image.Width, image.Height, 0,
                    PixelFormat.Rgba, PixelType.UnsignedByte, image.Data);
                
                DebugText.Log($"Loaded '{filename}' ({image.Width}x{image.Height})", Color4.LightGreen, 6f);
            }
            catch (Exception e)
            {
                DebugText.Log($"Texture '{filename}' failed: {e.Message}", Color4.Red, 6f);
            }
            
            // Unbind the texture to prevent accidental modification
            Unbind();
        }
        
        #region Public API
        /// <summary>
        /// Binds the texture for use in OpenGL.
        /// </summary>
        public void Bind() => GL.BindTexture(TextureTarget.Texture2D, ID);
        /// <summary>
        /// Unbinds the texture from the current OpenGL context.
        /// </summary>
        public void Unbind() => GL.BindTexture(TextureTarget.Texture2D, 0);
        /// <summary>
        /// Deletes the texture from the GPU, freeing up resources.
        /// </summary>
        public void Delete() => GL.DeleteTexture(ID);
        #endregion
    }
}