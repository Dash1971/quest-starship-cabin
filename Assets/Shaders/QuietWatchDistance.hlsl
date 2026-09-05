#ifndef QUIET_WATCH_DISTANCE
#define QUIET_WATCH_DISTANCE
// Include after the material declares _DistanceScale and _DistanceOrigin.
float3 QWProjectionPosition(float3 proxyWS)
{
    // P - E/scale is equivalent in direction to scale*P - E. Evaluate after
    // UNITY_SETUP_INSTANCE_ID: E must be this eye, not a shared centre camera.
    float inverseScale = rcp(max(1.0, _DistanceScale));
    return proxyWS + (GetCameraPositionWS() - _DistanceOrigin.xyz) * (1.0 - inverseScale);
}

float3 QWViewDirection(float3 proxyWS)
{
    float3 eyeInProxy = _DistanceOrigin.xyz
        + (GetCameraPositionWS() - _DistanceOrigin.xyz) / max(1.0, _DistanceScale);
    return normalize(eyeInProxy - proxyWS);
}

float3 QWDawnSun(float3 direction, float progress)
{
    float angle = progress * 0.32;
    float s = sin(angle), c = cos(angle);
    return normalize(float3(direction.x*c + direction.z*s, direction.y, -direction.x*s + direction.z*c));
}
#endif
