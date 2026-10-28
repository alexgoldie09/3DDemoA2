using _3DDemoA2.Debugging;
using _3DDemoA2.Utilities;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using StbImageSharp;

namespace _3DDemoA2.Graphics
{
    /// <summary>
    /// Represents a cube map texture in OpenGL: six square images, one per face of a cube,
    /// sampled with a direction instead of UVs. Used for the skybox, and by the model shader for reflections.
    /// </summary>
    internal class CubeMap : IGraphicsResources
    {
        /// <summary>
        /// Gets the ID of the cube map texture object in OpenGL.
        /// </summary>
        public int ID { get; }

        /// <summary>
        /// The texture unit this cube map always binds to (e.g. Skybox.CUBEMAPUNIT).
        /// </summary>
        public int Unit { get; private set; }

        #region Constructors
        /// <summary>
        /// Constructor that loads six face images from the Textures folder into one cube map.
        /// Every face must be square and the same size as the others, otherwise OpenGL treats the cube map
        /// as incomplete and it samples as black.
        /// </summary>
        /// <param name="faceFiles">
        /// Six files in OpenGL's face order: right (+X), left (-X), top (+Y), bottom (-Y), front (+Z), back (-Z).
        /// </param>
        /// <param name="unit">The texture unit this cube map always binds to (e.g. Skybox.CUBEMAPUNIT).</param>
        public CubeMap(string[] faceFiles, int unit)
        {
            Unit = unit;

            if (faceFiles.Length != 6)
            {
                DebugText.Log($"Cube map needs 6 faces but was given {faceFiles.Length}.", Color4.Red, 6f);
                return;
            }

            // Uploads always happen on unit 0, the same as Texture, so creating a cube map never disturbs
            // whatever is bound to the other units
            GL.ActiveTexture(TextureUnit.Texture0);

            // Generate a new texture ID and bind it as a cube map (a different target to a 2D texture)
            ID = GL.GenTexture();
            GL.BindTexture(TextureTarget.TextureCubeMap, ID);

            // Wrapping: clamp to edge on all three axes (S, T and R, the third axis of a direction lookup).
            // A direction that lands exactly on the seam between two faces then reads that face's edge pixel,
            // instead of blending in the opposite edge, which would show as a thin line in the sky
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToEdge);

            // Filtering: linear, no mipmaps (the sky is always seen at about the same size on screen)
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            // Cube map faces are used the way they are stored in the file (top row first), so no flip.
            // This flag is global in StbImageSharp; Texture sets it back to 1 every time it loads.
            StbImage.stbi_set_flip_vertically_on_load(0);

            // The first face's size, which every other face must match
            int faceSize = 0;

            for (int i = 0; i < faceFiles.Length; i++)
            {
                // Use a try-catch block to handle potential exceptions during file reading and image loading
                try
                {
                    using var stream = File.OpenRead(AssetPaths.Texture(faceFiles[i]));
                    ImageResult image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

                    if (image == null)
                    {
                        DebugText.Log($"Failed to load cube face '{faceFiles[i]}': ImageResult is null.", Color4.Red, 6f);
                        continue;
                    }

                    if (i == 0)
                        faceSize = image.Width;

                    if (image.Width != image.Height || image.Width != faceSize)
                        DebugText.Log($"Cube face '{faceFiles[i]}' is {image.Width}x{image.Height}; every face must be " +
                                      $"square and {faceSize}x{faceSize}.", Color4.Red, 6f);

                    // The six face targets come one after another in OpenGL (+X, -X, +Y, -Y, +Z, -Z),
                    // so face i is uploaded to PositiveX + i
                    GL.TexImage2D(TextureTarget.TextureCubeMapPositiveX + i, 0, PixelInternalFormat.Rgba,
                        image.Width, image.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, image.Data);
                }
                catch (Exception e)
                {
                    DebugText.Log($"Cube face '{faceFiles[i]}' failed: {e.Message}", Color4.Red, 6f);
                }
            }

            DebugText.Log($"Loaded cube map ({faceSize}x{faceSize} faces)", Color4.LightGreen, 6f);

            // Unbind the cube map to prevent accidental modification
            GL.BindTexture(TextureTarget.TextureCubeMap, 0);
        }
        #endregion

        #region IGraphicsResources Functions
        /// <summary>
        /// Binds the cube map to the texture unit it is assigned to.
        /// </summary>
        public void Bind()
        {
            GL.ActiveTexture(TextureUnit.Texture0 + Unit);
            GL.BindTexture(TextureTarget.TextureCubeMap, ID);
        }

        /// <summary>
        /// Unbinds any cube map from the texture unit this cube map is assigned to.
        /// </summary>
        public void Unbind()
        {
            GL.ActiveTexture(TextureUnit.Texture0 + Unit);
            GL.BindTexture(TextureTarget.TextureCubeMap, 0);
        }

        /// <summary>
        /// Deletes the cube map from the GPU, freeing up resources.
        /// </summary>
        public void Delete() => GL.DeleteTexture(ID);
        #endregion
    }
}
