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
    float4 _OccultorSphere, _CompanionSphere;
    float _SolarAngularRadius, _CloudLayerHeight, _CloudReliefStrength;
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
    float normalizedFootprint = footprint / max(0.01, _RingRadii.y - _RingRadii.x);
    float structure = 0.55
        + 0.18 * cos(t * 21.99115) * (1.0 - smoothstep(1.0, 3.14159, normalizedFootprint * 21.99115))
        + 0.11 * cos(t * 53.40708) * (1.0 - smoothstep(1.0, 3.14159, normalizedFootprint * 53.40708))
        + 0.12 * sin(radius * 2.9) * broad + 0.05 * sin(radius * 9.7) * fine;
    float gapWidth = max(0.027, normalizedFootprint * 0.5);
    float division = 1.0 - 0.92 * (0.027 / gapWidth) * exp(-pow((t - 0.64) / gapWidth, 2.0));
    return saturate(structure * edge * division);
}

float QWRingTransmission(float3 positionWS)
{
    float3 sun = normalize(_SunDirection.xyz);
    float denominator = dot(sun, _RingNormal.xyz);
    if (abs(denominator) < 0.0001) return 1.0;
    float distanceToPlane = dot(_RingCenter.xyz - positionWS, _RingNormal.xyz) / denominator;
    float radius = length(positionWS + sun * distanceToPlane - _RingCenter.xyz);
    float transmission = 1.0 - QWRingDensity(radius) * 0.92;
    return distanceToPlane > 0.001 ? transmission : 1.0;
}

// Approximate the finite solar disc with an umbra and soft penumbra.
// Geometry and shading share the same moving sphere; no painted shadow UV.
float QWSphereTransmission(float3 positionWS, float4 sphere)
{
    float3 toMoon = sphere.xyz - positionWS;
    float projected = dot(toMoon, normalize(_SunDirection.xyz));
    float separation = length(toMoon - normalize(_SunDirection.xyz) * projected);
    float penumbra = max(0.025, max(projected, 0.0) * _SolarAngularRadius);
    penumbra = max(penumbra, fwidth(separation));
    float transmission = smoothstep(max(0.0, sphere.w - penumbra),
        sphere.w + penumbra, separation);
    return (sphere.w > 0.0 && projected > 0.0) ? transmission : 1.0;
}

float QWMoonTransmission(float3 positionWS)
{
    // Both visible moons supply the same geometric shadow model.
    return min(QWSphereTransmission(positionWS, _OccultorSphere),
        QWSphereTransmission(positionWS, _CompanionSphere));
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
