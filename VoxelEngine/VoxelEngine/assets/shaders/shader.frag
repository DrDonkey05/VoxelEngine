#version 330 core

in vec2 fTexCoords;
in vec3 fColor;

out vec4 FragColor;

uniform sampler2D uTexture;

void main()
{
    vec4 texColor = texture(uTexture, fTexCoords) * vec4(fColor, 1.0);
    
    if(texColor.a == 0.0)
    {
        discard; 
    }
    
    FragColor = texColor;
}