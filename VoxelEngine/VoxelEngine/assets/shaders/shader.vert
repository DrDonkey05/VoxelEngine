#version 330 core

layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec3 aTexBounds;       // X=MinU, Y=MinV, Z=FrameWidthUV
layout (location = 2) in int aPackedAnimData;
layout (location = 3) in vec3 aColor;

out vec2 fTexCoords;
out vec3 fTexBounds;
flat out int fPackedAnimData;
out vec3 fColor;

uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProjection;

void main()
{
    gl_Position = uProjection * uView * uModel * vec4(aPosition, 1.0);
    
    fTexBounds = aTexBounds;
    fPackedAnimData = aPackedAnimData;
    fColor = aColor;

    // Reconstruct UV coordinates based on the corner index of the quad.
    // gl_VertexID % 4 gives us 0, 1, 2, or 3 for the current face quad corners.
    int cornerID = gl_VertexID % 4;
    
    // Corner mapping (Standard counter-clockwise or clockwise order):
    // 0 = Bottom-Left  (0.0, 0.0)
    // 1 = Bottom-Right (1.0, 0.0)
    // 2 = Top-Right    (1.0, 1.0)
    // 3 = Top-Left     (0.0, 1.0)
    float u = float((cornerID == 1 || cornerID == 2));
    float v = float((cornerID == 2 || cornerID == 3));
    
    fTexCoords = vec2(u, v);
}