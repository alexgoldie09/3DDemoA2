#version 330 core

in vec2 TexCoord; // Texture coordinate from the vertex shader
in vec3 WorldNormal; // World-space normal from the vertex shader
in vec3 WorldPos; // World-space position (for the view direction)
in vec3 WorldTangent; // World-space tangent (normal mapping)
in vec3 WorldBitangent; // World-space bitangent (normal mapping)

out vec4 FragColor; // Output colour of the fragment

// Material maps (sampler units are set once in Game; see Material.cs)
uniform sampler2D AlbedoMap; // Surface colour, laid out to the model's UVs (unit 0)
uniform sampler2D NormalMap; // Tangent-space normal map, OpenGL convention (unit 1)
uniform sampler2D MetallicMap; // Greyscale, white = metal (unit 2)
uniform sampler2D RoughnessMap; // Greyscale, white = rough (unit 3)

// Material values (set per mesh by Material.Apply): each multiplies its map.
// A material without a map gets a neutral default instead (white, or a flat normal), so the value is used alone.
uniform vec3 albedoFactor; // colour tint (white = map unchanged)
uniform float metallicFactor; // 0 = non-metal, 1 = metal
uniform float roughnessFactor; // 0 = mirror smooth, 1 = fully matte
uniform float normalStrength; // 0 = flat, 1 = normal map as authored
uniform float detailScale; // how many times the detail maps repeat across the UVs

// Debug and lighting
uniform int DebugView; // one of the DEBUGVIEW_ values, #defined from the C# DebugView enum (see DebugView.cs)
uniform int UseLighting; // 1 = lit, 0 = raw texture colour
uniform vec3 lightDir; // direction the light travels, in world space (sunlight pointing down)
uniform vec3 lightColor; // colour/intensity of the light
uniform float ambientStrength; // minimum brightness, so faces facing away aren't pitch black
uniform vec3 cameraPos; // for specular highlights and reflections

// Environment (what reflective surfaces mirror): sky above, horizon in the middle, ground below
uniform vec3 skyColour;
uniform vec3 horizonColour;
uniform vec3 groundColour;

// Looks up the colour of the surroundings in a given direction.
// A stand-in for the skybox: once the skybox cube map exists, this becomes texture(skybox, direction).
vec3 SampleEnvironment(vec3 direction)
{
    float up = direction.y;
    if (up >= 0.0)
    return mix(horizonColour, skyColour, smoothstep(0.0, 0.5, up)); // horizon fades into sky
    return mix(horizonColour, groundColour, smoothstep(0.0, 0.2, -up)); // horizon fades quickly into ground
}

void main()
{
    // --- Material values: factor x map ---
    // The albedo uses the model's own UVs; the tiling detail maps repeat detailScale times across them
    vec3 albedo = albedoFactor * texture(AlbedoMap, TexCoord).rgb;
    vec2 detailUV = TexCoord * detailScale;
    float metallic = metallicFactor * texture(MetallicMap, detailUV).r;
    float roughness = roughnessFactor * texture(RoughnessMap, detailUV).r;

    // --- Surface normal ---
    // Without a normal map, the flat default unpacks to about (0, 0, 1), which leaves the mesh normal unchanged
    vec3 N = normalize(WorldNormal); // re-normalise: interpolation shortens it
    // TBN: tangent, bitangent and normal as the axes of the surface. The tangent is straightened to be exactly
    // perpendicular to the normal (Gram-Schmidt), and the bitangent's sign keeps mirrored UVs the right way round.
    vec3 T = normalize(WorldTangent - dot(WorldTangent, N) * N);
    float handedness = dot(cross(N, T), WorldBitangent) < 0.0 ? -1.0 : 1.0;
    vec3 B = cross(N, T) * handedness;

    // The map stores directions as colours (0..1), so unpack to -1..1; scaling X/Y sets how strongly it bends
    vec3 mapped = texture(NormalMap, detailUV).rgb * 2.0 - 1.0;
    mapped.xy *= normalStrength;
    N = normalize(mat3(T, B, N) * mapped);

    if (DebugView == DEBUGVIEW_NORMALS)
    // Normals: Map each normal component from -1..1 to 0..1: +X = red, +Y (up) = green, +Z = blue
    FragColor = vec4(normalize(WorldNormal) * 0.5 + 0.5, 1.0);
    else if (DebugView == DEBUGVIEW_UVS)
    // UVs: U = red, V = green: black at (0, 0), yellow at (1, 1)
    FragColor = vec4(TexCoord, 0.0, 1.0);
    else if (DebugView == DEBUGVIEW_WIREFRAME)
    // Wireframe: Game switches to line rasterisation, so only edges reach here; draw them in one solid colour
    FragColor = vec4(1.0, 1.0, 0.0, 1.0);
    else if (DebugView == DEBUGVIEW_METALLIC)
    // Metallic: white = metal, black = non-metal
    FragColor = vec4(vec3(metallic), 1.0);
    else if (DebugView == DEBUGVIEW_ROUGHNESS)
    // Roughness: white = rough, black = smooth
    FragColor = vec4(vec3(roughness), 1.0);
    else if (UseLighting == 0)
    // Unlit: the colour exactly as textured
    FragColor = vec4(albedo, 1.0);
    else
    {
        vec3 L = normalize(-lightDir); // direction from the surface towards the light
        vec3 V = normalize(cameraPos - WorldPos); // direction from the surface towards the camera

        // Lambert diffuse: full brightness facing the light, falling to 0 side-on
        float diffuse = max(dot(N, L), 0.0);

        // Roughness drives the highlight: smooth = small and bright, rough = broad and faint, fully rough = none.
        // A simple remap that keeps shininess in the 8..256 range that suits these flat-shaded models.
        float smoothness = 1.0 - clamp(roughness, 0.0, 1.0);
        float shininess = mix(8.0, 256.0, smoothness * smoothness);
        float specularStrength = smoothness;

        // Blinn-Phong specular: a highlight where the light reflects towards the camera
        vec3 H = normalize(L + V);
        float specular = pow(max(dot(N, H), 0.0), shininess) * specularStrength;

        // Metals reflect light in their own colour; non-metals keep a white highlight
        vec3 specularColour = mix(vec3(1.0), albedo, metallic);

        // Environment reflection: the camera's view ray bounced off the surface, looked up in the surroundings.
        // Rough surfaces scatter the reflection, approximated by bending the ray towards the normal (a softer look-up),
        // and reflect less of it. Only metals get it: for non-metals, reflections are faint enough to leave out.
        vec3 R = reflect(-V, N);
        vec3 reflection = SampleEnvironment(normalize(mix(R, N, roughness)));
        vec3 environment = reflection * specularColour * metallic * mix(0.3, 1.0, smoothness);

        // Metal's colour comes mostly from what it reflects, so its diffuse is turned down (20% kept, so its
        // shape still reads in direct sunlight)
        float diffuseAmount = 1.0 - 0.8 * metallic;

        vec3 colour = albedo * diffuseAmount * (ambientStrength + diffuse) * lightColor
        + specular * specularColour * lightColor
        + environment;
        FragColor = vec4(colour, 1.0);
    }
}
