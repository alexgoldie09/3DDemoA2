#version 330 core

layout(location = 0) in vec3 aPos; // Vertex position
layout(location = 1) in vec2 aTexCoord; // Vertex texture coordinate
layout(location = 2) in vec3 aColor; // Vertex color

out vec2 TexCoord; // Output texture coordinate to the fragment shader
out vec3 VertexColor; // Output color to the fragment shader

// uniform variables
uniform mat4 model; // Model matrix
uniform mat4 view; // View matrix
uniform mat4 projection; // Projection matrix

void main()
{
    // Transform the vertex position by the model, view, and projection matrices
    // Order of multiplication is important because matrix multiplication is not commutative
    gl_Position = vec4(aPos, 1.0) * model * view * projection;
    // Pass the texture coordinate to the fragment shader
    TexCoord = aTexCoord;
    // Pass the vertex color to the fragment shader
    VertexColor = aColor;
}