#version 330 core

uniform vec2 invFrameSizeIn;
uniform vec3 sunPosScreenIn;
uniform vec3 sunPos3dIn;
uniform vec3 playerViewVector;
uniform float iGlobalTimeIn;
uniform float directionIn;
uniform int dusk;
uniform float moonLightStrength;
uniform float sunLightStrength;
uniform float dayLightStrength;
uniform float shadowIntensity;
uniform float flatFogDensity;
uniform float playerWaterDepth;
uniform vec4 fogColor;

out vec2 texCoord;
out vec3 sunPosScreen;
out float iGlobalTime;
out float direction;
out vec3 frontColor;
out vec3 backColor;

const float NumDayColors = 5.0;
const vec3 DayColors[5] = vec3[5](
vec3(1.0f, 0.2f, 0.0f),
vec3(1.0f, 0.6f, 0.2f),
vec3(0.8f, 0.75f, 0.7f),
vec3(0.6f, 0.8f, 1.0f),
vec3(0.4f, 0.6f, 1.0f)
);

/*const float NumDayColors = 5.0;
const vec3 DayColors[5] = vec3[5](
	vec3(1.0f, 0.0f, -0.2f),
	vec3(1.0f, 0.3f, 0.0f),
	vec3(1.0f, 0.5f, 0.1f),
	vec3(1.0f, 0.8f, 0.4f),
	vec3(1.0f, 0.5f, 0.0f)
);*/

void main(void)
{
    // https://rauwendaal.net/2014/06/14/rendering-a-screen-covering-triangle-in-opengl/
    float x = -1.0 + float((gl_VertexID & 1) << 2);
    float y = -1.0 + float((gl_VertexID & 2) << 1);
    gl_Position = vec4(x, y, 0, 1);
    texCoord = vec2((x+1.0) * 0.5, (y + 1.0) * 0.5);

    sunPosScreen = sunPosScreenIn;
    iGlobalTime = iGlobalTimeIn;

    direction = dot(sunPos3dIn, playerViewVector) >= 0 ? 1 : -1;

    vec3 moonColor = vec3(0.2, 0.4, 0.7) * moonLightStrength;

    float height = pow(min(max(sunPos3dIn.y*1.5f, 0.0f), 1.0f), 2.5f);
    float actualScale = height * NumDayColors;
    float cmpH = min(floor(actualScale), NumDayColors-1.0f);
    float cmpH1 = min(floor(actualScale)+1.0f, NumDayColors-1.0f);

    vec3 temp = DayColors[int(cmpH)];
    vec3 temp2 = DayColors[int(cmpH1)];
    vec3 sunlight = mix(temp, temp2, fract(actualScale));

    float rayIntensity = min(pow(shadowIntensity, 2.0f), 1.0f) * 1.2f;
    vec3 sunColor = sunlight * rayIntensity; // midday
    
    vec3 usedBackColor = mix(vec3(1.0f, 0.1f, 0.3f)*0.75, vec3(0.4f, 0.6f, 1.0f), clamp(height * 5, 0.0, 1.0));
    vec3 sunBackColor = usedBackColor * rayIntensity;
    
    vec3 outColor = moonColor;
    vec3 outBackColor = moonColor;
    if (sunLightStrength > 0.15f) {
        outColor = sunColor;
        outBackColor = sunBackColor;
    } else if (sunLightStrength > 0.05f) {
        float mixStrength = (sunLightStrength - 0.05f) / 0.1f;
        outColor = mix(moonColor, sunColor, mixStrength);
        outBackColor = mix(moonColor, sunBackColor, mixStrength);
    }

    float depthMult = clamp(playerWaterDepth * 5, 0, 1);
    outColor = mix(outColor, fogColor.xyz, depthMult);
    outBackColor = mix(outBackColor, fogColor.xyz, depthMult);

    float fogDensity = clamp((0.03 - flatFogDensity) * 50, 0, 1);
    outColor *= fogDensity;
    outBackColor *= fogDensity;
    
    frontColor = outColor;
    backColor = outBackColor;
}