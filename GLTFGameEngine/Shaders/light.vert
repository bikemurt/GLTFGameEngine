#version 330 core

layout(location = 0) in vec3 position;
layout(location = 1) in vec2 uvCoord;
layout(location = 2) in vec3 normal;
layout(location = 3) in vec4 tangent;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

void main(void)
{
    gl_Position = vec4(position, 1.0) * model * view * projection;
}