Shader "StarshipCabin/QuietWatchBlueWorld"
{
    Properties
    {
        _SurfaceMap ("Original Terrain / Land Mask", 2D) = "white" {}
        _CloudMap ("Cloud Density / Relief / Cities", 2D) = "black" {}
        _AtmosphereColor ("Atmosphere", Color) = (0.12,0.42,1,1)
        _SunsetColor ("Sunset", Color) = (1,0.32,0.08,1)
        _SunDirection ("Sun Direction", Vector) = (-0.82,0.12,0.24,0)
        _DistanceScale ("Physical Distance Scale", Float) = 1
        _DistanceOrigin ("Distance Reference Eye", Vector) = (-1.6,1.1,-1.42,0)
        _PlanetSphere ("Planet Center / Radius", Vector) = (0,0,0,1)
        _ObservationTime ("Observation Time", Float) = 0
        _DawnProgress ("Dawn Progress", Range(0,1)) = 0
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
                half4 _AtmosphereColor, _SunsetColor;
                float4 _SunDirection, _DistanceOrigin, _PlanetSphere;
                float _DistanceScale, _ObservationTime, _DawnProgress;
            CBUFFER_END
            #include "QuietWatchDistance.hlsl"
            TEXTURE2D(_SurfaceMap); SAMPLER(sampler_SurfaceMap);
            TEXTURE2D(_CloudMap); SAMPLER(sampler_CloudMap);
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings
            {
                float4 positionCS:SV_POSITION;
                float3 normalWS:TEXCOORD0;
                float3 globe:TEXCOORD1;
                float3 proxyWS:TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            float2 globeUv(float3 globe)
            {
                globe=normalize(globe);
                return float2(atan2(globe.x,-globe.z)*0.15915494+0.5,asin(clamp(globe.y,-1.0,1.0))*0.31830989+0.5);
            }
            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.proxyWS=TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS=TransformWorldToHClip(QWProjectionPosition(output.proxyWS));
                output.normalWS=TransformObjectToWorldNormal(input.normalOS);
                output.globe=input.normalOS;
                return output;
            }
            half4 frag(Varyings input):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float3 n=normalize(input.normalWS), globe=normalize(input.globe);
                float3 v=QWViewDirection(input.proxyWS);
                float3 sun=QWDawnSun(_SunDirection.xyz,_DawnProgress);
                float sunDot=dot(n,sun);
                float daylight=smoothstep(-0.06,0.12,sunDot);
                float2 uv=globeUv(globe);
                float2 dx=ddx(uv),dy=ddy(uv);
                dx.x-=round(dx.x); dy.x-=round(dy.x);
                float4 terrain=SAMPLE_TEXTURE2D_GRAD(_SurfaceMap,sampler_SurfaceMap,uv,dx,dy);
                float3 fixedWeather=SAMPLE_TEXTURE2D_GRAD(_CloudMap,sampler_CloudMap,uv,dx,dy).rgb;
                float2 cloudUv=uv+float2(_ObservationTime*0.0000018,0);
                float3 weather=SAMPLE_TEXTURE2D_GRAD(_CloudMap,sampler_CloudMap,cloudUv,dx,dy).rgb;
                float3 sunOS=TransformWorldToObjectDir(sun);
                float2 shadowUv=globeUv(globe+sunOS*0.004/max(sunDot,0.15))+float2(_ObservationTime*0.0000018,0);
                float shadow=SAMPLE_TEXTURE2D_GRAD(_CloudMap,sampler_CloudMap,shadowUv,dx,dy).r;
                float diffuse=0.014+max(0.0,sunDot)*1.2;
                float3 surface=terrain.rgb*diffuse*(1.0-shadow*daylight*0.42);
                float cloud=weather.r;
                float twilight=exp(-pow((sunDot+0.01)/0.11,2.0));
                float3 cloudColor=lerp(float3(0.91,0.96,1.0),_SunsetColor.rgb,twilight*0.62);
                cloudColor*=0.017+daylight*(0.30+0.70*saturate(sunDot))*(0.75+weather.g*0.45);
                surface=lerp(surface,cloudColor,cloud*0.94);
                float glint=pow(saturate(dot(n,normalize(sun+v))),80.0)*(1.0-terrain.a)*(1.0-cloud)*daylight;
                surface+=float3(1.0,0.78,0.48)*glint*0.65;
                surface+=float3(1.0,0.49,0.14)*fixedWeather.b*(1.0-daylight)*(1.0-cloud*0.75)*2.4;
                float rim=pow(1.0-saturate(dot(n,v)),3.8);
                surface+=_AtmosphereColor.rgb*rim*(0.025+daylight*0.48);
                surface+=_SunsetColor.rgb*rim*twilight*0.55;
                return half4(surface,1.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
