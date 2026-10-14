#version 330 core

in vec2 TexCoord; // Texture coordinates passed from the vertex shader
in vec3 VertexColor; // Vertex color passed from the vertex shader

out vec4 FragColor; // Output color of the fragment

uniform sampler2D Texture0; // Texture sampler for the first texture
uniform bool UseVertexColor; // Flag to determine whether to use vertex color or texture color

void main()
{
    // If UseVertexColor is true, use the vertex color; otherwise, sample the texture color
    FragColor = UseVertexColor ? vec4(VertexColor, 1.0) : texture(Texture0, TexCoord);
}