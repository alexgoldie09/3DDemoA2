namespace _3DDemoA2.Utilities
{
    /// <summary>
    /// Single place that knows where asset files live on disk.
    /// The .csproj copies the Shaders, Textures and Models folders next to the executable on every build,
    /// so paths are built from the executable's folder (AppContext.BaseDirectory) rather than the
    /// current working directory. This works from the IDE, `dotnet run`, and the compiled .exe in bin/.
    /// </summary>
    internal static class AssetPaths
    {
        /// <summary>
        /// The folder the executable runs from, e.g. bin/Debug/net10.0/.
        /// </summary>
        private static readonly string Root = AppContext.BaseDirectory;

        /// <summary>
        /// Full path to a file in the Shaders folder, e.g. Shader("Default.vert").
        /// </summary>
        public static string Shader(string fileName) => Path.Combine(Root, "Shaders", fileName);

        /// <summary>
        /// Full path to a file in the Textures folder. Sub-folders are allowed, e.g. Texture("Skybox/right.png").
        /// </summary>
        public static string Texture(string fileName) => Path.Combine(Root, "Textures", fileName);

        /// <summary>
        /// Full path to a file in the Models folder, e.g. Model("Ground.obj").
        /// </summary>
        public static string Model(string fileName) => Path.Combine(Root, "Models", fileName);
    }
}