Shader "StarshipCabin/QuietWatchBlueWorld"
{
    Properties
    {
        _OceanColor ("Ocean", Color) = (0.015, 0.12, 0.28, 1)
        _LandColor ("Land", Color) = (0.10, 0.24, 0.16, 1)
        _CloudColor ("Cloud", Color) = (0.88, 0.94, 1.0, 1)
        _AtmosphereColor ("Atmosphere", Color) = (0.12, 0.48, 1.0, 1)
        _SunsetColor ("Sunset", Color) = (1.0, 0.31, 0.08, 1)
        _SunDirection ("Sun Direction", Vector) = (-0.45, 0.28, -0.84, 0)
        _ObservationTime ("Observation Time", Float) = 0
        _DawnProgress ("Dawn Progress", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        Cull Back
        ZWrite On
        Pass
        {
            Name "BlueWorld"
            Tags { "LightMode"="SRPDefaultUnlit" }
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _OceanColor;
                half4 _LandColor;
                half4 _CloudColor;
                half4 _AtmosphereColor;
                half4 _SunsetColor;
                float4 _SunDirection;
                float _DawnProgress;
                float _ObservationTime;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 globe : TEXCOORD1;
                float3 viewWS : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float hash31(float3 p)
            {
                return frac(sin(dot(p, float3(127.1, 311.7, 74.7))) * 43758.5453);
            }
            float noise3(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float n000 = hash31(i);
                float n100 = hash31(i + float3(1,0,0));
                float n010 = hash31(i + float3(0,1,0));
                float n110 = hash31(i + float3(1,1,0));
                float n001 = hash31(i + float3(0,0,1));
                float n101 = hash31(i + float3(1,0,1));
                float n011 = hash31(i + float3(0,1,1));
                float n111 = hash31(i + 1.0);
                return lerp(lerp(lerp(n000,n100,f.x),lerp(n010,n110,f.x),f.y),
                            lerp(lerp(n001,n101,f.x),lerp(n011,n111,f.x),f.y),f.z);
            }
            float fbm(float3 p)
            {
                float v = 0.0;
                float a = 0.5;
                [unroll] for (int i = 0; i < 4; i++)
                {
                    v += noise3(p) * a;
                    p = p * 2.07 + 9.31;
                    a *= 0.5;
                }
                return v;
            }

            float ridged(float3 p)
            {
                float n = fbm(p);
                return 1.0 - abs(n * 2.0 - 1.0);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = position.positionCS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.globe = normalize(input.normalOS);
                output.viewWS = GetCameraPositionWS() - position.positionWS;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float3 n = normalize(input.normalWS);
                float3 v = normalize(input.viewWS);
                float3 sun = normalize(_SunDirection.xyz
                    + float3(_DawnProgress * 0.22, _DawnProgress * 0.10, 0.0));
                float sunDot = dot(n, sun);
                float daylight = smoothstep(-0.19, 0.14, sunDot);
                float broadLight = 0.10 + 0.90 * saturate(sunDot * 0.68 + 0.32);

                // Domain-warped continents build recognisable continental
                // masses, shelves, mountain chains and broken archipelagos.
                float3 globe = normalize(input.globe);
                float warpA = fbm(globe * 2.15 + float3(8.3, 17.1, 3.7));
                float warpB = fbm(globe * 2.65 + float3(31.7, 5.4, 12.2));
                float3 warped = globe + float3(warpA - 0.5, warpB - 0.5, warpA - warpB) * 0.36;
                float continent = fbm(warped * float3(2.7, 4.2, 2.7) + float3(1.2, 8.0, 4.5));
                continent = continent * 0.78 + fbm(warped * 7.8 + 19.0) * 0.22;
                float land = smoothstep(0.515, 0.59, continent);
                float shelf = smoothstep(0.475, 0.54, continent) - smoothstep(0.54, 0.59, continent);
                float mountain = ridged(warped * 18.0 + 7.2) * land;
                mountain *= smoothstep(0.45, 0.82, fbm(warped * 6.0 + 23.1));

                float latitude = abs(globe.y);
                float ice = smoothstep(0.76, 0.93, latitude) * (0.58 + fbm(warped * 13.0) * 0.42);
                float oceanVariation = fbm(globe * 9.0 + 6.8);
                float3 ocean = _OceanColor.rgb * lerp(0.64, 1.28, oceanVariation);
                ocean = lerp(ocean, float3(0.02, 0.34, 0.48), shelf * 0.74);
                float vegetation = smoothstep(0.28, 0.70, noise3(warped * 8.0 + 1.4));
                float3 dryLand = float3(0.33, 0.28, 0.14);
                float3 wetLand = _LandColor.rgb * 1.18;
                float3 landColor = lerp(dryLand, wetLand, vegetation);
                landColor = lerp(landColor, float3(0.34, 0.30, 0.25), mountain * 0.46);
                float3 surface = lerp(ocean, landColor, land);
                surface = lerp(surface, float3(0.80, 0.90, 0.97), ice * 0.88);

                // Two cloud scales move at different geological speeds. A
                // displaced dark copy gives the upper deck visible altitude.
                float time = _ObservationTime * 0.0014;
                float cloudBroad = fbm(globe * float3(6.0, 13.0, 6.0)
                    + float3(31.0 + time, 4.0, 17.0 - time * 0.7));
                float cloudDetail = ridged(globe * float3(17.0, 31.0, 17.0)
                    + float3(5.0 - time * 1.8, 23.0, 9.0));
                float cloud = smoothstep(0.57, 0.73, cloudBroad + cloudDetail * 0.18);
                cloud *= 0.72 + smoothstep(0.18, 0.82, cloudDetail) * 0.28;
                float shadowCloud = smoothstep(0.58, 0.73,
                    fbm((globe - sun * 0.018) * float3(6.0, 13.0, 6.0) + float3(31.0 + time, 4.0, 17.0)));
                surface *= 1.0 - shadowCloud * daylight * 0.16;
                surface = lerp(surface, _CloudColor.rgb * (0.78 + broadLight * 0.34), cloud * daylight * 0.88);

                // Ocean glint and clustered coastal civilisation make the
                // horizon feel inhabited without becoming a city-light map.
                float3 halfVector = normalize(sun + v);
                float oceanGlint = pow(saturate(dot(n, halfVector)), 54.0) * (1.0 - land) * daylight;
                float coast = saturate((smoothstep(0.49, 0.55, continent) - smoothstep(0.56, 0.62, continent)) * 2.0);
                float cityCells = noise3(globe * 118.0 + 7.0) * noise3(globe * 47.0 + 29.0);
                float nightLights = smoothstep(0.58, 0.79, cityCells) * land * (0.35 + coast) * (1.0 - daylight);

                float rim = pow(1.0 - saturate(dot(n, v)), 2.75);
                float terminator = 1.0 - smoothstep(0.0, 0.16, abs(sunDot + 0.035));
                float3 color = surface * broadLight * daylight;
                color += float3(0.35, 0.72, 1.0) * oceanGlint * 1.8;
                color += float3(1.0, 0.48, 0.12) * nightLights * 2.2;
                color += _AtmosphereColor.rgb * rim * (0.36 + daylight * 1.32);
                color += _SunsetColor.rgb * rim * terminator * 1.55;
                float dawnReach = rim * smoothstep(-0.24, 0.22, sunDot);
                color += _SunsetColor.rgb * dawnReach * _DawnProgress * 0.72;
                color += float3(1.0, 0.76, 0.48) * daylight * _DawnProgress * 0.055;
                color = 1.0 - exp(-color * 1.42);
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
