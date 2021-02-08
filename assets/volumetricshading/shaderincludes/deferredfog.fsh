float getFogLevelDeferred(float depth, float fogMin, float fogDensity, float worldPosY) {
    float clampedDepth = min(250, depth);
    float heightDiff = worldPosY - flatFogStart;

    //float extraDistanceFog = max(-flatFogDensity * flatFogStart / (160 + heightDiff * 3), 0);   // heightDiff*3 seems to fix distant mountains being supper fogged on most flat fog values
    // ^ this breaks stuff. Also doesn't seem to be needed? Seems to work fine without

    float extraDistanceFog = max(-flatFogDensity * clampedDepth * (flatFogStart) / 60, 0); // div 60 was 160 before, at 160 thick flat fog looks broken when looking at trees

    float distanceFog = 1 - 1 / exp(clampedDepth * (fogDensity + extraDistanceFog));
    float flatFog = 1 - 1 / exp(heightDiff * flatFogDensity);

    float val = max(flatFog, distanceFog);
    float nearnessToPlayer = clamp((8-depth)/16, 0, 0.8);
    val = max(min(0.04, val), val - nearnessToPlayer);

    // Needs to be added after so that underwater fog still gets applied. 
    val += fogMin;

    return clamp(val, 0, 1);
}