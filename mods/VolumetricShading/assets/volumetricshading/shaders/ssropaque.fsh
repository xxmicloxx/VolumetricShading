#version 330 core
#extension GL_ARB_explicit_attrib_location: enable

uniform sampler2D terrainTex;
uniform sampler2D terrainTexLinear;
uniform float playerUnderwater;

in vec4 fragPosition;
flat in int renderFlags;
in vec4 gnormal;
in vec2 uv;

layout(location = 0) out vec4 outGPosition;
layout(location = 1) out vec4 outGNormal;
layout(location = 2) out vec4 outTint;
#if VSMOD_REFRACT > 0
layout(location = 3) out vec4 outRefraction;
#endif

#include colormap.fsh


void main()
{
    // read shiny flag
    if (((renderFlags >> 5) & 1) == 0) discard;
    vec4 color = texture(terrainTex, uv);
    if (color.a < 0.5) discard;

	outGPosition = vec4(fragPosition.xyz, 0);
	outGNormal = vec4(normalize(gnormal.xyz), playerUnderwater);
    outTint = vec4(pow(color.rgb, vec3(2.2)) * getColorMapped(terrainTexLinear, texture(terrainTex, uv)).rgb, 0);
    #if VSMOD_REFRACT > 0
    outRefraction = vec4(0, 0, 0, 1);
    #endif
}
