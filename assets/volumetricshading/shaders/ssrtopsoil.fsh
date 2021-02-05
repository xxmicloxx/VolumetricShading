#version 330 core

uniform sampler2D terrainTex;

in vec3 worldPosition;
in vec4 fragPosition;
in vec3 normal;
in vec4 gnormal;
in vec2 uv;

flat in int renderFlags;


layout(location = 0) out vec4 outGPosition;
layout(location = 1) out vec4 outGNormal;
layout(location = 2) out vec4 outTint;

#include noise3d.ash

void main() 
{
	if (normal.y <= 0) discard; // we only want top
	
	vec4 color = texture(terrainTex, uv);
	if (color.a < 0.02) discard;
	
	float noise = gnoise(worldPosition * 10);
	noise += cnoise(worldPosition * 50) * 0.5;
	float alpha = 0.0;
	
	vec3 normal = gnormal.xyz;
	normal += vec3(noise * 0.05);
	
	outGPosition = vec4(fragPosition.xyz, alpha);
	outGNormal = vec4(normalize(normal), alpha);
	outTint = vec4(vec3(1.0), alpha);
}