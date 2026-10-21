#version 330 core

in vec2 TexCoord; // Texture coordinate from the vertex shader
in vec3 WorldNormal; // World-space normal from the vertex shader
in vec3 WorldPos; // World-space position (for the view direction)
in vec3 WorldTangent; // World-space tangent (normal mapping)
in vec3 WorldBitangent; // World-space bitangent (normal mapping)

out vec4 FragColor; // Output colour of the fragment

// Textures (sampler units are set once in Game; see Material.cs)
uniform sampler2D Texture0; // The model's colour texture (unit 0)
uniform sampler2D NormalMap; // Tangent-space normal map, OpenGL convention (unit 1)
uniform sampler2D MetallicMap; // Greyscale, white = metal (unit 2)
uniform sampler2D RoughnessMap; // Greyscale, white = rough (unit 3)

// Material (set per mesh by Material.Apply)
uniform int useNormalMap; // 1 = bend the normal with the normal map
uniform int useMetallicMap; // 1 = multiply metallicFactor by the metallic map
uniform int useRoughnessMap; // 1 = multiply roughnessFactor by the roughness map
uniform float metallicFactor; // 0 = non-metal, 1 = metal
uniform float roughnessFactor; // 0 = mirror smooth, 1 = fully matte
uniform float normalStrength; // 0 = flat, 1 = normal map as authored
uniform float detailScale; // how many times the maps repeat across the UVs

// Debug and lighting
uniform int DebugView; // 0 texture, 1 normals, 2 UVs, 3 wireframe, 4 mapped normals, 5 metallic, 6 roughness
uniform int UseLighting; // 1 = lit, 0 = raw texture colour
uniform vec3 lightDir; // direction the light travels, in world space (sunlight pointing down)
uniform vec3 lightColor; // colour/intensity of the light
uniform float ambientStrength; // minimum brightness, so faces facing away aren't pitch black
uniform vec3 cameraPos; // for specular highlights

void main()
{
    vec3 albedo = texture(Texture0, TexCoord).rgb;
    vec2 detailUV = TexCoord * detailScale;

    // --- Surface normal ---
    vec3 N = normalize(WorldNormal); // re-normalise: interpolation shortens it
    if (useNormalMap == 1)
    {
        // TBN: tangent, bitangent and normal as the axes of the surface. The tangent is straightened to be exactly
        // perpendicular to the normal (Gram-Schmidt), and the bitangent's sign keeps mirrored UVs the right way round.
        vec3 T = normalize(WorldTangent - dot(WorldTangent, N) * N);
        float handedness = dot(cross(N, T), WorldBitangent) < 0.0 ? -1.0 : 1.0;
        vec3 B = cross(N, T) * handedness;

        // The map stores directions as colours (0..1), so unpack to -1..1; scaling X/Y sets how strongly it bends
        vec3 mapped = texture(NormalMap, detailUV).rgb * 2.0 - 1.0;
        mapped.xy *= normalStrength;
        N = normalize(mat3(T, B, N) * mapped);
    }

    // --- Material values: factor x map (or the factor alone when there's no map) ---
    float metallic = metallicFactor * (useMetallicMap == 1 ? texture(MetallicMap, detailUV).r : 1.0);
    float roughness = roughnessFactor * (useRoughnessMap == 1 ? texture(RoughnessMap, detailUV).r : 1.0);

    if (DebugView == 1)
    // Normals: Map each normal component from -1..1 to 0..1: +X = red, +Y (up) = green, +Z = blue
    FragColor = vec4(normalize(WorldNormal) * 0.5 + 0.5, 1.0);
    else if (DebugView == 2)
    // UVs: U = red, V = green: black at (0, 0), yellow at (1, 1)
    FragColor = vec4(TexCoord, 0.0, 1.0);
    else if (DebugView == 3)
    // Wireframe: Game switches to line rasterisation, so only edges reach here; draw them in one solid colour
    FragColor = vec4(1.0, 1.0, 0.0, 1.0);
    else if (DebugView == 4)
    // Mapped normals: the same colours as view 1, but after the normal map has bent them
    FragColor = vec4(N * 0.5 + 0.5, 1.0);
    else if (DebugView == 5)
    // Metallic: white = metal, black = non-metal
    FragColor = vec4(vec3(metallic), 1.0);
    else if (DebugView == 6)
    // Roughness: white = rough, black = smooth
    FragColor = vec4(vec3(roughness), 1.0);
    else if (UseLighting == 0)
    // Unlit: the texture exactly as baked
    FragColor = vec4(albedo, 1.0);
    else
    {
        vec3 L = normalize(-lightDir); // direction from the surface towards the light

        // Lambert diffuse: full brightness facing the light, falling to 0 side-on
        float diffuse = max(dot(N, L), 0.0);

        // Roughness drives the highlight: smooth = small and bright, rough = broad and faint, fully rough = none.
        // A simple remap that keeps shininess in the 8..256 range that suits these flat-shaded models.
        float smoothness = 1.0 - clamp(roughness, 0.0, 1.0);
        float shininess = mix(8.0, 256.0, smoothness * smoothness);
        float specularStrength = smoothness;

        // Blinn-Phong specular: a highlight where the light reflects towards the camera
        vec3 V = normalize(cameraPos - WorldPos);
        vec3 H = normalize(L + V);
        float specular = pow(max(dot(N, H), 0.0), shininess) * specularStrength;

        // Metallic: metal reflects light in its own colour (tinted highlight) and has little diffuse light,
        // while non-metals keep a white highlight. 40% of the diffuse is kept on metal for now, because
        // without reflections of the surroundings a fully metal surface looks black.
        vec3 specularColour = mix(vec3(1.0), albedo, metallic);
        float diffuseAmount = 1.0 - 0.6 * metallic;

        vec3 colour = albedo * diffuseAmount * (ambientStrength + diffuse) * lightColor
        + specular * specularColour * lightColor;
        FragColor = vec4(colour, 1.0);
    }
}
