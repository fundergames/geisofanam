#ifndef GEIS_SOUL_REALM_TAKEN_COMMON_INCLUDED
#define GEIS_SOUL_REALM_TAKEN_COMMON_INCLUDED

// Shared noise + dissolve helpers for Geis/SoulRealm/Taken.
// Object-space sampling follows the Destiny 2 Taken approach: stable smoke/veins that
// do not swim with UV seams.

float GeisTakenHash31(float3 p)
{
    p = frac(p * 0.3183099 + 0.1);
    p *= 17.0;
    return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
}

float GeisTakenValueNoise3(float3 p)
{
    float3 i = floor(p);
    float3 f = frac(p);
    f = f * f * (3.0 - 2.0 * f);

    float n000 = GeisTakenHash31(i + float3(0.0, 0.0, 0.0));
    float n100 = GeisTakenHash31(i + float3(1.0, 0.0, 0.0));
    float n010 = GeisTakenHash31(i + float3(0.0, 1.0, 0.0));
    float n110 = GeisTakenHash31(i + float3(1.0, 1.0, 0.0));
    float n001 = GeisTakenHash31(i + float3(0.0, 0.0, 1.0));
    float n101 = GeisTakenHash31(i + float3(1.0, 0.0, 1.0));
    float n011 = GeisTakenHash31(i + float3(0.0, 1.0, 1.0));
    float n111 = GeisTakenHash31(i + float3(1.0, 1.0, 1.0));

    float nx00 = lerp(n000, n100, f.x);
    float nx10 = lerp(n010, n110, f.x);
    float nx01 = lerp(n001, n101, f.x);
    float nx11 = lerp(n011, n111, f.x);
    float nxy0 = lerp(nx00, nx10, f.y);
    float nxy1 = lerp(nx01, nx11, f.y);
    return lerp(nxy0, nxy1, f.z);
}

float GeisTakenFbm3(float3 p, int octaves)
{
    float value = 0.0;
    float amplitude = 0.5;
    float frequency = 1.0;
    for (int i = 0; i < octaves; i++)
    {
        value += amplitude * GeisTakenValueNoise3(p * frequency);
        frequency *= 2.03;
        amplitude *= 0.5;
    }
    return value;
}

float3 GeisTakenObjectSpaceSample(float3 positionOS, float time, float noiseScale, float noiseSpeed, float3 noiseFlow)
{
    float3 p = positionOS * noiseScale;
    float t = time * noiseSpeed;
    p += noiseFlow * t;
    p.y += sin(t * 0.73 + p.x * 0.35) * 0.18;
    p.x += cos(t * 0.61 + p.z * 0.29) * 0.14;
    return p;
}

void GeisTakenEvaluateSurface(
    float3 positionOS,
    float3 normalWS,
    float3 viewDirWS,
    float2 uv,
    float time,
    float noiseScale,
    float noiseSpeed,
    float3 noiseFlow,
    float veinThreshold,
    float veinSoftness,
    float veinIntensity,
    float starScale,
    float starBrightness,
    float fresnelPower,
    float fresnelIntensity,
    half4 baseMap,
    half4 darkColor,
    half4 veinColor,
    half4 fresnelColor,
    out half3 color,
    out half alpha)
{
    float3 p = GeisTakenObjectSpaceSample(positionOS, time, noiseScale, noiseSpeed, noiseFlow);

    float smoke = GeisTakenFbm3(p, 4);
    float veins = GeisTakenFbm3(p * 1.85 + float3(4.2, 1.7, 2.9), 3);
    float detail = GeisTakenFbm3(p * 4.6 + float3(11.0, 3.0, 7.0), 2);

    float veinMask = smoothstep(veinThreshold - veinSoftness, veinThreshold + veinSoftness, veins);
    veinMask = saturate(veinMask * (0.55 + smoke * 0.75));
    veinMask *= veinIntensity;

    float starCell = GeisTakenHash31(floor(positionOS * starScale + float3(time * 0.07, 0.0, time * 0.05)));
    float stars = step(1.0 - starBrightness, starCell) * smoothstep(0.72, 0.95, detail);

    half3 albedo = lerp(darkColor.rgb, veinColor.rgb, veinMask);
    albedo = lerp(albedo, veinColor.rgb * 1.35h, stars);
    albedo *= lerp(half3(1.0h, 1.0h, 1.0h), baseMap.rgb, baseMap.a > 0.001h ? 0.65h : 0.0h);

    float fresnel = pow(saturate(1.0 - dot(normalize(normalWS), normalize(viewDirWS))), fresnelPower);
    albedo += fresnelColor.rgb * fresnel * fresnelIntensity;

    color = albedo;
    alpha = saturate(lerp(darkColor.a, veinColor.a, veinMask) + fresnel * 0.25h + stars * 0.15h);
}

void GeisTakenApplyDissolve(
    float3 positionOS,
    float dissolve,
    float noiseScale,
    half4 edgeColor,
    float edgeWidth,
    inout half3 color,
    inout half alpha)
{
    float dissolveNoise = GeisTakenFbm3(positionOS * noiseScale * 0.85 + float3(19.0, 7.0, 3.0), 3);
    float edge = smoothstep(dissolve - edgeWidth, dissolve, dissolveNoise);
    float edgeGlow = smoothstep(dissolve - edgeWidth * 1.35, dissolve - edgeWidth * 0.15, dissolveNoise)
        * (1.0 - smoothstep(dissolve - edgeWidth * 0.15, dissolve + 0.02, dissolveNoise));

    color += edgeColor.rgb * edgeGlow * edgeColor.a;
    alpha *= 1.0h - edge;
    clip(alpha - 0.001h);
}

#endif
