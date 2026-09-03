Shader "StarshipCabin/QuietWatchGasGiant"
{
    Properties
    {
        _PaleBand ("Pale Band", Color) = (0.82, 0.62, 0.38, 1)
        _DarkBand ("Dark Band", Color) = (0.24, 0.10, 0.08, 1)
        _StormColor ("Storm", Color) = (0.86, 0.30, 0.12, 1)
        _SunDirection ("Sun Direction", Vector) = (-0.62, 0.30, -0.72, 0)
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
            float hash21(float2 p) { return frac(sin(dot(p,float2(127.1,311.7))) * 43758.5453); }
            float noise2(float2 p)
            {
                float2 i=floor(p), f=frac(p); f=f*f*(3.0-2.0*f);
                return lerp(lerp(hash21(i),hash21(i+float2(1,0)),f.x),lerp(hash21(i+float2(0,1)),hash21(i+1.0),f.x),f.y);
            }
            float fbm(float2 p)
            {
                float v=0.0,a=0.5;
                [unroll] for(int i=0;i<4;i++){v+=noise2(p)*a;p=p*2.03+11.7;a*=0.5;}
                return v;
            }
            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                VertexPositionInputs p=GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS=p.positionCS;
                output.normalWS=TransformObjectToWorldNormal(input.normalOS);
                output.globe=normalize(input.normalOS);
                output.viewWS=GetCameraPositionWS()-p.positionWS;
                return output;
            }
            half4 frag(Varyings input):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float3 n=normalize(input.normalWS), v=normalize(input.viewWS), sun=normalize(_SunDirection.xyz);
                float longitude=atan2(input.globe.x,-input.globe.z)*0.15915494+0.5;
                float latitude=asin(clamp(input.globe.y,-1.0,1.0))*0.31830989+0.5;
                float warp=fbm(float2(longitude*5.0,latitude*16.0)+4.2)-0.5;
                float bands=0.5+0.5*sin((latitude+warp*0.032)*92.0 + sin(longitude*12.0)*0.8);
                bands=smoothstep(0.18,0.82,bands);
                float3 color=lerp(_DarkBand.rgb,_PaleBand.rgb,bands);

                float2 stormDelta=float2((longitude-0.61)*2.0,latitude-0.43);
                float storm=smoothstep(0.085,0.020,length(stormDelta))*smoothstep(0.65,0.30,abs(stormDelta.y));
                storm*=0.65+0.35*sin(atan2(stormDelta.y,stormDelta.x)*5.0+length(stormDelta)*90.0);
                color=lerp(color,_StormColor.rgb,storm*0.86);

                float light=smoothstep(-0.17,0.18,dot(n,sun));
                float rim=pow(1.0-saturate(dot(n,v)),2.7);
                color*=0.14+light*1.02;
                color+=float3(0.42,0.22,0.13)*rim*(0.25+light)*0.52;
                color=1.0-exp(-color*1.55);
                return half4(color,1.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
