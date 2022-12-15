#version 330 core
#extension GL_ARB_explicit_attrib_location: enable

layout(location = 0) in vec3 vertexPositionIn;
layout(location = 1) in vec2 uvIn;
// rgb = block light, a=sun light level
layout(location = 2) in vec4 rgbaLightIn;
// Check out vertexflagbits.ash for understanding the contents of this data
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
	bool isLeaves = ((renderFlagsIn & WindModeBitMask) > 0); 
	vec4 worldPos = vec4(vertexPositionIn + origin, 1.0);
	
	worldPos = applyVertexWarping(renderFlagsIn, worldPos);
	
	vec4 cameraPos = modelViewMatrix * worldPos;

	gl_Position = projectionMatrix * cameraPos;
	
	calcColorMapUvs(colormapData, worldPos + vec4(playerpos,1), rgbaLightIn.a, isLeaves);
	
	renderFlags = renderFlagsIn;  
	vec3 normal = unpackNormal(renderFlagsIn);	
    worldPos.xyz += normal * 0.001;
	
	fragPosition = cameraPos;
	gnormal = modelViewMatrix * vec4(normal.xyz, 0);
	uv = uvIn;

	// Now the lowest 3 bits are used as an unsigned number 
	// to fix Z-Fighting on blocks over certain other blocks. 
	if (gl_Position.z > 0) {
		int zOffset = (renderFlags & ZOffsetBitMask) >> 8;
        gl_Position.w += zOffset * 0.00025 / max(0.1, gl_Position.z * 0.05);
	}

    gl_Position.z -= 0.01;
}