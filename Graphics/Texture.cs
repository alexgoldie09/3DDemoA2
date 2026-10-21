using _3DDemoA2.Debugging;
using _3DDemoA2.Utilities;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using StbImageSharp;

namespace _3DDemoA2.Graphics
{
    /// <summary>
    /// Represents a 2D texture in OpenGL, allowing for loading, binding, and managing texture resources.
    /// </summary>
    internal class Texture : IGraphicsResources
    {
        /// <summary>
        /// Gets the ID of the texture object in OpenGL.
        /// </summary>
        public int ID { get; set; }
        
        /// <summary>
        /// Constructor that initialises a new instance of the Texture class and loads a texture from the Textures folder.
        /// </summary>
        /// <param name="filename">The texture file, relative to the Textures folder.</param>
        /// <param name="pixelPerfect">
        /// True for pixel art such as the debug font: nearest filtering, no mipmaps, so pixels stay crisp.
        /// False (default) for model textures: trilinear filtering with mipmaps, so the texture stays smooth
        /// up close and doesn't shimmer when seen at a distance or a shallow angle.
        /// </param>
        public Texture(string filename, bool pixelPerfect = false)
        {
            if (string.IsNullOrEmpty(filename))
            {
                DebugText.Log("Texture filename is null or empty.", Color4.Red, 6f);
                return;
            }
            
            // Uploads always happen on unit 0, so creating a texture never disturbs
            // whatever is bound to the other units (e.g. a cube map on unit 1)
            GL.ActiveTexture(TextureUnit.Texture0);
            
            // Generate a new texture ID and bind it to the OpenGL context
            ID = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, ID);
            
            // Wrapping: repeat, so UVs outside 0..1 tile the texture
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

            // Filtering: nearest for pixel art, trilinear (blends between mipmap levels) for everything else
            var minFilter = pixelPerfect ? TextureMinFilter.Nearest : TextureMinFilter.LinearMipmapLinear;
            var magFilter = pixelPerfect ? TextureMagFilter.Nearest : TextureMagFilter.Linear;
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)minFilter);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)magFilter);
            
            // Image files store the top row first, but OpenGL expects the bottom row first, so flip on load.
            // This flag is global in StbImageSharp, so it is set explicitly every time.
            StbImage.stbi_set_flip_vertically_on_load(1);

            // Use a try-catch block to handle potential exceptions during file reading and image loading
            try
            {
                using var stream = File.OpenRead(AssetPaths.Texture(filename));
                ImageResult image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

                if (image == null)
                {
                    DebugText.Log($"Failed to load texture '{filename}': ImageResult is null.", Color4.Red, 6f);
                }
                else
                {
                    // Upload the image data to the GPU
                    GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, image.Width, image.Height, 0,
                        PixelFormat.Rgba, PixelType.UnsignedByte, image.Data);

                    // Build the smaller mipmap levels from the uploaded image (required by LinearMipmapLinear,
                    // otherwise the texture is "incomplete" and samples as black)
                    if (!pixelPerfect)
                        GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

                    DebugText.Log($"Loaded '{filename}' ({image.Width}x{image.Height})", Color4.LightGreen, 6f);
                }
            }
            catch (Exception e)
            {
                DebugText.Log($"Texture '{filename}' failed: {e.Message}", Color4.Red, 6f);
            }

            // Unbind the texture to prevent accidental modification
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }
        
        #region IGraphicsResources Functions
        /// <summary>
        /// Binds the texture to a texture unit. The shader's sampler uniform must be set to the same unit number
        /// (e.g. shader.SetInt("Texture0", 0)). Separate units let a shader sample several textures at once,
        /// such as a model texture on unit 0 and the skybox cube map on unit 1.
        /// </summary>
        /// <param name="unit">The texture unit to bind to (0, 1, 2...).</param>
        public void Bind(int unit = 0)
        {
            GL.ActiveTexture(TextureUnit.Texture0 + unit);
            GL.BindTexture(TextureTarget.Texture2D, ID);
        }

        /// <summary>
        /// Unbinds any 2D texture from the given texture unit.
        /// </summary>
        /// <param name="unit">The texture unit to clear.</param>
        public void Unbind(int unit = 0)
        {
            GL.ActiveTexture(TextureUnit.Texture0 + unit);
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        /// <summary>
        /// Deletes the texture from the GPU, freeing up resources.
        /// </summary>
        public void Delete() => GL.DeleteTexture(ID);
        #endregion
    }
}