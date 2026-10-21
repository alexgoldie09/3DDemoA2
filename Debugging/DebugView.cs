namespace _3DDemoA2.Debugging
{
    /// <summary>
    /// What the model shader outputs to the screen: the normal textured view, or a debug view
    /// for checking normals, UVs, wireframe and the material maps.
    ///
    /// This is the only place the values are set. ShaderProgram writes each one into the shaders
    /// as a #define (e.g. DEBUGVIEW_WIREFRAME 3), so reordering or adding views here updates the shader too.
    /// </summary>
    internal enum DebugView { Texture, Normals, UVs, Wireframe, Metallic, Roughness }
}