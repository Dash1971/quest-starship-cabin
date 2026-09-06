#ifndef QUIET_WATCH_WEATHER_COMMON
#define QUIET_WATCH_WEATHER_COMMON

// All coordinates below are in the same compressed exterior space. A single
// scale applies to planet, rings and moons, preserving their depth ordering.
CBUFFER_START(UnityPerMaterial)
    half4 _PaleBand, _DarkBand, _StormColor;
    half4 _Highland, _Maria, _LightColor, _DarkColor;
    float4 _SunDirection;
    float4 _DistanceOrigin;
    float4 _RingCenter, _RingNormal, _RingRadii, _PlanetSphere;
    float _DistanceScale, _WeatherPulse, _ObservationTime;
    half4 _AtmosphereColor;
    float _AtmosphereHeight, _DawnProgress;
CBUFFER_END

#include "QuietWatchDistance.hlsl"

float QWRingDensity(float radius)
{
    float t = saturate((radius - _RingRadii.x) / max(0.01, _RingRadii.y - _RingRadii.x));
    float edge = smoothstep(0.0, 0.025, t) * (1.0 - smoothstep(0.965, 1.0, t));
    float footprint = max(fwidth(radius), 0.00001);
    float broad = 1.0 - smoothstep(1.0, 3.14159, footprint * 2.9);
    float fine = 1.0 - smoothstep(1.0, 3.14159, footprint * 9.7);
    float structure = 0.68 + 0.18 * sin(radius * 2.9) * broad + 0.10 * sin(radius * 9.7) * fine;
    float division = 1.0 - 0.92 * exp(-pow((t - 0.64) / 0.027, 2.0));
    return saturate(structure * edge * division);
}

float QWRingTransmission(float3 positionWS)
{
    float3 sun = normalize(_SunDirection.xyz);
    float denominator = dot(sun, _RingNormal.xyz);
    if (abs(denominator) < 0.0001) return 1.0;
    float distanceToPlane = dot(_RingCenter.xyz - positionWS, _RingNormal.xyz) / denominator;
    if (distanceToPlane <= 0.001) return 1.0;
    float radius = length(positionWS + sun * distanceToPlane - _RingCenter.xyz);
    return 1.0 - QWRingDensity(radius) * 0.92;
}

float QWPlanetTransmission(float3 positionWS)
{
    float3 toCenter = _PlanetSphere.xyz - positionWS;
    float projected = dot(toCenter, normalize(_SunDirection.xyz));
    if (projected <= 0.0) return 1.0;
    float separation = sqrt(max(0.0, dot(toCenter, toCenter) - projected * projected));
    return smoothstep(_PlanetSphere.w - 0.35, _PlanetSphere.w + 0.35, separation);
}
#endif
