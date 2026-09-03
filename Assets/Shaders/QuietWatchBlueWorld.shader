Shader "StarshipCabin/QuietWatchBlueWorld"
{
    Properties
    {
        _OceanColor ("Ocean", Color) = (0.015, 0.12, 0.28, 1)
        _LandColor ("Land", Color) = (0.10, 0.24, 0.16, 1)
        _CloudColor ("Cloud", Color) = (0.88, 0.94, 1.0, 1)
        _SunDirection ("Sun Direction", Vector) = (-0.45, 0.28, -0.84, 0)
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
                float4 _SunDirection;
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
                float3 sun = normalize(_SunDirection.xyz);
                float daylight = smoothstep(-0.16, 0.18, dot(n, sun));
                float broadLight = 0.08 + 0.92 * saturate(dot(n, sun) * 0.72 + 0.28);

                float continent = fbm(input.globe * float3(3.2, 5.8, 3.2) + float3(1.2, 8.0, 4.5));
                continent += 0.22 * fbm(input.globe * 11.0 + 19.0);
                float land = smoothstep(0.54, 0.66, continent);
                float3 surface = lerp(_OceanColor.rgb, _LandColor.rgb, land);

                float cloudNoise = fbm(input.globe * float3(7.0, 16.0, 7.0) + float3(31.0, 4.0, 17.0));
                float cloud = smoothstep(0.61, 0.76, cloudNoise) * daylight;
                surface = lerp(surface, _CloudColor.rgb, cloud * 0.78);

                float nightLights = smoothstep(0.68, 0.80, noise3(input.globe * 82.0 + 7.0)) * land * (1.0 - daylight);
                float rim = pow(1.0 - saturate(dot(n, v)), 3.1);
                float horizonDay = smoothstep(-0.24, 0.10, dot(n, sun));
                float3 color = surface * broadLight * daylight;
                color += float3(1.0, 0.55, 0.18) * nightLights * 1.5;
                color += lerp(float3(0.10, 0.24, 0.65), float3(0.35, 0.72, 1.0), horizonDay) * rim * 1.8;
                color = 1.0 - exp(-color * 1.35);
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
