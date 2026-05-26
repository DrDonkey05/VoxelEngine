#version 330 core

in vec2 fTexCoords;
in vec3 fTexBounds; // X=MinU, Y=MinV, Z=FrameWidthUV
flat in int fPackedAnimData;
in vec3 fColor;

out vec4 FragColor;

uniform sampler2D uTexture;
uniform int uGlobalFrameTicker; // Pass this uniform variable from your game update tick loop!

void main()
{
    int totalFrames = fPackedAnimData & 0xFFFF;
    int delay = (fPackedAnimData >> 16) & 0xFFFF;

    int frameOffset = (uGlobalFrameTicker / delay) % totalFrames;

    float localU = fract(fTexCoords.x);
    float localV = 1.0 - fract(fTexCoords.y);

    float finalU = fTexBounds.x + (fTexBounds.z * float(frameOffset)) + (localU * fTexBounds.z);
    
    float finalV = fTexBounds.y + (localV * fTexBounds.z);

    vec4 texColor = texture(uTexture, vec2(finalU, finalV)) * vec4(fColor, 1.0);
    
    if(texColor.a == 0.0)
    {
        discard; 
    }
    
    FragColor = texColor;
}