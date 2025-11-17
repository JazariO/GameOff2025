TEXTURE2D_SHADOW(_MainLightShadowmapTexture);
TEXTURE2D_SHADOW(_AdditionalLightsShadowmapTexture);

struct Light
{
    half3 direction;
    half3 color;
    float distanceAttenuation;
    half shadowAttenuation;
};

Light MainLight()
{
    Light light;
    light.direction = half3(_MainLightPosition.xyz);
    light.shadowAttenuation = 1.0;
    light.color = _MainLightColor.rgb;
    return light;
}


void MainLightMap_float(float3 worldPos, float3 worldNorm, float3 worldView, float4 screenPos, out float3 lightMap)
{
    
}

void OldLightMap_float(float3 worldPos, float3 worldNorm, float3 worldView, float4 screenPos, out float3 lightMap)
{
    uint pixelLightCount = GetAdditionalLightsCount();
    float3 additionalLightMap = float3(0, 0, 0);
    float valueMap = 0;

    
    half4 shadowMask = float4(0, 0, 0, 0);
       
    LIGHT_LOOP_BEGIN(pixelLightCount)
    {
        Light light = GetAdditionalLight(lightIndex, worldPos, shadowMask);

        float nDotL = saturate(dot(light.direction, worldNorm));
        float atten = light.distanceAttenuation * light.shadowAttenuation;
        float diffuse = nDotL * atten;

        float3 halfVec = normalize(light.direction + worldView);
        float nDotH = saturate(dot(worldNorm, halfVec));
        float mod = pow(nDotH, specularFallOff);
        
        float3 specular = mod * specularStrength * diffuse * light.color;
        
        float fresnel = 1 - saturate(dot(worldView, worldNorm));
        float ambient = saturate(dot(worldNorm, -light.direction));
        rimLightPower = max(rimLightPower, 0.001);
        float rimLight = pow(fresnel * ambient, rimLightPower) * rimLightStrength;
        
        additionalLightMap += light.color * (diffuse + specular + rimLight);
    }
    LIGHT_LOOP_END

    float4 shadowCoord = TransformWorldToShadowCoord(worldPos);
    
    Light mainLight = GetMainLight(shadowCoord);
    float nDotL = dot(mainLight.direction, worldNorm) * 0.5 + 0.5;
    
    float3 mainLightMap = mainLight.color * nDotL;
    
    return additionalLightMap + mainLightMap;
}