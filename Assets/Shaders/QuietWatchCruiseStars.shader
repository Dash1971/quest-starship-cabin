Shader "StarshipCabin/QuietWatchCruiseStars"
{
    Properties { _Travel ("Integrated travel", Float) = 0 }
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
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float _Travel;
            CBUFFER_END
            struct A { float4 p:POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 p:SV_POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;UNITY_VERTEX_OUTPUT_STEREO };
            V vert(A a)
            {
                V v;UNITY_SETUP_INSTANCE_ID(a);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(v);
                float distance=180+frac((a.p.z-_Travel)/6000)*6000;
                float3 world=float3(a.p.xy+float2(-1.6,1.1),-1.42-distance);
                v.p=TransformWorldToHClip(world);
                // Per-eye expansion keeps the core circular and roughly 1–2 pixels.
                float pixels=1.3+a.color.a*.65;
                v.p.xy+=a.uv*pixels*2/_ScaledScreenParams.xy*v.p.w;
                v.uv=a.uv;
                float fade=smoothstep(180,650,distance)*(1-smoothstep(5350,6180,distance));
                v.color=half4(a.color.rgb,fade*a.color.a);
                return v;
            }
            half4 frag(V v):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(v);
                float r=length(v.uv);
                float core=exp(-r*r*5)*(1-smoothstep(.65,1,r));
                return half4(v.color.rgb*core*v.color.a*1.7,0);
            }
            ENDHLSL
        }
    }
}
