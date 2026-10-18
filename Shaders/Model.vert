#version 330 core

// Vertex attributes (locations match the constants in Mesh.cs)
layout(location = 0) in vec3 aPos; // Vertex position (model space)
layout(location = 1) in vec2 aTexCoord; // Texture coordinate
layout(location = 2) in vec3 aNormal; // Surface normal (model space)

out vec2 TexCoord; // Texture coordinate for the fragment shader
out vec3 WorldNormal; // Normal in world space (for lighting / reflections)
out vec3 WorldPos; // Position in world space (for lighting / reflections)

uniform mat4 model; // Model matrix (object -> world)
uniform mat4 view; // View matrix (world -> camera)
uniform mat4 projection; // Projection matrix (camera -> clip space)
uniform mat3 normalMatrix; // Inverse-transpose of the model matrix, so normals stay perpendicular under non-uniform scale

void main()
{
    // Row-vector convention (matrices are uploaded with transpose = true), so vectors go on the left
    vec4 worldPos = vec4(aPos, 1.0) * model;

    WorldPos = worldPos.xyz;
    WorldNormal = aNormal * normalMatrix;
    TexCoord = aTexCoord;

    gl_Position = worldPos * view * projection;
}