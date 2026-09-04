// The Quiet Watch: The First Question.
//
// The field is sampled from the camera ray, not the window mesh UV, so it
// remains celestial space beyond the glass as the officer moves their head.
// The visual distribution deliberately follows the proven V3 starfield:
// many clean point sources, a wide brightness range, and only rare circular
// halos. Diffraction spikes were removed after Quest testing because they
// resolved as HUD-like scratches rather than stars.
Shader "StarshipCabin/QuietWatchStarWindow"
{
    Properties
    {
        _DeepColor ("Deep Space", Color) = (0.0004, 0.0008, 0.0022, 1)
        _HazeColor ("Distant Haze", Color) = (0.018, 0.026, 0.050, 1)
        _BandColor ("Galactic River", Color) = (0.075, 0.082, 0.120, 1)
        _StarColor ("Star Color", Color) = (0.96, 0.98, 1.0, 1)
        _WarmColor ("Warm Stars", Color) = (1.0, 0.72, 0.48, 1)
        _CoolColor ("Cool Stars", Color) = (0.58, 0.76, 1.0, 1)
        _Density ("Density", Range(0.2, 1.0)) = 0.78
        _Twinkle ("Twinkle", Range(0, 0.12)) = 0.012
        _Speed ("Comfort Drift", Float) = 0
        _Drift ("Comfort Rise", Float) = 0
        _NebulaAmount ("Legacy Nebula", Range(0, 1)) = 0
        _Meteors ("Legacy Meteors", Range(0, 1)) = 0
        _GraceStart ("Grace Note Start", Float) = -1000
        _VistaClock ("Vista Clock", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Background" "Queue"="Background" "RenderPipeline"="UniversalPipeline" }
        Cull Back
        ZWrite Off
        Lighting Off

        Pass
        {
            Name "QuietWatchStars"
            Tags { "LightMode"="SRPDefaultUnlit" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _DeepColor;
                half4 _HazeColor;
                half4 _BandColor;
                half4 _StarColor;
                half4 _WarmColor;
                half4 _CoolColor;
                float _Density;
                float _Twinkle;
                float _Speed;
                float _Drift;
                float _NebulaAmount;
                float _Meteors;
                float _GraceStart;
                float _VistaClock;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = position.positionCS;
                output.positionWS = position.positionWS;
                return output;
            }

            float hash21(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453123);
            }

            float2 hash22(float2 p)
            {
                float2 q = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
                return frac(sin(q) * 43758.5453123);
            }

            float valueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(
                    lerp(hash21(i), hash21(i + float2(1, 0)), u.x),
                    lerp(hash21(i + float2(0, 1)), hash21(i + 1.0), u.x),
                    u.y);
            }

            float fbm(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.5;
                [unroll] for (int octave = 0; octave < 4; octave++)
                {
                    value += valueNoise(p) * amplitude;
                    p = p * 2.03 + 13.71;
                    amplitude *= 0.5;
                }
                return value;
            }

            float2 DirectionToSky(float3 direction)
            {
                float longitude = atan2(direction.x, -direction.z) * 0.159154943 + 0.5;
                float latitude = asin(clamp(direction.y, -1.0, 1.0)) * 0.318309886 + 0.5;
                return float2(longitude, latitude);
            }

            float galacticMask(float2 sky)
            {
                // An asymmetric tilted river reads as a galactic structure,
                // not a decorative sine wave painted across the glass.
                float spine = 0.51 + (sky.x - 0.5) * 0.11
                    + 0.075 * sin((sky.x + 0.08) * 6.2831853)
                    + 0.018 * sin(sky.x * 18.849556);
                float distanceToSpine = sky.y - spine;
                float taper = smoothstep(0.03, 0.24, sky.x) * smoothstep(1.02, 0.74, sky.x);
                return exp(-distanceToSpine * distanceToSpine * 74.0) * (0.68 + taper * 0.32);
            }

            float3 stellarTint(float value)
            {
                float3 tint = _StarColor.rgb;
                tint = lerp(tint, _WarmColor.rgb, saturate((0.22 - value) * 5.0));
                tint = lerp(tint, _CoolColor.rgb, saturate((value - 0.78) * 5.0));
                return tint;
            }

            float3 starLayer(
                float2 sky,
                float scale,
                float radius,
                float occupancy,
                float parallax,
                float band,
                float gain,
                float allowHalo,
                float elapsed)
            {
                float2 layerSky = sky;
                layerSky.x += _Speed * elapsed * 0.00065 * parallax;
                layerSky.y += sin(elapsed * 0.018) * _Drift * 0.0009 * parallax;

                float2 grid = layerSky * scale;
                float2 cell = floor(grid);
                float2 local = frac(grid);
                float2 random = hash22(cell + scale * 0.173);

                // Direction-space covers much less area than the old mesh UVs.
                // A higher grid frequency restores the proven dense-sky read
                // without making individual stars larger or streak-like.
                float density = saturate(_Density * occupancy * (1.0 + band * 1.15));
                float keep = step(1.0 - density, hash21(cell * 1.71 + 3.13));
                float2 starPoint = 0.16 + random * 0.68;
                float2 offset = local - starPoint;

                // Equirectangular longitude spans 360 degrees while latitude
                // spans 180. Correct that metric before measuring the core or
                // stars become horizontal ovals on the window.
                float latitude = (sky.y - 0.5) * 3.14159265;
                float longitudeMetric = 2.0 * max(0.35, cos(latitude));
                float2 sphericalOffset = offset;
                sphericalOffset.x *= longitudeMetric;
                float distanceToPoint = length(sphericalOffset);

                float magnitude = hash21(cell + 7.77);
                float brightness = 0.10 + 3.45 * pow(magnitude, 6.0);
                float sizeVariation = 0.78 + 1.15 * pow(magnitude, 10.0);
                float coreRadius = radius * sizeVariation;

                // Never derive the filter width from frac(grid): its jump at a
                // cell edge produces false horizontal/vertical strokes. The
                // continuous grid derivatives give one stable pixel footprint.
                float2 gridDx = ddx(grid);
                float2 gridDy = ddy(grid);
                gridDx.x *= longitudeMetric;
                gridDy.x *= longitudeMetric;
                float antialias = max(min(length(gridDx), length(gridDy)) * 0.62, 0.0030);
                float core = smoothstep(
                    coreRadius + antialias,
                    max(0.0, coreRadius - antialias),
                    distanceToPoint);

                float shimmer = 1.0 - _Twinkle * (1.0 - saturate(brightness))
                    * (0.5 + 0.5 * sin(elapsed * (0.42 + random.x * 0.65) + random.y * 6.2831853));
                float3 tint = stellarTint(hash21(cell + 21.3));
                float3 color = tint * core * brightness;

                // A rare circular bloom cue gives a few stars hierarchy while
                // remaining a light source, never a crosshair or line.
                float brilliant = step(0.982, magnitude) * allowHalo;
                float halo = smoothstep(coreRadius * 4.8, coreRadius * 1.1, distanceToPoint);
                color += tint * halo * 0.32 * brilliant;

                return color * keep * shimmer * gain;
            }

            float3 firstQuestionComet(float2 sky, float now)
            {
                float age = now - _GraceStart;
                float alive = step(0.0, age) * step(age, 8.0);
                float progress = saturate(age / 8.0);
                float ease = progress * progress * (3.0 - 2.0 * progress);
                float2 start = float2(0.27, 0.69);
                float2 finish = float2(0.65, 0.43);
                float2 head = lerp(start, finish, ease);
                float2 direction = normalize(finish - start);
                float2 delta = sky - head;
                float behind = dot(delta, -direction);
                float perpendicular = abs(dot(delta, float2(-direction.y, direction.x)));
                float tail = smoothstep(0.18, 0.0, behind) * step(0.0, behind);
                float trailLine = smoothstep(0.0045, 0.0006, perpendicular);
                float headGlow = smoothstep(0.026, 0.0, length(delta));
                float lifeFade = sin(progress * 3.14159265);
                return lerp(_CoolColor.rgb, _StarColor.rgb, 0.65) * (trailLine * tail + headGlow * 1.8) * lifeFade * alive;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float3 ray = normalize(input.positionWS - GetCameraPositionWS());
                float2 sky = DirectionToSky(ray);
                float elapsed = max(0.0, _Time.y - _VistaClock);
                float band = galacticMask(sky);

                float hazeStructure = fbm(sky * float2(4.0, 7.0) + 2.7);
                float3 color = lerp(_DeepColor.rgb, _HazeColor.rgb, hazeStructure * 0.035);

                float dustLarge = fbm(sky * float2(15.0, 10.0) + 9.2);
                float dustFine = fbm(sky * float2(39.0, 27.0) + 31.4);
                float dust = smoothstep(0.50, 0.79, dustLarge * 0.72 + dustFine * 0.28);
                float riverTexture = fbm(sky * float2(12.0, 17.0) + 21.0);
                float river = band * (0.075 + riverTexture * 0.20);
                color += _BandColor.rgb * river * (1.0 - dust * 0.94);
                color += _WarmColor.rgb * band * smoothstep(0.72, 0.90, riverTexture) * 0.006;

                float3 stars = 0.0;
                stars += starLayer(sky,         34.0, 0.007, 0.18, 1.00, band, 1.22, 1.0, elapsed);
                stars += starLayer(sky + 4.61,  48.0, 0.010, 0.46, 0.88, band, 0.94, 0.0, elapsed);
                stars += starLayer(sky + 9.17,  92.0, 0.017, 0.38, 0.68, band, 0.76, 0.0, elapsed);
                stars += starLayer(sky + 37.51, 176.0, 0.030, 0.26, 0.40, band, 0.52, 0.0, elapsed);
                stars += starLayer(sky + 73.21, 320.0, 0.050, 0.10, 0.22, band, 0.31, 0.0, elapsed);
                color += stars;
                color += firstQuestionComet(sky, _Time.y);

                // Filmic response keeps true negative space while preserving
                // brilliant stellar cores for the existing restrained bloom.
                color = 1.0 - exp(-color * 1.52);
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
