Shader "StarshipCabin/QuietWatchRings"
{
    Properties
    {
        _DistanceScale ("Physical Distance Scale", Float) = 1
        _DistanceOrigin ("Distance Reference Eye", Vector) = (-1.6,1.1,-1.42,0)
        _RingCenter ("Ring Center", Vector) = (8,6,-76,0)
        _RingNormal ("Ring Plane Normal", Vector) = (0,1,0,0)
        _RingRadii ("Ring Inner / Outer Radius", Vector) = (31.5,44,0,0)
        _PlanetSphere ("Planet Center / Radius", Vector) = (8,6,-76,29)

        _SunDirection ("Sun Direction", Vector) = (-0.62,0.1,0.78,0)
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
            #include "QuietWatchWeatherCommon.hlsl"
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionWS=TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS=TransformWorldToHClip(QWProjectionPosition(output.positionWS));
                return output;
            }
            half4 frag(Varyings input):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float radius=length(input.positionWS-_RingCenter.xyz);
                float density=QWRingDensity(radius);
                float3 color=lerp(_DarkColor.rgb,_LightColor.rgb,density);
                color*=0.12+0.88*QWPlanetTransmission(input.positionWS)
                    *(0.3+0.7*abs(dot(normalize(_RingNormal.xyz),normalize(_SunDirection.xyz))));
                return half4(color,density*0.68);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
