Shader "StarshipCabin/QuietWatchCruiseStars"
{
    Properties
    {
        _Travel ("Integrated translation", Float) = 0
        _Sector ("Passed sectors", Float) = 0
        _WrapWidth ("Sector width", Float) = 131072
        _DepthRange ("Transverse proxy radius", Vector) = (8000,65536,0,0)
        _ReviewStar ("Editor motion audit only", Float) = -1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-30" "RenderType"="Transparent" }
        Blend One One
        ZWrite Off
        Cull Off
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float _Travel, _Sector, _WrapWidth, _ReviewStar;
                float4 _DepthRange;
            CBUFFER_END
            struct A { float4 p:POSITION; float2 uv:TEXCOORD0; float3 data:TEXCOORD1;
                float4 color:COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 p:SV_POSITION; float2 pixel:TEXCOORD0; float sigma:TEXCOORD1;
                half3 energy:TEXCOORD2; half halo:TEXCOORD3; UNITY_VERTEX_OUTPUT_STEREO };
            uint mixBits(uint v)
            {
                v^=v>>16; v*=0x7feb352du; v^=v>>15; v*=0x846ca68bu; return v^(v>>16);
            }
            float unitHash(uint v) { return (mixBits(v)&0xffffffu)/16777216.0; }
            V vert(A a)
            {
                V v; UNITY_SETUP_INSTANCE_ID(a); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(v);
                // The desk/ship bow is -X. Space moves +X relative to the cabin.
                // Facing the window (-Z), camera-right is -X: this projects LEFT.
                float cellScale=max(.5,a.data.z);
                float period=_WrapWidth*cellScale;
                float localCycle=floor((a.p.x+_Travel+period*.5)/period);
                float3 stellarPosition=a.p.xyz;
                stellarPosition.x=a.p.x+_Travel-localCycle*period;
                // Both cell periods divide the global period exactly, preserving phase at rollover.
                uint cycle=(uint)(_Sector/cellScale+localCycle);
                if(cycle!=0u)
                {
                    uint seed=(uint)(a.data.y+1)^cycle*0x9e3779b9u;
                    float radius=sqrt(lerp(_DepthRange.x*_DepthRange.x,
                        _DepthRange.y*_DepthRange.y*a.data.z*a.data.z,unitHash(seed)));
                    float angle=unitHash(seed+1u)*6.28318530718;
                    stellarPosition.y=sin(angle)*radius; stellarPosition.z=cos(angle)*radius;
                }
                float fade=1-smoothstep(period*.40,period*.5,abs(stellarPosition.x));
                // Project virtual stellar distance without far-plane clipping. Per-eye rays
                // retain correct head tracking with negligible room-scale stellar parallax.
                float3 eye=GetCameraPositionWS();
                float3 ray=normalize(stellarPosition-eye);
                v.p=TransformWorldToHClip(eye+ray*12000);
                v.sigma=sqrt(a.data.x*a.data.x+.18);
                v.halo=smoothstep(1.6,3.5,a.color.a)*.035;
                float extent=v.sigma*lerp(3.5,7,step(.001,v.halo));
                v.p.xy+=a.uv*extent*2/_ScaledScreenParams.xy*v.p.w;
                v.pixel=a.uv*extent;
                // Energy-preserving filtered cores: no twinkle, pulses, spikes or stationary layer.
                v.energy=a.color.rgb*a.color.a*fade/(v.sigma*v.sigma);
                if(_ReviewStar>=0) { v.energy=abs(a.data.y-_ReviewStar)<.1 ? half3(1,1,1) : half3(0,0,0); v.halo=0; }
                return v;
            }
            half4 frag(V v):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(v);
                float core=exp(-.5*dot(v.pixel,v.pixel)/(v.sigma*v.sigma));
                float halo=exp(-.5*dot(v.pixel,v.pixel)/(v.sigma*v.sigma*6.25))*v.halo;
                return half4(1-exp(-v.energy*(core+halo)),0);
            }
            ENDHLSL
        }
    }
}
