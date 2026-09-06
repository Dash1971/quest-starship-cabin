Shader "StarshipCabin/QuietWatchHull"
{
    Properties
    {
        _BaseMap ("Hull finish", 2D) = "white" {}
        [Normal] _BumpMap ("Panel relief", 2D) = "bump" {}
        _MetallicGlossMap ("Metal and roughness", 2D) = "white" {}
        _OcclusionMap ("Surface occlusion", 2D) = "white" {}
        _Metallic ("Metal", Float) = 0
        _Smoothness ("Smoothness", Float) = .2
        _BaseColor ("Hull tint", Color) = (1,1,1,1)
        _EmissionMap ("Windows", 2D) = "white" {}
        _EmissionColor ("Window light", Color) = (0,0,0,1)
        _SunDirection ("Sun", Vector) = (-0.5,0.5,0.5,0)
        _SunColor ("Sunlight", Color) = (1.2,1.1,0.91,1)
        _FixedShadow ("Baked fixed structure shadow", Float) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST, _SunDirection;
                half4 _BaseColor, _EmissionColor, _SunColor;
                float _FixedShadow, _Metallic, _Smoothness;
            CBUFFER_END
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_MetallicGlossMap); SAMPLER(sampler_MetallicGlossMap);
            TEXTURE2D(_OcclusionMap); SAMPLER(sampler_OcclusionMap);
            TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);
            TEXTURE2D(_EmissionMap); SAMPLER(sampler_EmissionMap);
            struct A { float4 p:POSITION; float3 n:NORMAL; float2 uv:TEXCOORD0; half4 color:COLOR; float4 t:TANGENT; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 p:SV_POSITION; float3 n:TEXCOORD0; float2 uv:TEXCOORD1; float3 world:TEXCOORD2; half2 occlusion:TEXCOORD3; float4 t:TEXCOORD4; UNITY_VERTEX_OUTPUT_STEREO };
            V vert(A a)
            {
                V v; UNITY_SETUP_INSTANCE_ID(a); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(v);
                v.world=TransformObjectToWorld(a.p.xyz);v.p=TransformWorldToHClip(v.world);
                v.t=float4(TransformObjectToWorldDir(a.t.xyz,false),a.t.w*GetOddNegativeScale());
                v.n=TransformObjectToWorldNormal(a.n);v.uv=TRANSFORM_TEX(a.uv,_BaseMap);v.occlusion=a.color.rg;
                return v;
            }
            half4 frag(V v):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(v);
                float3 n=normalize(v.n), sun=normalize(_SunDirection.xyz);
                float3 tangent=normalize(v.t.xyz+float3(0.000001,0,0));
                float3 bitangent=cross(n,tangent)*v.t.w;
                float3 detail=UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,v.uv),.65);
                n=normalize(n*detail.z+tangent*detail.x+bitangent*detail.y);
                float3 view=normalize(GetCameraPositionWS()-v.world);
                float diffuse=saturate(dot(n,sun))*lerp(1,v.occlusion.r,_FixedShadow);
                float ao=v.occlusion.g*lerp(1.0,SAMPLE_TEXTURE2D(_OcclusionMap,sampler_OcclusionMap,v.uv).g,.82);
                float4 finish=SAMPLE_TEXTURE2D(_MetallicGlossMap,sampler_MetallicGlossMap,v.uv);
                float metal=saturate(finish.r*_Metallic), gloss=saturate(finish.a*_Smoothness);
                float3 base=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,v.uv).rgb*_BaseColor.rgb;
                float3 ambient=lerp(float3(.028,.036,.055),float3(.09,.13,.19),saturate(n.y*.5+.5))*ao;
                float3 color=base*(ambient+_SunColor.rgb*diffuse*1.15);
                float broadSpec=pow(saturate(dot(n,normalize(sun+view))),8+72*gloss*gloss)*diffuse*ao*.28;
                color+=_SunColor.rgb*lerp(float3(.12,.12,.12),base,metal)*broadSpec;
                color+=SAMPLE_TEXTURE2D(_EmissionMap,sampler_EmissionMap,v.uv).rgb*_EmissionColor.rgb;
                return half4(color,1);
            }
            ENDHLSL
        }
    }
}
