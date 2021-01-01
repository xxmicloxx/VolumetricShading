#version 330 core

uniform sampler2D terrainTex;
uniform vec3 playerpos;
uniform float windWaveCounter;
uniform float waterWaveCounter;
uniform float waterFlowCounter;
uniform mat4 modelViewMatrix;
uniform float dropletIntensity = 0;

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

#include colormap.fsh

vec3 ghash( vec3 p ) // replace this by something better
{
	p = vec3( dot(p,vec3(127.1,311.7, 74.7)),
			  dot(p,vec3(269.5,183.3,246.1)),
			  dot(p,vec3(113.5,271.9,124.6)));

	return -1.0 + 2.0*fract(sin(p)*43758.5453123);
}

float gnoise( in vec3 p )
{
    vec3 i = floor( p );
    vec3 f = fract( p );
	
	vec3 u = f*f*(3.0-2.0*f);

    return 0.7 * mix( mix( mix( dot( ghash( i + vec3(0.0,0.0,0.0) ), f - vec3(0.0,0.0,0.0) ), 
                          dot( ghash( i + vec3(1.0,0.0,0.0) ), f - vec3(1.0,0.0,0.0) ), u.x),
                     mix( dot( ghash( i + vec3(0.0,1.0,0.0) ), f - vec3(0.0,1.0,0.0) ), 
                          dot( ghash( i + vec3(1.0,1.0,0.0) ), f - vec3(1.0,1.0,0.0) ), u.x), u.y),
                mix( mix( dot( ghash( i + vec3(0.0,0.0,1.0) ), f - vec3(0.0,0.0,1.0) ), 
                          dot( ghash( i + vec3(1.0,0.0,1.0) ), f - vec3(1.0,0.0,1.0) ), u.x),
                     mix( dot( ghash( i + vec3(0.0,1.0,1.0) ), f - vec3(0.0,1.0,1.0) ), 
                          dot( ghash( i + vec3(1.0,1.0,1.0) ), f - vec3(1.0,1.0,1.0) ), u.x), u.y), u.z );
}

vec2 droplethash3( vec2 p )
{
    vec2 q = vec2(dot(p,vec2(127.1,311.7)), dot(p,vec2(269.5,183.3)));
	return fract(sin(q)*43758.5453);
}

float dropletnoise(in vec2 x, in float waveCounter)
{
	if (dropletIntensity < 0.001) return 0.;
	
    x *= dropletIntensity;
    
    vec2 p = floor(x);
    vec2 f = fract(x);
    
		
	float va = 0.0;
    for( int j=-1; j<=1; j++ )
    for( int i=-1; i<=1; i++ )
    {
        vec2 g = vec2(float(i), float(j));
		vec2 o = droplethash3(p + g);
		vec2 r = ((g - f) + o.xy) / dropletIntensity;
		float d = sqrt(dot(r,r));
        
        float a = max(cos(d - waveCounter * 2.7 + (o.x + o.y) * 5.0), 0.);
        a = smoothstep(0.99, 0.999, a);
        
	    float ripple = mix(a, 0., d);
        va += max(ripple, 0.);
    }
	
    return va;
}

float generateNoise(vec3 coord1, float div, float wind) {
    vec3 coord2 = coord1 * 4 + 16;

    vec3 noisepos1 = vec3(coord1.x - windWaveCounter / 6, coord1.z, waterWaveCounter / 12 + wind * windWaveCounter / 6);
    vec3 noisepos2 = vec3(coord2.x - windWaveCounter / 6, coord2.z, waterWaveCounter / 12 + wind * windWaveCounter / 6);

    return gnoise(noisepos1) / div + gnoise(noisepos2) / div;
}

float generateSplash(vec3 pos)
{
    vec2 uv = 6.0 * pos.xz;

    float totalNoise = 0;
    for (int i = 0; i < 2; ++i) {
        totalNoise += dropletnoise(uv, waterWaveCounter - (0.1*i));
    }
    return totalNoise;
}

void generateBump(inout vec3 normalMap)
{
    const vec3 deltaPos = vec3(0.01, 0.0, 0.0);
    
    float val0 = generateSplash(fragWorldPos.xyz);
    float val1 = generateSplash(fragWorldPos.xyz + deltaPos.xyz);
    float val2 = generateSplash(fragWorldPos.xyz - deltaPos.xyz);
    float val3 = generateSplash(fragWorldPos.xyz + deltaPos.zyx);
    float val4 = generateSplash(fragWorldPos.xyz - deltaPos.zyx);

    float xDelta = ((val1 - val0) + (val0 - val2));
    float zDelta = ((val3 - val0) + (val0 - val4));

    normalMap += vec3(xDelta * 0.5, zDelta * 0.5, 0) * 0.75;
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
    return transpose(mat3(T * invmax, B * invmax, N));
}

void main() 
{
    // apply waves
    float div = ((waterFlags & (1<<27)) > 0) ? 90 : 10;
    float wind = ((waterFlags & 0x2000000) == 0) ? 1 : 0;
	float noise = generateNoise(worldPos.xyz + playerpos.xyz, div, wind);

    mat3 tbn = transpose(cotangentFrame(worldNormal, worldPos.xyz, uv));

    vec3 normalMap = vec3(noise, noise, 0f);

    float isWater = ((waterFlags & (1<<25)) > 0) ? 0f : 1f;

    if (isWater > 0 && skyExposed > 0) {
        //generateSplash(fragWorldPos.xyz);
        generateBump(normalMap);
    }

    vec3 worldNormalMap = tbn * normalMap;
    vec3 camNormalMap = (modelViewMatrix * vec4(worldNormalMap, 0.0)).xyz;
    float myAlpha = alpha * isWater;
	outGPosition = vec4(fragPosition.xyz, myAlpha);
	outGNormal = vec4(normalize(camNormalMap + gnormal.xyz), myAlpha);
    outTint = vec4(getColorMapping(terrainTex).rgb, myAlpha);
}