#version 330 core
#extension GL_ARB_explicit_attrib_location: enable

uniform sampler2D terrainTex;
in vec2 uv;

flat in int renderFlags;
in vec3 fragWorldPos;
in vec4 gnormal;
in vec4 gposition;


layout(location = 0) out vec4 outGPosition;
layout(location = 1) out vec4 outGNormal;
layout(location = 2) out vec4 outTint;

#include colormap.fsh
#include noise3d.ash

void main()
{
	if (((renderFlags >> 5) & 1) == 0) discard;
	vec4 color = texture(terrainTex, uv);
	if (color.a < 0.02) discard;
	
	float noise = cnoise(fragWorldPos) * 0.01;
	if ((renderFlags & 7) == 1) {
		// ice has z-offset 1, glass has 0 or 2
		noise += gnoise(fragWorldPos*10) * 0.02;
	}
	
	outGPosition = vec4(gposition.xyz, 0);
	outGNormal = vec4(normalize(gnormal.xyz + vec3(noise)), 0);
	outTint = vec4(color.xyz, 0);
}