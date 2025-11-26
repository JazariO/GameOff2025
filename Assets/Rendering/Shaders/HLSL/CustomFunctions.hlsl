#define BIRDCOUNT 32
float4 _BirdPositions[BIRDCOUNT];

void BirdMap_float(float2 worldPos, float2 texSize, out float o)
{
    o = 0.0;

    for (int i = 0; i < BIRDCOUNT; i++)
    {
        float2 birdPos = _BirdPositions[i].xy / texSize;

        o += 1 - (saturate(length(worldPos - birdPos) * 20));
    }
}
