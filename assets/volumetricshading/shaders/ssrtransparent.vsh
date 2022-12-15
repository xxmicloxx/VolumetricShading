#version 330 core
#extension GL_ARB_explicit_attrib_location: enable

layout(location = 0) in vec3 vertexPositionIn;
layout(location = 1) in vec2 uvIn;
// rgb = block light, a=sun light level
layout(location = 2) in vec4 rgbaLightIn;
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

out vec2 uv;
out vec4 gposition;
out vec3 fragWorldPos;

flat out int renderFlags;
out vec4 gnormal;


#include vertexflagbits.ash
#include shadowcoords.vsh
#include fogandlight.vsh
#include vertexwarp.vsh
#include colormap.vsh

void main(void)
{
	vec4 worldPos = vec4(vertexPositionIn + origin, 1.0);
	
	worldPos = applyVertexWarping(renderFlagsIn, worldPos);

	vec4 cameraPos = modelViewMatrix * worldPos;
	
	gl_Position = projectionMatrix * cameraPos;
	
	fragWorldPos = worldPos.xyz + playerpos;
	
	calcColorMapUvs(colormapData, vec4(vertexPositionIn + origin, 1.0) + vec4(playerpos, 1), rgbaLightIn.a, false);
	
	uv = uvIn;
	
	// Lower 8 bit is glow level
	renderFlags = renderFlagsIn;  
	
	// Now the lowest 3 bits are used as an unsigned number 
	// to fix Z-Fighting on blocks over certain other blocks. 
	if (renderFlags > 0 && gl_Position.z > 0) {
		gl_Position.w += (renderFlags & 7) * 0.00025 / max(0.1, gl_Position.z);
	}
	
	gnormal = modelViewMatrix * vec4(unpackNormal(renderFlagsIn).xyz, 0);
	gposition = cameraPos;
}