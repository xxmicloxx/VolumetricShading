#if SHADOWQUALITY > 0
uniform float shadowRangeFar;
uniform mat4 toShadowMapSpaceMatrixFar;
#endif

#if SHADOWQUALITY > 1
uniform float shadowRangeNear;
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

float getBrightnessFromShadowMap(vec4 shadowCoordsNear, vec4 shadowCoordsFar) {
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

float getShadowBrightnessAt(vec4 worldPos) {
    vec4 shadowCoordNear = vec4(0);
    vec4 shadowCoordFar = vec4(0);

    calcShadowMapCoords(worldPos, shadowCoordNear, shadowCoordFar);
    return getBrightnessFromShadowMap(shadowCoordNear, shadowCoordFar) * 2.0 - 1.0;
}