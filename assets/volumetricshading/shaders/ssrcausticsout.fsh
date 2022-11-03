#version 330 core

uniform sampler2D gDepth;
uniform sampler2D gNormal;

uniform mat4 invProjectionMatrix;
uniform mat4 invModelViewMatrix;

uniform float dayLight;
uniform vec3 sunPosition;
uniform vec3 playerPos;

in vec2 texcoord;
out float outStrength;

uniform float fogDensityIn;
uniform float fogMinIn;
uniform vec4 rgbaFog;

#include noise3d.ash
#include fogandlight.fsh
#include wavenoise.ash
#include deferredfogandlight.fsh

void main(void)
{
    float underwater = texture(gNormal, texcoord).w;
    if (underwater > 0.5) discard;
    
    float projectedZ = texture(gDepth, texcoord).r;
    vec4 screenPosition = vec4(vec3(texcoord, projectedZ) * 2.0 - 1.0, 1.0);
    screenPosition = invProjectionMatrix * screenPosition;
    screenPosition.xyz /= screenPosition.w;
    screenPosition.w = 1.0;
    vec4 worldPosition = invModelViewMatrix * screenPosition;
    vec4 cameraWorldPos = invModelViewMatrix * vec4(0, 0, 0, 1);

    vec3 absWorldPos = worldPosition.xyz + playerPos;
    
    float shadowBrightness = getPCFBrightnessAt(worldPosition);
    float shadowStrength = clamp(pow(shadowIntensity, 2.0f), 0.2f, 1.0f);
    //float shadowStrength = 1.0f;

    float waveNoise = generateCausticsNoise(absWorldPos, sunPosition) * 1.3;
    
    float fog = 1.0 - getFogLevelDeferred(length(screenPosition), fogMinIn, fogDensityIn, absWorldPos.y);
    float distance = exp(-length(worldPosition - cameraWorldPos) * 0.008f);
    outStrength = (waveNoise * distance * shadowBrightness * shadowStrength * fog + 0.5) - 0.05 * distance * fog * shadowStrength;
}