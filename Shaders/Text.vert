#version 330 core

layout(location = 0) in vec2 aPos;      // Screen position in pixels
layout(location = 1) in vec2 aTexCoord; // Position in the font atlas
layout(location = 2) in vec4 aColor;    // Text colour

out vec2 TexCoord;
out vec4 TextColor;

uniform mat4 projection; // Orthographic: pixels to clip space

void main()
{
    // Same row-vector convention as Default.vert (matrices uploaded with transpose = true)
    gl_Position = vec4(aPos, 0.0, 1.0) * projection;
    TexCoord = aTexCoord;
    TextColor = aColor;
}
