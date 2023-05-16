#version 330

out vec4 outputColor;

in vec2 texCoord;
in vec3 normal;
in vec3 worldPos;
in mat3 TBN;

uniform sampler2D albedoMap;
uniform sampler2D metallicRoughnessMap;
uniform sampler2D normalMap;

uniform int pointLightSize;
uniform vec3 pointLightPositions[10];
uniform vec3 pointLightColors[10];

uniform vec3 camPos;

const float PI = 3.14159265359;

vec3 getNormalFromMap()
{
    // normal map's rgb components are actually xyz spatial components on [0,1]
    vec3 tangentNormal = texture(normalMap, texCoord).xyz * 2.0 - 1.0;

    return normalize(TBN * tangentNormal);
}

float distributionGGX(vec3 N, vec3 H, float roughness)
{
    float a = roughness * roughness;
    float a2 = a * a;
    float NdotH = max(dot(N, H), 0.0);
    float NdotH2 = NdotH * NdotH;

    float bottom = (NdotH2 * (a2 - 1.0) + 1.0);

    return a2 / (PI * bottom * bottom);
}

float geometrySchlickGGX(float NdotV, float roughness)
{
    float r = (roughness + 1.0);
    float k = (r * r) / 8.0;

    return NdotV / (NdotV * (1.0 - k) + k);
}

float geometrySmith(vec3 N, vec3 V, vec3 L, float roughness)
{
    float NdotV = max(dot(N, V), 0.0);
    float NdotL = max(dot(N, L), 0.0);
    float ggx2 = geometrySchlickGGX(NdotV, roughness);
    float ggx1 = geometrySchlickGGX(NdotL, roughness);

    return ggx1 * ggx2;
}

vec3 fresnelSchlick(float cosTheta, vec3 F0)
{
    return F0 + (1.0 - F0) * pow(clamp(1.0 - cosTheta, 0.0, 1.0), 5.0);
}

void main()
{
    vec3 albedo = texture(albedoMap, texCoord).rgb;
    albedo = pow(albedo.rgb, vec3(2.2));

    float metallic = texture(metallicRoughnessMap, texCoord).r;
    float roughness = texture(metallicRoughnessMap, texCoord).g;

    vec3 N = getNormalFromMap();
    vec3 V = normalize(camPos - worldPos);

    vec3 F0 = mix(vec3(0.4), albedo, metallic);

    vec3 Lo = vec3(0.0);
    for (int i = 0; i < pointLightSize; i++)
    {
        vec3 LP = pointLightPositions[i];
        vec3 L = normalize(LP - worldPos);

        vec3 H = normalize(V + L);
        float distance = length(LP - worldPos);
        float attenuation = 1.0 / (distance * distance);

        vec3 lightColor = pointLightColors[i];
        vec3 radiance = lightColor * attenuation;

        float NDF = distributionGGX(N, H, roughness);
        float G = geometrySmith(N, V, L, roughness);
        vec3 F = fresnelSchlick(max(dot(H ,V), 0.0), F0);

        vec3 numerator = NDF * G * F;
        float denominator = 4.0 * max(dot(N, V), 0.0) * max(dot(N, L), 0.0) + 0.0001;
        vec3 specular = numerator / denominator;

        vec3 kS = F;
        vec3 kD = vec3(1.0) - kS;
        kD *= 1.0 - metallic;

        float NdotL = max(dot(N, L), 0.0);

        Lo += (kD * albedo / PI + specular) * radiance * NdotL;
    }
    // ambient lighting - there's more we can do here
    vec3 ambient = vec3(0.5) * albedo;

    vec3 result = ambient + Lo;

    // HDR tonemapping - can do more research on this part
    result = result / (result + vec3(1.0));

    //outputColor = vec4(3*albedo, 1.0);
    outputColor = vec4(result, 1.0);
}