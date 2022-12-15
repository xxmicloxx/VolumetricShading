#version 330 core
#extension GL_ARB_explicit_attrib_location: enable

uniform sampler2D terrainTex;
uniform float playerUnderwater;
in vec2 uv;

flat in int renderFlags;
in vec3 fragWorldPos;
in vec4 gnormal;
in vec4 gposition;


layout(location = 0) out vec4 outGPosition;
layout(location = 1) out vec4 outGNormal;
layout(location = 2) out vec4 outTint;
#if VSMOD_REFRACT > 0
layout(location = 3) out vec4 outRefraction;
#endif

#include vertexflagbits.ash
#include colormap.fsh
#include noise3d.ash

void main()
{
	if ((renderFlags & ReflectiveBitMask) == 0) discard;
	vec4 color = texture(terrainTex, uv);
	if (color.a < 0.02) discard;
	
	float refractionMultiplier = 10;
	float noise = cnoise(fragWorldPos) * 0.01;
	if ((renderFlags & 7) == 1) {
		// ice has z-offset 1, glass has 0 or 2
		noise += gnoise(fragWorldPos*10) * 0.02;
		refractionMultiplier = 5;
	}
	
	outGPosition = vec4(gposition.xyz, 0);
	outGNormal = vec4(normalize(gnormal.xyz + vec3(noise)), playerUnderwater);
	outTint = vec4(color.xyz, 0);
	#if VSMOD_REFRACT > 0
	outRefraction = vec4(vec2(noise * refractionMultiplier / gposition.z), 0, 0);
	#endif
}