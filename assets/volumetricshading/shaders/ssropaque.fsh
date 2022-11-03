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

#include vertexflagbits.ash
#include colormap.fsh


void main()
{
    // read shiny flag
    if ((renderFlags & ReflectiveBitMask) == 0) discard;
    vec4 color = texture(terrainTex, uv);
    if (color.a < 0.5) discard;

	outGPosition = vec4(fragPosition.xyz, 0);
	outGNormal = vec4(normalize(gnormal.xyz), playerUnderwater);
	color = vec4(pow(color.rgb, vec3(2.2)), 1.0);
    outTint = vec4(getColorMapped(terrainTexLinear, color).rgb, 0);
    #if VSMOD_REFRACT > 0
    outRefraction = vec4(0, 0, 0, 1);
    #endif
}
