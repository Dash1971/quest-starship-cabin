Shader "StarshipCabin/QuietWatchRings"
{
    Properties
    {
        _LightColor ("Ice and Dust", Color) = (0.72, 0.55, 0.36, 1)
        _DarkColor ("Rock Bands", Color) = (0.16, 0.10, 0.08, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            Name "AuthoredRingBands"
            Tags { "LightMode"="SRPDefaultUnlit" }
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _LightColor;
                half4 _DarkColor;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS=TransformObjectToHClip(input.positionOS.xyz);
                output.uv=input.uv;
                return output;
            }
            half4 frag(Varyings input):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float radius=length(input.uv);
                float broad=0.5+0.5*sin(radius*2.4+sin(radius*0.37)*1.8);
                float fine=0.5+0.5*sin(radius*13.0);
                float gaps=smoothstep(0.12,0.30,broad*0.72+fine*0.28);
                float3 color=lerp(_DarkColor.rgb,_LightColor.rgb,broad*0.72+fine*0.18);
                float alpha=0.20+gaps*0.42;
                return half4(color,alpha);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
