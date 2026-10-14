#version 330 core

in vec2 TexCoord;
in vec4 TextColor;

out vec4 FragColor;

uniform sampler2D FontAtlas;

void main()
{
    // The atlas is white glyphs on transparent, so only its alpha matters
    float glyph = texture(FontAtlas, TexCoord).a;

    // Pixel font: a pixel is either part of the glyph or not
    if (glyph < 0.5)
        discard;

    FragColor = TextColor;
}
