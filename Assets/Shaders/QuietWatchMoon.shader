Shader "StarshipCabin/QuietWatchMoon"
{
    Properties
    {
        _DistanceScale ("Physical Distance Scale", Float) = 1
        _DistanceOrigin ("Distance Reference Eye", Vector) = (-1.6,1.1,-1.42,0)
        _RingCenter ("Ring Center", Vector) = (8,6,-76,0)
        _RingNormal ("Ring Plane Normal", Vector) = (0,1,0,0)
        _RingRadii ("Ring Inner / Outer Radius", Vector) = (31.5,44,0,0)
        _PlanetSphere ("Planet Center / Radius", Vector) = (8,6,-76,29)

        _Highland ("Highland", Color) = (0.48, 0.43, 0.36, 1)
        _Maria ("Maria", Color) = (0.16, 0.15, 0.15, 1)
        _SunDirection ("Sun Direction", Vector) = (-0.42, 0.52, 0.74, 0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        Cull Back
        ZWrite On
        Pass
        {
            Name "AuthoredMoon"
            Tags { "LightMode"="SRPDefaultUnlit" }
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #include "QuietWatchWeatherCommon.hlsl"

            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings
            {
                float4 positionCS:SV_POSITION;
                float3 normalWS:TEXCOORD0;
                float3 globe:TEXCOORD1;
                float3 positionWS:TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float hash31(float3 p)
            {
                return frac(sin(dot(p, float3(127.1, 311.7, 74.7))) * 43758.5453);
            }

            float noise3(float3 p)
            {
                float3 i=floor(p), f=frac(p);
                f=f*f*(3.0-2.0*f);
                float n000=hash31(i), n100=hash31(i+float3(1,0,0));
                float n010=hash31(i+float3(0,1,0)), n110=hash31(i+float3(1,1,0));
                float n001=hash31(i+float3(0,0,1)), n101=hash31(i+float3(1,0,1));
                float n011=hash31(i+float3(0,1,1)), n111=hash31(i+1.0);
                return lerp(lerp(lerp(n000,n100,f.x),lerp(n010,n110,f.x),f.y),
                    lerp(lerp(n001,n101,f.x),lerp(n011,n111,f.x),f.y),f.z);
            }

            float crater(float3 p, float3 center, float radius)
            {
                float distanceToCenter=length(p-normalize(center));
                float rim=smoothstep(radius*0.19,0.0,abs(distanceToCenter-radius));
                float bowl=(1.0-smoothstep(0.0,radius,distanceToCenter))*0.72;
                return rim-bowl;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                VertexPositionInputs p=GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS=TransformWorldToHClip(QWProjectionPosition(p.positionWS));
                output.normalWS=TransformObjectToWorldNormal(input.normalOS);
                output.globe=normalize(input.normalOS);
                output.positionWS=p.positionWS;
                return output;
            }

            half4 frag(Varyings input):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float3 globe=normalize(input.globe);
                float terrain=noise3(globe*7.0+3.1)*0.68+noise3(globe*21.0+19.0)*0.32;
                float maria=smoothstep(0.56,0.73,noise3(globe*3.4+8.7));
                float relief=0.0;
                relief+=crater(globe,float3(-0.36,0.24,0.90),0.23);
                relief+=crater(globe,float3(0.22,-0.16,0.96),0.14)*0.82;
                relief+=crater(globe,float3(0.48,0.34,0.81),0.10)*0.70;
                relief+=crater(globe,float3(-0.08,-0.48,0.88),0.08)*0.62;

                float3 surface=lerp(_Highland.rgb,_Maria.rgb,maria*0.72);
                surface*=0.72+terrain*0.43;
                surface*=1.0+relief*0.42;

                float3 n=normalize(input.normalWS);
                float3 v=QWViewDirection(input.positionWS);
                float light=smoothstep(-0.10,0.20,dot(n,normalize(_SunDirection.xyz)));
                float rim=pow(1.0-saturate(dot(n,v)),2.2);
                float transmission=QWRingTransmission(input.positionWS)*QWPlanetTransmission(input.positionWS);
                float3 color=surface*(0.035+light*transmission*1.08);
                color+=float3(0.22,0.16,0.11)*rim*light*transmission*0.18;
                color=1.0-exp(-color*1.55);
                return half4(color,1.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
