#version 330 core

uniform sampler2D terrainTex;
uniform vec3 playerpos;
uniform mat4 modelViewMatrix;
uniform float dropletIntensity = 0;
uniform float playerUnderwater;
uniform vec4 cameraWorldPosition;
const vec4 rgbaFog = vec4(0);

in vec4 worldPos;
in vec4 fragPosition;
in vec3 fragWorldPos;
in vec4 gnormal;
in vec3 worldNormal;
in vec2 flowVectorf;
in vec2 uv;
flat in int waterFlags;
flat in float alpha;
flat in int skyExposed;

layout(location = 0) out vec4 outGPosition;
layout(location = 1) out vec4 outGNormal;
layout(location = 2) out vec4 outTint;
#if VSMOD_REFRACT > 0
layout(location = 3) out vec4 outRefraction;
#endif

#include colormap.fsh
#include fogandlight.fsh
#include noise3d.ash
#include wavenoise.ash
#generated dropletnoise

void generateNoiseBump(inout vec3 normalMap, vec3 position, float div) {
    const vec3 offset = vec3(0.05, 0.0, 0.0);
    vec3 posCenter = position.xyz;
    vec3 posNorth = posCenter - offset.zyx;
    vec3 posEast = posCenter + offset.xzy;

    float val0 = generateWaveNoise(posCenter, div);
    float val1 = generateWaveNoise(posNorth, div);
    float val2 = generateWaveNoise(posEast, div);

    float zDelta = (val0 - val1);
    float xDelta = (val2 - val0);

    normalMap += vec3(xDelta * 0.5, zDelta * 0.5, 0);
}

void generateNoiseParallax(inout vec3 normalMap, vec3 viewVector, float div, out vec3 parallaxPos) {
    vec3 targetPos = fragWorldPos.xyz;

    float currentNoise = generateWaveNoise(fragWorldPos.xyz, div);
    targetPos.xz += (currentNoise * viewVector.xy) * 0.4;

    generateNoiseBump(normalMap, targetPos, div);
    parallaxPos = targetPos;
}

float generateSplash(vec3 pos)
{
    vec3 localPos = fract(pos.xyz / 512.0) * 512.0;
    vec2 uv = 5.0 * pos.xz;
    
    return dropletnoise(uv, waterWaveCounter);
}

void generateSplashBump(inout vec3 normalMap, vec3 pos)
{
    const vec3 deltaPos = vec3(0.01, 0.0, 0.0);
    vec3 startPos = pos - deltaPos.xyx * 0.5;
    
    float val0 = generateSplash(startPos);
    float val1 = generateSplash(startPos + deltaPos.xyz);
    //float val2 = generateSplash(pos - deltaPos.xyz);
    float val2 = generateSplash(startPos + deltaPos.zyx);
    //float val4 = generateSplash(pos - deltaPos.zyx);

    float xDelta = (val1 - val0);
    float zDelta = (val2 - val0);

    normalMap += vec3(xDelta, zDelta, 0) * 0.75;
}

// https://gamedev.stackexchange.com/questions/86530/is-it-possible-to-calculate-the-tbn-matrix-in-the-fragment-shader
mat3 cotangentFrame(vec3 N, vec3 p, vec2 uv) {
    vec3 dp1 = dFdx(p);
    vec3 dp2 = dFdy(p);
    vec2 duv1 = dFdx(uv);
    vec2 duv2 = dFdy(uv);

    vec3 dp2perp = cross(dp2, N);
    vec3 dp1perp = cross(N, dp1);
    vec3 T = dp2perp * duv1.x + dp1perp * duv2.x;
    vec3 B = dp2perp * duv1.y + dp1perp * duv2.y;

    float invmax = inversesqrt(max(dot(T, T), dot(B, B)));
    return mat3(T * invmax, B * invmax, N);
}

void main() 
{
    float isWater = ((waterFlags & (1<<25)) > 0) ? 0.0f : 1.0f;
    float myAlpha = alpha * isWater;
    if (myAlpha < 0.5) discard;
    
    // apply waves
    float caustics = length(flowVectorf) > 0.001 ? 0 : 1;
    float div = ((waterFlags & (1<<27)) > 0) ? 90 : 10;
	//float noise = generateNoise(worldPos.xyz + playerpos.xyz, div, wind);

    mat3 tbn = cotangentFrame(worldNormal, worldPos.xyz, uv);
    mat3 invTbn = transpose(tbn);

    //vec3 normalMap = vec3(noise, noise, 0.0f);
    vec3 normalMap = vec3(0);
    //generateNoiseBump(normalMap, div);
    vec3 parallaxPos;
    vec3 viewTangent = normalize(invTbn * (worldPos.xyz - cameraWorldPosition.xyz));
    generateNoiseParallax(normalMap, viewTangent, div, parallaxPos);

    if (dropletIntensity > 0.001) {
        //generateSplash(fragWorldPos.xyz);
        generateSplashBump(normalMap, parallaxPos);
    }

    vec3 worldNormalMap = tbn * normalMap;
    vec3 camNormalMap = (modelViewMatrix * vec4(worldNormalMap, 0.0)).xyz;
    vec3 myGNormal = gnormal.xyz;
    
    if (dot(gnormal.xyz, fragPosition.xyz) > 0) {
        // flip the normal if viewed from behind
        myGNormal = -gnormal.xyz;
    }
    
	outGPosition = vec4(fragPosition.xyz, 0);
	outGNormal = vec4(normalize(camNormalMap*2 + myGNormal), 1.0 - playerUnderwater * caustics);
    outTint = vec4(getColorMapped(terrainTex, vec4(1)).rgb, 0);
    #if VSMOD_REFRACT > 0
    outRefraction = vec4((-camNormalMap.xy*1.2) / fragPosition.z, 0, 0);
    #endif
}