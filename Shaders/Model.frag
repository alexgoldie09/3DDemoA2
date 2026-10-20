#version 330 core

in vec2 TexCoord; // Texture coordinate from the vertex shader
in vec3 WorldNormal; // World-space normal from the vertex shader
in vec3 WorldPos; // World-space position (used for lighting / reflections in a later phase)

out vec4 FragColor; // Output colour of the fragment

uniform sampler2D Texture0; // The model's texture (bound to texture unit 0)
uniform int DebugView; // 0 = texture, 1 = normals as colour, 2 = UVs as colour, 3 = wireframe
uniform int UseLighting; // 1 = lit (diffuse + ambient + specular), 0 = raw texture colour
uniform vec3 lightDir;  // direction the light travels, in world space (sunlight pointing down)
uniform vec3 lightColor; // colour/intensity of the light
uniform float ambientStrength;  // minimum brightness, so faces facing away aren't pitch black
uniform vec3 cameraPos; // for specular highlights
uniform float specularStrength; // 0 for matte things (grass/dirt), higher for metal (the bronze barrel later)
uniform float shininess; // highlight tightness, e.g. 32

void main()
{
    if (DebugView == 1)
    // Normals: Map each normal component from -1..1 to 0..1: +X = red, +Y (up) = green, +Z = blue
    FragColor = vec4(normalize(WorldNormal) * 0.5 + 0.5, 1.0);
    else if (DebugView == 2)
    // UVs: U = red, V = green: black at (0, 0), yellow at (1, 1)
    FragColor = vec4(TexCoord, 0.0, 1.0);
    else if (DebugView == 3)
    // Wireframe: Game switches to line rasterisation, so only edges reach here; draw them in one solid colour
    FragColor = vec4(1.0, 1.0, 0.0, 1.0);
    else if (UseLighting == 0)
    // Unlit: the texture exactly as baked
    FragColor = vec4(texture(Texture0, TexCoord).rgb, 1.0);
    else
    {
        vec3 albedo = texture(Texture0, TexCoord).rgb;
        vec3 N = normalize(WorldNormal); // re-normalise: interpolation shortens it
        vec3 L = normalize(-lightDir); // direction from the surface towards the light

        // Lambert diffuse: full brightness facing the light, falling to 0 side-on
        float diffuse = max(dot(N, L), 0.0);

        // Blinn-Phong specular: a highlight where the light reflects towards the camera
        vec3 V = normalize(cameraPos - WorldPos);
        vec3 H = normalize(L + V);
        float specular = pow(max(dot(N, H), 0.0), shininess) * specularStrength;

        // Combine the components: ambient + diffuse + specular, modulated by the light colour and the albedo
        // Metallic surfaces have a coloured specular highlight, so multiply the specular by the albedo too
        vec3 colour = albedo * (ambientStrength + diffuse) * lightColor + specular * lightColor * albedo;        
        FragColor = vec4(colour, 1.0);
    }
}