#version 330 core

layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec2 aTexCoord;
layout (location = 2) in vec3 aNormal;
layout (location = 3) in vec4 aTangent;
layout (location = 4) in vec4 aJoint;
layout (location = 5) in vec4 aWeight;

out vec2 texCoord;
out vec3 normal;
out vec3 worldPos;
out mat3 TBN;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

const int MAX_JOINTS = 50;
uniform mat4 jointMatrix[MAX_JOINTS];

void main(void)
{
    mat4 skinMatrix = aWeight.x * jointMatrix[int(aJoint.x)] +
        aWeight.y * jointMatrix[int(aJoint.y)] +
        aWeight.z * jointMatrix[int(aJoint.z)] +
        aWeight.w * jointMatrix[int(aJoint.w)];
    
    vec4 worldPos4 = vec4(aPosition, 1.0) * model * skinMatrix;
    worldPos = vec3(worldPos4);

    texCoord = aTexCoord;
    normal = aNormal;

    vec3 T = normalize(vec3(vec4(aTangent.xyz, 0.0) * model));
    vec3 N = normalize(vec3(vec4(aNormal, 0.0) * model));

    // re-orthogonoalize T with respect to N
    T = normalize(T - dot(T, N) * N);

    // then retrieve perpendicular vector B with cross of T and N
    vec3 B = cross(N, T) * aTangent.w;

    TBN = mat3(T, B, N);
    
    gl_Position = worldPos4 * view * projection;
}