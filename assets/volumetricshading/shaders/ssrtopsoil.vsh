#version 330 core
#extension GL_ARB_explicit_attrib_location: enable

layout(location = 0) in vec3 vertexPositionIn;
layout(location = 1) in vec2 uvIn;
// rgb = block light, a=sun light level
layout(location = 2) in vec4 rgbaLightIn;
layout(location = 3) in int renderFlagsIn;
layout(location = 4) in vec2 uv2In;

// Bits 0..7 = season map index
// Bits 8..11 = climate map index
// Bits 12 = Frostable bit
// Bits 13, 14, 15 = free \o/
// Bits 16-23 = temperature
// Bits 24-31 = rainfall
layout(location = 5) in int colormapData;


uniform vec3 origin;
uniform mat4 projectionMatrix;
uniform mat4 modelViewMatrix;

out vec3 worldPosition;
out vec4 fragPosition;
out vec4 gnormal;
out vec3 normal;
out vec2 uv;

flat out int renderFlags;

#include vertexflagbits.ash
#include fogandlight.vsh
#include vertexwarp.vsh

void main(void)
{
	vec4 worldPos = vec4(vertexPositionIn + origin, 1.0);
	
	worldPos = applyVertexWarping(renderFlagsIn, worldPos);
	worldPosition = worldPos.xyz + playerpos;
	
	vec4 cameraPos = modelViewMatrix * worldPos;
	cameraPos.w += 0.01;
	
	gl_Position = projectionMatrix * cameraPos;
	
	uv = uvIn;

	renderFlags = renderFlagsIn;
	normal = unpackNormal(renderFlagsIn);
	
	fragPosition = cameraPos;
	gnormal = modelViewMatrix * vec4(normal, 0);
}
