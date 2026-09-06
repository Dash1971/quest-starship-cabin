Shader "StarshipCabin/QuietWatchCloudDeck"
{
    Properties
    {
        _CloudMap ("Cloud structure", 2D) = "black" {}
        _SunDirection ("Sun", Vector) = (-.82,.12,.24,0)
        _DistanceScale ("Physical scale", Float) = 1
        _DistanceOrigin ("Reference eye", Vector) = (-1.6,1.1,-1.42,0)
        _PlanetSphere ("Planet", Vector) = (0,0,0,1)
        _ObservationTime ("Time", Float) = 0
        _DawnProgress ("Dawn", Float) = 0
        _AuroraOnly ("Auroral layer", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back
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
                float4 _SunDirection, _DistanceOrigin, _PlanetSphere;
                float _DistanceScale, _ObservationTime, _DawnProgress, _AuroraOnly;
            CBUFFER_END
            #include "QuietWatchDistance.hlsl"
            TEXTURE2D(_CloudMap); SAMPLER(sampler_CloudMap);
            struct A { float4 p:POSITION;float3 n:NORMAL;UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 p:SV_POSITION;float3 n:TEXCOORD0;float3 globe:TEXCOORD1;float3 world:TEXCOORD2;UNITY_VERTEX_OUTPUT_STEREO };
            V vert(A a)
            {
                V v;UNITY_SETUP_INSTANCE_ID(a);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(v);
                v.world=TransformObjectToWorld(a.p.xyz);v.p=TransformWorldToHClip(QWProjectionPosition(v.world));
                v.n=TransformObjectToWorldNormal(a.n);v.globe=a.n;return v;
            }
            float2 uvFor(float3 n) { return float2(atan2(n.x,-n.z)*.15915494+.5,asin(clamp(n.y,-1,1))*.31830989+.5); }
            half4 frag(V v):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(v);
                float3 n=normalize(v.n),globe=normalize(v.globe);
                float2 uv=uvFor(globe),dx=ddx(uv),dy=ddy(uv);dx.x-=round(dx.x);dy.x-=round(dy.x);
                float3 sun=QWDawnSun(_SunDirection.xyz,_DawnProgress);
                float light=dot(n,sun);
                float2 flow=float2(_ObservationTime*.0000018,0);
                float3 clouds=SAMPLE_TEXTURE2D_GRAD(_CloudMap,sampler_CloudMap,uv+flow,dx,dy).rgb;
                if (_AuroraOnly>.5)
                {
                    // One slowly moving, bounded oval. Derivative filtering keeps
                    // folds stable in stereo; no flashes or global shader time.
                    float longitude=uv.x*6.2831853;
                    float wave=sin(longitude*7+_ObservationTime*.006)*.003+sin(longitude*17)*.0015;
                    float oval=abs(globe.y)-(.94+wave);
                    float width=max(.006,fwidth(oval)*1.5);
                    float ribbon=exp(-pow(oval/width,2));
                    float folds=.5+.5*sin(longitude*117+clouds.g*9+_ObservationTime*.009);
                    folds=lerp(folds,.5,saturate(fwidth(longitude*117)/3.14));
                    float night=1-smoothstep(-.15,.18,light);
                    float intensity=ribbon*(.35+folds*.65)*night;
                    float3 color=lerp(float3(.12,1.45,.55),float3(.65,.18,.95),smoothstep(-.004,.008,oval));
                    return half4(color,intensity*.72);
                }
                float2 eastUv=uv+flow+float2(.00075,0);
                float3 neighbour=SAMPLE_TEXTURE2D_GRAD(_CloudMap,sampler_CloudMap,eastUv,dx,dy).rgb;
                float relief=clamp((neighbour.g-clouds.g)*12,-.28,.28);
                float daylight=smoothstep(-.07,.12,light);
                float twilight=exp(-pow((light+.015)/.13,2));
                float3 tint=lerp(float3(.94,.97,1),float3(1,.39,.12),twilight*.82);
                float3 color=tint*(.018+daylight*(.20+max(0,light+relief)*.93));
                float alpha=smoothstep(.10,.78,clouds.r)*.98;
                return half4(color,alpha);
            }
            ENDHLSL
        }
    }
}
