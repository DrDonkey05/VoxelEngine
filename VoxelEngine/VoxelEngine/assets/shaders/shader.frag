#version 330 core

in vec2 fTexCoords;
//in vec3 fColor;
//flat in int fAnimIndex; // Match layout flat constraint input

out vec4 FragColor;

uniform sampler2D uTexture;

// Array of frame location transformations: 
// xy = Current Frame offset bounding minimum, zw = Absolute width/height bounding dimensions scale
//uniform vec4 uAnimationOffsets[256]; 

void main()
{
    vec4 texColor = texture(uTexture, fTexCoords);// * vec4(fColor, 1.0);
    
    if(texColor.a == 0.0)
    {
        discard; 
    }
    
    FragColor = texColor;
}