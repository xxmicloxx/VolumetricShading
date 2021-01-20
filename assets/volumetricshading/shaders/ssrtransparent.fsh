#version 330 core
#extension GL_ARB_explicit_attrib_location: enable

uniform sampler2D terrainTex;
in vec2 uv;

flat in int renderFlags;
in vec4 gnormal;
in vec4 gposition;


layout(location = 0) out vec4 outGPosition;
layout(location = 1) out vec4 outGNormal;
layout(location = 2) out vec4 outTint;

#include colormap.fsh

void main()
{
	if (((renderFlags >> 5) & 1) == 0) discard;
	outGPosition = vec4(gposition.xyz, 0);
	outGNormal = vec4(gnormal.xyz, 0);
	outTint = vec4(texture(terrainTex, uv).xyz, 0);
}