#if SHADOWQUALITY > 0
uniform mat4 toShadowMapSpaceMatrixFar;
#endif

#if SHADOWQUALITY > 1
uniform mat4 toShadowMapSpaceMatrixNear;
#endif

void calcShadowMapCoords(vec4 worldPos, out vec4 shadowCoordsNear, out vec4 shadowCoordsFar) {
    float nearSub = 0;
    #if SHADOWQUALITY > 0
    float len = length(worldPos);
    #endif

    #if SHADOWQUALITY > 1
    // Near map
    shadowCoordsNear = toShadowMapSpaceMatrixNear * worldPos;

    float distanceNear = clamp(
    max(max(0, 0.03 - shadowCoordsNear.x) * 100, max(0, shadowCoordsNear.x - 0.97) * 100) +
    max(max(0, 0.03 - shadowCoordsNear.y) * 100, max(0, shadowCoordsNear.y - 0.97) * 100) +
    max(0, shadowCoordsNear.z - 0.98) * 100 +
    max(0, len / shadowRangeNear - 0.15)
    , 0, 1);

    nearSub = shadowCoordsNear.w = clamp(1.0 - distanceNear, 0.0, 1.0);

    #endif
    #if SHADOWQUALITY > 0
    // Far map
    shadowCoordsFar = toShadowMapSpaceMatrixFar * worldPos;

    float distanceFar = clamp(
    max(max(0, 0.03 - shadowCoordsFar.x) * 10, max(0, shadowCoordsFar.x - 0.97) * 10) +
    max(max(0, 0.03 - shadowCoordsFar.y) * 10, max(0, shadowCoordsFar.y - 0.97) * 10) +
    max(0, shadowCoordsFar.z - 0.98) * 10 +
    max(0, len / shadowRangeFar - 0.15)
    , 0, 1);

    distanceFar = distanceFar * 2 - 0.5;

    shadowCoordsFar.w = max(0, clamp(1.0 - distanceFar, 0.0, 1.0) - nearSub);

    //shadowCoordsFar.w = max(0, 1.0 - nearSub);

    #endif
}

float getPCFBrightness(vec4 shadowCoordsNear, vec4 shadowCoordsFar) {
    #if SHADOWQUALITY > 0

    float totalFar = 0.0;
    if (shadowCoordsFar.z < 0.999 && shadowCoordsFar.w > 0) {
        for (int x = -1; x <= 1; x++) {
            for (int y = -1; y <= 1; y++) {
                float inlight = texture (shadowMapFar, vec3(shadowCoordsFar.xy + vec2(x * shadowMapWidthInv, y * shadowMapHeightInv), shadowCoordsFar.z - 0.0009 + (0.0001 * VSMOD_FARSHADOWOFFSET)));
                totalFar += 1 - inlight;
            }
        }
    }

    totalFar /= 9.0f;


    float b = 1.0 - totalFar * shadowCoordsFar.w * 0.5;
    #endif


    #if SHADOWQUALITY > 1
    float totalNear = 0.0;
    if (shadowCoordsNear.z < 0.999 && shadowCoordsNear.w > 0) {
        for (int x = -1; x <= 1; x++) {
            for (int y = -1; y <= 1; y++) {
                float inlight = texture (shadowMapNear, vec3(shadowCoordsNear.xy + vec2(x * shadowMapWidthInv, y * shadowMapHeightInv), shadowCoordsNear.z - 0.0005 + (0.0001 * VSMOD_NEARSHADOWOFFSET)));
                totalNear += 1 - inlight;
            }
        }
    }

    totalNear /= 9.0f;


    b -= totalNear * shadowCoordsNear.w * 0.5;
    #endif

    #if SHADOWQUALITY > 0
    b = clamp(b, 0, 1);
    return b;
    #endif

    return 1.0;
}

float getPCSSBrightness(vec4 shadowCoordsNear, vec4 shadowCoordsFar, float blockBrightness, vec3 normal) {
    #if SHADOWQUALITY > 0

    float totalFar = 0.0;
    if (shadowCoordsFar.z < 0.999 && shadowCoordsFar.w > 0) {
        for (int x = -1; x <= 1; x++) {
            for (int y = -1; y <= 1; y++) {
                float inlight = texture (shadowMapFar, vec3(shadowCoordsFar.xy + vec2(x * shadowMapWidthInv, y * shadowMapHeightInv), shadowCoordsFar.z - 0.0009 + (0.0001 * VSMOD_FARSHADOWOFFSET)));
                totalFar += 1 - inlight;
            }
        }
    }

    totalFar /= 9.0f;


    float b = 1.0 - shadowIntensity * totalFar * shadowCoordsFar.w * 0.5;
    #endif


    #if SHADOWQUALITY > 1
    #if VSMOD_SOFTSHADOWS > 0
    float totalNear = 1.0;
    if (shadowCoordsNear.z < 0.999 && shadowCoordsNear.w > 0) {
        totalNear = 1.0 - vsmod_pcss_sloped(shadowCoordsNear.xyz, normal);
    }
    #else
    float totalNear = 0.0;
    if (shadowCoordsNear.z < 0.999 && shadowCoordsNear.w > 0) {
        for (int x = -1; x <= 1; x++) {
            for (int y = -1; y <= 1; y++) {
                float inlight = texture (shadowMapNear, vec3(shadowCoordsNear.xy + vec2(x * shadowMapWidthInv, y * shadowMapHeightInv), shadowCoordsNear.z - 0.0005 + (0.0001 * VSMOD_NEARSHADOWOFFSET)));
                totalNear += 1 - inlight;
            }
        }
    }

    totalNear /= 9.0f;
    #endif


    b -= shadowIntensity * totalNear * shadowCoordsNear.w * 0.5;
    #endif

    #if SHADOWQUALITY > 0
    b = clamp(b + blockBrightness, 0, 1);
    return b;
    #endif

    return 1.0;
}

float getPCFBrightnessAt(vec4 worldPos) {
    vec4 shadowCoordNear = vec4(0);
    vec4 shadowCoordFar = vec4(0);

    calcShadowMapCoords(worldPos, shadowCoordNear, shadowCoordFar);
    return getPCFBrightness(shadowCoordNear, shadowCoordFar);
}

float getPCSSBrightnessAt(vec4 worldPos, float blockBrightness, vec3 normal) {
    vec4 shadowCoordNear = vec4(0);
    vec4 shadowCoordFar = vec4(0);

    calcShadowMapCoords(worldPos, shadowCoordNear, shadowCoordFar);
    return getPCSSBrightness(shadowCoordNear, shadowCoordFar, blockBrightness, normal);
}

vec4 applyOverexposedFogAndShadowDeferred(vec4 worldPos, vec4 rgbaPixel, float fogWeight, vec3 normal,
    float normalShadeIntensity, float minNormalShade, float fogDensity, float blockBrightness, inout float glow) {

    float b = getPCSSBrightnessAt(worldPos, blockBrightness, normal);
    float nb = getBrightnessFromNormal(normal, normalShadeIntensity, minNormalShade);

    float outB = min(b, nb);
    rgbaPixel *= vec4(outB, outB, outB, 1);

    applyOverexposure(rgbaPixel, b, normal, worldPos.xyz, fogDensity, glow);

    return applyFog(rgbaPixel, fogWeight);
}

float getFogLevelDeferred(float depth, float fogMin, float fogDensity, float worldPosY) {
    float clampedDepth = min(250, depth);
    float heightDiff = worldPosY - flatFogStart;

    //float extraDistanceFog = max(-flatFogDensity * flatFogStart / (160 + heightDiff * 3), 0);   // heightDiff*3 seems to fix distant mountains being supper fogged on most flat fog values
    // ^ this breaks stuff. Also doesn't seem to be needed? Seems to work fine without

    float extraDistanceFog = max(-flatFogDensity * clampedDepth * (flatFogStart) / 60, 0); // div 60 was 160 before, at 160 thick flat fog looks broken when looking at trees

    float distanceFog = 1 - 1 / exp(clampedDepth * fogDensity + extraDistanceFog);
    float flatFog = 1 - 1 / exp(heightDiff * flatFogDensity);

    float val = max(flatFog, distanceFog);
    float nearnessToPlayer = clamp((8-depth)/8, 0, 0.9);
    val = max(min(0.04, val), val - nearnessToPlayer);

    // Needs to be added after so that underwater fog still gets applied. 
    val += fogMin;

    return clamp(val, 0, 1);
}

uint volumetricHash( uint x, uint y )
{
    x += x >> 11;
    x ^= x << 7;
    x += y;
    x ^= x << 6;
    x += x >> 15;
    x ^= x << 5;
    x += x >> 12;
    x ^= x << 9;
    return x;
}

float volumetricRandom( uvec2 v ) {
    const uint mantissaMask = 0x007FFFFFu;
    const uint one          = 0x3F800000u;
   
    uvec2 uv = floatBitsToUint(v);
    uint h = volumetricHash( uv.x, uv.y );
    h &= mantissaMask;
    h |= one;
    
    float  r2 = uintBitsToFloat( h );
    return r2 - 1.0;
}

float calculateVolumetricScatterDeferred(vec4 worldPos, vec4 cameraPos) {
    #if GODRAYS > 0
    vec4 shadowCoordsFar = toShadowMapSpaceMatrixFar * worldPos;
    vec4 shadowRayStart = toShadowMapSpaceMatrixFar * cameraPos;
    vec4 shadowLightPos = toShadowMapSpaceMatrixFar * vec4(lightPosition, 0.0);
    
    float dither = fract(0.75487765 * gl_FragCoord.x + 0.56984026 * gl_FragCoord.y);

    const int maxSamples = 6;

    vec3 dV = (shadowCoordsFar.xyz-shadowRayStart.xyz)/maxSamples;

    vec3 progress = shadowRayStart.xyz + dV*dither;

    float vL = 0.0f;

    for (int i = 0; i < maxSamples; ++i) {
        vL += texture(shadowMapFar, vec3(progress.xy, progress.z - 0.0009));
        progress += dV;
    }

    float normalOut = min(1, vL * length(worldPos) / 1000.0f / maxSamples);
    float intensity = dot(normalize(dV), normalize(shadowLightPos.xyz));
    //float phase = 2.5+exp(intensity*3.0)/3.0;
    float phase = 2.0+exp(intensity*4.0)/4.0;
    return min(0.9f, pow(phase * normalOut, VOLUMETRIC_FLATNESS));
    #endif
    return 0.0f;
}