#version 330 core

// Input attributes from our C# 'Vertex' struct layout
layout (location = 0) in vec3 aPosition;
//layout (location = 1) in vec2 aTexCoords;
// layout (location = 2) in vec3 aColor;
// layout (location = 3) in int aAnimIndex;

// Output to the Fragment Shader
//out vec2 fTexCoords;
//out vec3 fColor;
//flat out int fAnimIndex; // Using flat to stop interpolation artifacts across triangles

// Uniform matrices passed from our BlockRenderer
uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProjection;

void main()
{
    // Multiply the matrices from right to left to get the final screen position:
    // Projection * View * Model * LocalPosition
    gl_Position = uProjection * uView * uModel * vec4(aPosition, 1.0);
    
    // Pass the texture coordinates directly down to the fragment shader
    //fTexCoords = aTexCoords;
    //fColor = aColor;
    //fAnimIndex = aAnimIndex;
}