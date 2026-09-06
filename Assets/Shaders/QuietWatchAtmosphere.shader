Shader "StarshipCabin/QuietWatchAtmosphere"
{
    Properties
    {
        _DistanceScale ("Physical Distance Scale", Float) = 1
        _DistanceOrigin ("Distance Reference Eye", Vector) = (-1.6,1.1,-1.42,0)
        _PlanetSphere ("Planet Center / Radius", Vector) = (0,0,0,1)
        _SunDirection ("Sun Direction", Vector) = (-0.8,0.1,0.3,0)
        _AtmosphereColor ("Scattered Light", Color) = (0.12,0.42,1,1)
        _AtmosphereHeight ("Scale Height / Radius", Float) = 0.005
        _DawnProgress ("Dawn Progress", Range(0,1)) = 0
        _RingCenter ("Ring Center", Vector) = (0,0,0,0)
        _RingNormal ("Ring Normal", Vector) = (0,1,0,0)
        _RingRadii ("Ring Radii", Vector) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-20" "RenderType"="Transparent" }
        // Back surface supplies one fragment per limb pixel; opaque planet
        // depth hides its interior. No fullscreen effect or volumetric raymarch.
        Cull Front
        ZWrite Off
        Blend One OneMinusSrcAlpha
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "QuietWatchWeatherCommon.hlsl"
            struct Attributes { float4 positionOS:POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS:SV_POSITION; float3 proxyWS:TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.proxyWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(QWProjectionPosition(output.proxyWS));
                return output;
            }
            half4 frag(Varyings input):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float3 eye = _DistanceOrigin.xyz + (GetCameraPositionWS()-_DistanceOrigin.xyz)/max(1.0,_DistanceScale);
                float3 ray = normalize(input.proxyWS-eye);
                float3 toPlanet = _PlanetSphere.xyz-eye;
                float3 tangent = ray*dot(toPlanet,ray)-toPlanet;
                float altitude = (length(tangent)-_PlanetSphere.w)/_PlanetSphere.w;
                float height = max(0.0001,_AtmosphereHeight);
                float density = exp(-max(0.0,altitude)/height);
                density *= 1.0-smoothstep(height*5.0,height*8.0,altitude);
                float3 sun = QWDawnSun(_SunDirection.xyz,_DawnProgress);
                float incidence = dot(normalize(tangent),sun);
                float day = smoothstep(-0.12,0.28,incidence);
                float twilight = exp(-pow((incidence+0.015)/0.10,2.0));
                float alpha = density*(0.012+day*0.58+twilight*0.22);
                float3 light = lerp(_AtmosphereColor.rgb,float3(1.0,0.27,0.055),twilight*0.68);
                return half4(light*alpha*1.55,alpha);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
