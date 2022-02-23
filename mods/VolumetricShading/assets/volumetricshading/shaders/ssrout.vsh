#version 330 core

out vec2 texcoord;

void main(void)
{
	// https://rauwendaal.net/2014/06/14/rendering-a-screen-covering-triangle-in-opengl/
	float x = -1.0 + float((gl_VertexID & 1) << 2);
    float y = -1.0 + float((gl_VertexID & 2) << 1);
    gl_Position = vec4(x, y, 0, 1);
    texcoord = vec2((x+1.0) * 0.5, (y + 1.0) * 0.5);
} 