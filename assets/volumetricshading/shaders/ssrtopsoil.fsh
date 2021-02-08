#version 330 core

uniform sampler2D terrainTex;
uniform float rainStrength = 1.0;
uniform float playerUnderwater;

in vec3 worldPosition;
in vec4 fragPosition;
in vec3 normal;
in vec4 gnormal;
in vec2 uv;

flat in int renderFlags;


layout(location = 0) out vec4 outGPosition;
layout(location = 1) out vec4 outGNormal;
layout(location = 2) out vec4 outTint;
#if VSMOD_REFRACT > 0
layout(location = 3) out vec4 outRefraction;
#endif

#include noise3d.ash

void main()
{
	vec4 color = texture(terrainTex, uv);
	if (color.a < 0.02) discard;
	
	float noise = gnoise(worldPosition * 10);
	noise += cnoise(worldPosition * 50) * 0.5;
	float alpha = 1.0 - pow(rainStrength, 0.7);

	if (normal.y <= 0) {
		alpha = 1.0 - (1.0 - alpha) * 0.3;
	}
	
	vec3 normal = gnormal.xyz;
	normal += vec3(noise * 0.05);
	
	outGPosition = vec4(fragPosition.xyz, alpha);
	outGNormal = vec4(normalize(normal), playerUnderwater);
	outTint = vec4(vec3(1.0), 0);
	#if VSMOD_REFRACT > 0
	outRefraction = vec4(0, 0, 0, 1);
	#endif
}