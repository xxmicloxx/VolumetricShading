#version 330 core
#extension GL_ARB_explicit_attrib_location: enable

layout(location = 0) in vec3 vertexPositionIn;
layout(location = 1) in vec2 uvIn;
// rgb = block light, a=sun light level
layout(location = 2) in vec4 rgbaLightIn;
// Check out chunkvertexflags.ash for understanding the contents of this data
layout(location = 3) in int renderFlags;

layout(location = 4) in vec2 flowVector;

// Bit 0: Should animate yes/no
// Bit 1: Should texture fade yes/no
// Bits 8-15: x-Distance to upper left corner, where 255 = size of the block texture
// Bits 16-24: y-Distance to upper left corner, where 255 = size of the block texture
// Bit 25: Lava yes/no
// Bit 26: Weak foamy yes/no
// Bit 27: Weak Wavy yes/no
layout(location = 5) in int waterFlagsIn;

// Bits 0..7 = season map index
// Bits 8..11 = climate map index
// Bits 12 = Frostable bit
// Bits 13, 14, 15 = free \o/
// Bits 16-23 = temperature
// Bits 24-31 = rainfall
layout(location = 6) in int colormapData;

uniform vec3 origin;
uniform mat4 projectionMatrix;
uniform mat4 modelViewMatrix;
uniform vec4 rgbaFogIn;

out vec2 flowVectorf;
out vec4 worldPos;
out vec4 fragPosition;
out vec4 gnormal;
out vec3 worldNormal;
out vec3 fragWorldPos;
out vec2 uv;
flat out int waterFlags;
flat out float alpha;
flat out int skyExposed;


#include vertexflagbits.ash
#include vertexwarp.vsh
#include fogandlight.vsh
#include colormap.vsh

void main(void)
{
	worldPos = vec4(vertexPositionIn + origin, 1.0);
	
	float div = ((waterFlagsIn & (1<<27)) > 0) ? 90 : 10;
	float yBefore = worldPos.y;
	
	worldPos = applyLiquidWarping((waterFlagsIn & 0x2000000) == 0, worldPos, div);
	
	vec4 cameraPos = modelViewMatrix * worldPos;
	
	gl_Position = projectionMatrix * cameraPos;
	
	vec3 fragNormal = unpackNormal(renderFlags);

	fragWorldPos = worldPos.xyz + playerpos;
    fragPosition = cameraPos;
	gnormal = modelViewMatrix * vec4(fragNormal.xyz, 0);
	worldNormal = fragNormal;
    waterFlags = waterFlagsIn;
	skyExposed = renderFlags & LiquidExposedToSkyBitMask;

	flowVectorf = flowVector;
	uv = uvIn;

	alpha = rgbaLightIn.a < 0.2f ? 0.0f : 1.0f;
	calcColorMapUvs(colormapData, vec4(vertexPositionIn + origin, 1.0) + vec4(playerpos, 1), rgbaLightIn.a, false);
}