#version 330 core

uniform sampler2D inputTexture;
uniform sampler2D glowParts;
uniform vec3 sunPos3dIn;
uniform mat4 invProjectionMatrix;
uniform mat4 invModelViewMatrix;

in vec2 texCoord;
in vec3 sunPosScreen;
in float iGlobalTime;
in float direction;
in vec3 frontColor;
in vec3 backColor;

out vec4 outColor;


#include printvalues.fsh

vec4 applyVolumetricLighting(in vec3 color, in vec2 uv) {
    float vgr = texture(glowParts, uv).g;

    vec3 vgrC = color*1.05*VOLUMETRIC_INTENSITY*vgr;
    return vec4(vgrC, 1.0);
}


void main(void) {
    vec4 proCoord = invProjectionMatrix * vec4(texCoord * 2.0 - 1.0, -1.0, 1);
    proCoord.xyz /= proCoord.w;
    proCoord.w = 0;
    proCoord = invModelViewMatrix * proCoord;
    
    float dp = dot(normalize(sunPos3dIn), normalize(proCoord.xyz));
    vec3 useColor = mix(backColor, frontColor, dp * 0.5 + 0.5);
    outColor = applyVolumetricLighting(useColor, texCoord);
}