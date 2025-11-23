#define BIRDCOUNT 32
float4 _BirdPositions[16];

void BirdMap_float(float2 worldPos, out float o)
{
    o = 0.0;

    for (int i = 0; i < BIRDCOUNT; i++)
    {
        int pairIndex = i / 2;
        bool isEven = (i % 2) == 0;

        float2 birdPos = isEven ? _BirdPositions[pairIndex].xy : _BirdPositions[pairIndex].zw;

        o += 1 - (saturate(length(worldPos - birdPos) * 20));
    }
}
