#version 330

out vec4 outputColor;

uniform vec3 lightColor;

void main()
{
    outputColor = vec4(lightColor, 1.0);
}