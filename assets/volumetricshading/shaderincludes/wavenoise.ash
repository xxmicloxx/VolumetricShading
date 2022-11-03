uniform float waterWaveCounter;
uniform float waterFlowCounter;
uniform float windSpeed;

float generateWaveNoise(vec3 coord1, float div) {
    vec3 coord2 = coord1 * 4 + 16;

    vec3 noisepos1 = vec3(coord1.x - windWaveCounter / 3, coord1.z, waterWaveCounter / 12 + windWaveCounter / 4);
    vec3 noisepos2 = vec3(coord2.x - windWaveCounter / 4, coord2.z, waterWaveCounter / 12 + windWaveCounter / 4);

    return gnoise(noisepos1) / div * ((50 * windSpeed) + 3) + gnoise(noisepos2) / div * ((4 * windSpeed) + 1);
}

#define F length(.5-fract(k.xyz*=mat3(-2,-1,2, 3,-2,1, 1,2,2)*

float cellNoise(vec3 k)
{
    return pow(min(min(F.5)),F.4))),F.3))), 6.)*10.;
}

float generateCausticsNoise(vec3 coord, vec3 sunPos) {
    vec3 coord1 = coord;
    coord1.xz -= (sunPos * coord.y).xz;
    coord1.x -= windWaveCounter / 3;
    coord1 *= 0.25;
    
    float div = 90;
    
    vec3 noisepos1 = vec3(coord1.x, coord1.z, waterWaveCounter / 12 + windWaveCounter / 12 + coord1.y);
    
    return (cellNoise(noisepos1) - 0.1) / div * ((24 * windSpeed) + 3);
}