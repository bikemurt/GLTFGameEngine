#version 330 core

layout(location = 0) in vec3 position;
layout(location = 1) in vec2 uvCoord;
layout(location = 2) in vec3 normal;
layout(location = 3) in vec4 tangent;

out vec2 texCoord;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

void main(void)
{
    texCoord = uvCoord;
    gl_Position = projection * view * model * vec4(position, 1.0);
}