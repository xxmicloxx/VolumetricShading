#version 330 core
#extension GL_ARB_explicit_attrib_location: enable

uniform sampler2D terrainTex;
uniform sampler2D terrainTexLinear;

in vec4 fragPosition;
flat in int renderFlags;
in vec3 vertexPosition;
in vec4 gnormal;
in vec2 uv;

layout(location = 0) out vec4 outGPosition;
layout(location = 1) out vec4 outGNormal;
layout(location = 2) out vec4 outTint;

#include colormap.fsh


void main()
{
    // read shiny flag
    if (((renderFlags >> 5) & 1) == 0) discard;

	outGPosition = vec4(fragPosition.xyz, 0);
	outGNormal = vec4(gnormal.xyz, 0);
    outTint = vec4(pow(texture(terrainTex, uv).rgb, vec3(2.2)) * getColorMapping(terrainTexLinear).rgb, 0);
}
