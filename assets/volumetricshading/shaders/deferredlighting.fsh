#version 330 core

uniform sampler2D gDepth;
uniform sampler2D gNormal;
uniform sampler2D inColor;
uniform sampler2D inGlow;

uniform mat4 invProjectionMatrix;
uniform mat4 invModelViewMatrix;

uniform float dayLight;
uniform vec3 sunPosition;

in vec2 texcoord;
out vec4 outColor;
out vec4 outGlow;

uniform float fogDensityIn;
uniform float fogMinIn;
uniform vec4 rgbaFog;

#include fogandlight.fsh
#include deferredfogandlight.fsh

void main(void)
{
    float projectedZ = texture(gDepth, texcoord).r;
    vec4 normal = texture(gNormal, texcoord);
    vec4 color = texture(inColor, texcoord);
    vec4 glowVec = texture(inGlow, texcoord);

    #if SHADOWQUALITY > 0
    float intensity = 0.34 + (1 - shadowIntensity)/8.0; // this was 0.45, which makes shadow acne visible on blocks
    #else
    float intensity = 0.45;
    #endif
    
    if (projectedZ < 1.0) {
        vec4 screenPosition = vec4(vec3(texcoord, projectedZ) * 2.0 - 1.0, 1.0);
        screenPosition = invProjectionMatrix * screenPosition;
        screenPosition.xyz /= screenPosition.w;
        screenPosition.w = 1.0;
        vec4 worldPosition = invModelViewMatrix * screenPosition;
        vec4 cameraWorldPos = invModelViewMatrix * vec4(0, 0, 0, 1);
        vec4 worldNormal = invModelViewMatrix * vec4(normal.xyz, 0);
        
        float fog = getFogLevelDeferred(length(screenPosition), fogMinIn, fogDensityIn, worldPosition.y);
        color = applyOverexposedFogAndShadowDeferred(worldPosition, color, fog, worldNormal.xyz,
            1, intensity, fogDensityIn, glowVec.b, glowVec.r);
        
        glowVec.y = calculateVolumetricScatterDeferred(worldPosition, cameraWorldPos);
    }
    
    glowVec.z = 0.0;
    outColor = color;
    outGlow = glowVec;
}