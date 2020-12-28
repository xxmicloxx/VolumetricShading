#version 330 core
#extension GL_ARB_explicit_attrib_location: enable

uniform sampler2D terrainTexLinear;

in vec4 fragPosition;
flat in int renderFlags;
in vec3 vertexPosition;
in vec4 gnormal;

layout(location = 0) out vec4 outGPosition;
layout(location = 1) out vec4 outGNormal;
layout(location = 2) out vec4 outTint;

#include colormap.fsh


void main()
{
    // read shiny flag
    float alpha = ((renderFlags >> 5) & 1) > 0 ? 0 : 1;
	outGPosition = vec4(fragPosition.xyz, alpha);
	outGNormal = vec4(gnormal.xyz, alpha);
    outTint = vec4(getColorMapping(terrainTexLinear).rgb, alpha);
}
