#version 330 core
#extension GL_ARB_explicit_attrib_location: enable

layout(location = 0) in vec3 vertexPositionIn;
layout(location = 1) in vec2 uvIn;
// rgb = block light, a=sun light level
layout(location = 2) in vec4 rgbaLightIn;

// Bits 0-7: Glow level
// Bits 8-10: Z-Offset
// Bit 11: Wind waving yes/no
// Bit 12: Water waving yes/no
// Bit 13: Reflective yes/no
// Bit 14-26: x/y/z normals, 12 bits total. Each axis with 1 sign bit and 3 value bits
// Bit 27: Leaves waving yes/no
// Bit 28, 29, 30: Ground distance
// Bit 31: Is Lod 0
layout(location = 3) in int renderFlagsIn;

// Bits 0..7 = season map index
// Bits 8..11 = climate map index
// Bits 12 = Frostable bit
// Bits 13, 14, 15 = free \o/
// Bits 16-23 = temperature
// Bits 24-31 = rainfall
layout(location = 4) in int colormapData;


uniform vec3 origin;
uniform mat4 projectionMatrix;
uniform mat4 modelViewMatrix;

out vec4 fragPosition;
out vec4 gnormal;
out vec2 uv;
flat out int renderFlags;

#include vertexflagbits.ash
#include vertexwarp.vsh
#include fogandlight.vsh
#include colormap.vsh

void main(void)
{
	bool isLeaves = ((renderFlagsIn & 0x8000000) > 0); 
	vec4 worldPos = vec4(vertexPositionIn + origin, 1.0);
	
	worldPos = applyVertexWarping(renderFlagsIn, worldPos);
	
	vec4 cameraPos = modelViewMatrix * worldPos;

	gl_Position = projectionMatrix * cameraPos;
	
	calcColorMapUvs(colormapData, worldPos + vec4(playerpos,1), rgbaLightIn.a, isLeaves);
	
	// Lower 8 bit is glow level
	renderFlags = renderFlagsIn >> 8;  
	
	vec3 normal = unpackNormal(renderFlagsIn);	
    worldPos.xyz += normal * 0.001;
	
	fragPosition = cameraPos;
	gnormal = modelViewMatrix * vec4(normal.xyz, 0);
	uv = uvIn;

	// Now the lowest 3 bits are used as an unsigned number 
	// to fix Z-Fighting on blocks over certain other blocks. 
	if (renderFlags > 0 && gl_Position.z > 0) {
		gl_Position.w += (renderFlags & 7) * 0.00025 / max(0.1, gl_Position.z);
	}

    gl_Position.z -= 0.01;
}