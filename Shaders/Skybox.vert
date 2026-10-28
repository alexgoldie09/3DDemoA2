#version 330 core

// Vertex attribute (location matches POSITIONLOCATION in Skybox.cs)
layout(location = 0) in vec3 aPos; // Corner of the cube, centred on the origin

out vec3 TexCoords; // Direction from the cube's centre: a cube map's texture coordinate is a direction

uniform mat4 view; // View matrix (world to camera); its translation is ignored, see below
uniform mat4 projection; // Projection matrix (camera to clip space)

void main()
{
    // The cube is centred on the origin, so each corner's position is also the direction to it from the centre
    TexCoords = aPos;

    // Row-vector convention (matrices are uploaded with transpose = true), so vectors go on the left.
    // w = 0 treats the corner as a direction: the view matrix's translation (its bottom row) is multiplied by w,
    // so it drops out. The sky turns with the camera but never moves closer, however far the camera travels.
    vec4 pos = vec4(aPos, 0.0) * view * projection;

    // Depth is z / w after the perspective divide, so z = w puts every sky pixel at exactly 1.0, the far plane:
    // behind everything in the scene (drawn with DepthFunc Lequal, see Skybox.Render)
    gl_Position = pos.xyww;
}
