Shader "StarshipCabin/QuietWatchDistantComet"
{
    Properties
    {
        _CometPosition ("Distant nucleus and visibility", Vector) = (12000,7000,-36000,0)
        _Extent ("Angular tail length and half width", Vector) = (.10472,.011345,0,0)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-29" "RenderType"="Transparent" }
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
                float4 _CometPosition, _Extent;
            CBUFFER_END
            struct A { float4 p:POSITION; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 p:SV_POSITION; float2 angle:TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
            V vert(A a)
            {
                V v; UNITY_SETUP_INSTANCE_ID(a); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(v);
                float3 eye=GetCameraPositionWS(); float3 ray=normalize(_CometPosition.xyz-eye);
                // An anti-sun direction in world space, not a screen-locked speed trail.
                float3 antiSun=normalize(float3(-1,.24,-.08));
                float3 along=normalize(antiSun-ray*dot(antiSun,ray)); float3 across=normalize(cross(ray,along));
                v.angle=float2((a.uv.x*.55+.45)*_Extent.x,a.uv.y*_Extent.y);
                float3 direction=normalize(ray+along*v.angle.x+across*v.angle.y);
                v.p=TransformWorldToHClip(eye+direction*12000);
                return v;
            }
            half4 frag(V v):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(v);
                float u=v.angle.x/_Extent.x;
                float footprint=max(length(ddx(v.angle)),length(ddy(v.angle)))*.45;
                // A resolved coma and two readable tails; the former 2.1-degree,
                // subpixel nucleus disappeared behind glass and headset filtering.
                float sigma=sqrt(.0012*.0012+footprint*footprint);
                float coma=exp(-.5*dot(v.angle,v.angle)/(sigma*sigma))*.0012*.0012/(sigma*sigma);
                float envelope=exp(-.5*dot(v.angle,v.angle)/(.003*.003))*.14;
                float width=.0009+saturate(u)*.006;
                width=sqrt(width*width+footprint*footprint);
                float fan=exp(-.5*pow((v.angle.y+.0025*u*u)/width,2));
                float tail=fan*exp(-max(0,u)*1.6)*smoothstep(0,.025,u)*(1-smoothstep(.65,1,u));
                float ion=exp(-.5*pow(v.angle.y/max(.00045,footprint),2))*exp(-max(0,u)*1.4)
                    *smoothstep(0,.05,u)*(1-smoothstep(.8,1,u));
                float3 color=(coma+envelope)*float3(2.0,2.15,2.3)
                    +tail*float3(.42,.38,.29)+ion*float3(.055,.12,.21);
                return half4(color*_CometPosition.w,0);
            }
            ENDHLSL
        }
    }
}
