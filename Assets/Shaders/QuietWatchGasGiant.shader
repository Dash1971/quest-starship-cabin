Shader "StarshipCabin/QuietWatchGasGiant"
{
    Properties
    {
        _PaleBand ("Pale Band", Color) = (0.94, 0.69, 0.39, 1)
        _DarkBand ("Dark Band", Color) = (0.22, 0.065, 0.045, 1)
        _StormColor ("Storm", Color) = (1.0, 0.28, 0.075, 1)
        _SunDirection ("Sun Direction", Vector) = (-0.62, 0.30, -0.72, 0)
        _WeatherPulse ("Weather Grace", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        Cull Back
        ZWrite On
        Pass
        {
            Name "GreatWeather"
            Tags { "LightMode"="SRPDefaultUnlit" }
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _PaleBand;
                half4 _DarkBand;
                half4 _StormColor;
                float4 _SunDirection;
                float _WeatherPulse;
            CBUFFER_END

            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings
            {
                float4 positionCS:SV_POSITION;
                float3 normalWS:TEXCOORD0;
                float3 globe:TEXCOORD1;
                float3 viewWS:TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float hash21(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float noise2(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(
                    lerp(hash21(i), hash21(i + float2(1, 0)), f.x),
                    lerp(hash21(i + float2(0, 1)), hash21(i + 1.0), f.x), f.y);
            }

            float fbm4(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.5;
                [unroll] for (int i = 0; i < 4; i++)
                {
                    value += noise2(p) * amplitude;
                    p = p * 2.03 + float2(11.7, 7.1);
                    amplitude *= 0.5;
                }
                return value;
            }

            float vortex(float2 uv, float2 center, float2 aspect, float turns)
            {
                float2 delta = (uv - center) * aspect;
                float radius = length(delta);
                float angle = atan2(delta.y, delta.x);
                float spiral = 0.5 + 0.5 * sin(angle * turns - radius * 78.0);
                float body = smoothstep(0.12, 0.018, radius);
                return body * (0.52 + spiral * 0.48);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                VertexPositionInputs p = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = p.positionCS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.globe = normalize(input.normalOS);
                output.viewWS = GetCameraPositionWS() - p.positionWS;
                return output;
            }

            half4 frag(Varyings input):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float3 n = normalize(input.normalWS);
                float3 v = normalize(input.viewWS);
                float3 sun = normalize(_SunDirection.xyz);

                float longitude = atan2(input.globe.x, -input.globe.z) * 0.15915494 + 0.5;
                float latitude = asin(clamp(input.globe.y, -1.0, 1.0)) * 0.31830989 + 0.5;
                float2 globeUv = float2(longitude, latitude);

                // Multiple wind speeds break the old regular striped-ball read.
                // The motion is deliberately geological rather than screen-like.
                float time = _Time.y * 0.0022;
                float broadWarp = fbm4(float2(longitude * 4.0 + time, latitude * 10.0) + 4.2) - 0.5;
                float shearWarp = fbm4(float2(longitude * 11.0 - time * 1.7, latitude * 31.0) + 19.7) - 0.5;
                float warpedLatitude = latitude + broadWarp * 0.034 + shearWarp * 0.010;

                float broadBands = 0.5 + 0.5 * sin(warpedLatitude * 78.0 + sin(longitude * 10.0) * 0.55);
                float narrowBands = 0.5 + 0.5 * sin(warpedLatitude * 183.0 + broadWarp * 7.0);
                float ribbon = smoothstep(0.16, 0.84, broadBands) * 0.72 + narrowBands * 0.28;
                float turbulent = fbm4(float2(longitude * 28.0 + time * 3.1, warpedLatitude * 92.0));
                ribbon = saturate(ribbon * 0.70 + turbulent * 0.44 - 0.06);

                float3 ochre = lerp(_DarkBand.rgb, _PaleBand.rgb, smoothstep(0.10, 0.90, ribbon));
                float polarFade = smoothstep(0.52, 0.12, abs(latitude - 0.5));
                ochre = lerp(ochre * float3(0.64, 0.70, 0.80), ochre, polarFade);

                // Broad belts shift hue as well as brightness. Fine turbulent
                // ridges then shade the cloud deck, so it reads as stacked
                // weather at depth rather than stripes painted on a sphere.
                float warmBelt = smoothstep(0.54, 0.82, broadBands)
                    * smoothstep(0.18, 0.78, turbulent);
                float paleZone = smoothstep(0.62, 0.92, narrowBands)
                    * (1.0 - smoothstep(0.56, 0.90, broadBands));
                ochre = lerp(ochre, float3(0.66, 0.20, 0.07), warmBelt * 0.42);
                ochre = lerp(ochre, float3(1.0, 0.78, 0.48), paleZone * 0.34);
                float cloudRelief = fbm4(float2(longitude * 61.0 - time * 4.0, warpedLatitude * 146.0) + 8.4);
                ochre *= 0.82 + cloudRelief * 0.34;

                // A primary oval and a smaller trailing storm make the weather
                // read as embedded circulation rather than a painted dot.
                float primaryStorm = vortex(globeUv, float2(0.61, 0.43), float2(1.0, 2.4), 5.0);
                float secondaryStorm = vortex(globeUv, float2(0.39, 0.57), float2(1.0, 3.2), -4.0) * 0.54;
                float roamingStorm = vortex(globeUv, float2(0.08, 0.48), float2(1.0, 2.8), 4.0) * 0.62
                    + vortex(globeUv, float2(0.90, 0.55), float2(1.0, 3.1), -5.0) * 0.48;
                float stormNoise = 0.72 + fbm4(globeUv * float2(42.0, 88.0) + 33.0) * 0.42;
                float storm = saturate((primaryStorm + secondaryStorm + roamingStorm) * stormNoise);
                float eye = smoothstep(0.018, 0.005,
                    length((globeUv - float2(0.61, 0.43)) * float2(1.0, 2.4)));
                float stormFilaments = 0.64 + 0.36 * sin((longitude + latitude * 0.18) * 510.0 + stormNoise * 13.0);
                float3 color = lerp(ochre, _StormColor.rgb, storm * (0.70 + stormFilaments * 0.20));
                color = lerp(color, _PaleBand.rgb * 1.24, eye * 0.78);

                // The ring plane casts a broad soft diagonal shadow across the
                // dayside. It is intentionally approximate but spatially tied
                // to the authored ring composition rather than texture bands.
                float3 ringPlane = normalize(float3(0.18, 0.83, 0.53));
                float planeDistance = dot(input.globe, ringPlane) + 0.028;
                float ringShadow = smoothstep(0.105, 0.025, abs(planeDistance));

                float sunDot = dot(n, sun);
                float light = smoothstep(-0.16, 0.22, sunDot);
                float daysideShadow = ringShadow * smoothstep(-0.04, 0.38, sunDot);
                light *= 1.0 - daysideShadow * 0.62;

                float viewDot = saturate(dot(n, v));
                float atmosphere = pow(1.0 - viewDot, 2.35);
                float forwardGlow = smoothstep(-0.18, 0.42, sunDot);
                // Reflected ring light keeps the nominal nightside legible in
                // the cabin; the direct sun still supplies the main contrast.
                color *= 0.245 + light * 1.08;
                color += float3(0.72, 0.30, 0.09) * atmosphere * (0.18 + forwardGlow * 0.82);
                color += float3(0.12, 0.19, 0.36) * atmosphere * (1.0 - light) * 0.24;

                // A thin forward-scattering veil catches light over the limb
                // and within bright zones, separating upper haze from belts.
                float upperHaze = pow(1.0 - viewDot, 5.0) * smoothstep(-0.12, 0.55, sunDot);
                color += float3(1.0, 0.55, 0.22) * upperHaze * 0.72;
                color += _StormColor.rgb * storm * _WeatherPulse * 0.075;
                color += float3(0.88, 0.48, 0.20) * atmosphere * _WeatherPulse * 0.055;

                // Filmic compression preserves storm and shadow structure under
                // the cabin's restrained bloom without flattening the nightside.
                color = 1.0 - exp(-color * 1.58);
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
