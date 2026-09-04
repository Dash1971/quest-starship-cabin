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
            float hash11(float p) { return frac(sin(p * 127.1) * 43758.5453); }
            float noise1(float p)
            {
                float i=floor(p), f=frac(p);
                f=f*f*(3.0-2.0*f);
                return lerp(hash11(i),hash11(i+1.0),f);
            }
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
                float radialWarp=(noise1(radius*0.72+3.8)-0.5)*0.72;
                float broad=0.5+0.5*sin(radius*1.83+radialWarp+sin(radius*0.29)*1.25);
                float medium=0.5+0.5*sin(radius*6.4+noise1(radius*1.9)*2.1);
                float fine=0.5+0.5*sin(radius*19.7+sin(radius*4.7)*0.62);
                float dust=noise1(radius*11.3+17.0);
                float structure=saturate(broad*0.50+medium*0.27+fine*0.13+dust*0.10);

                // Several narrow divisions and broad low-density regions break
                // the vinyl-record regularity while preserving readable rings.
                float divisions=smoothstep(0.035,0.13,abs(sin(radius*0.91+0.7)));
                divisions*=smoothstep(0.025,0.10,abs(sin(radius*2.37+1.9)));
                float gaps=smoothstep(0.15,0.34,broad*0.52+medium*0.28+fine*0.10+dust*0.10);
                gaps*=lerp(0.42,1.0,divisions);
                float3 color=lerp(_DarkColor.rgb,_LightColor.rgb,structure);
                color*=0.76+fine*0.19+dust*0.13;
                float alpha=(0.10+structure*0.42)*gaps;
                return half4(color,alpha);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
