Shader "StarshipCabin/QuietWatchGasGiant"
{
    Properties
    {
        _CompanionSphere ("Companion Moon Center / Radius", Vector) = (0,0,0,0)
        _OccultorSphere ("Eclipse Moon Center / Radius", Vector) = (0,0,0,0)
        _SolarAngularRadius ("Solar Angular Radius", Float) = 0.00465
        _DistanceScale ("Physical Distance Scale", Float) = 1
        _DistanceOrigin ("Distance Reference Eye", Vector) = (-1.6,1.1,-1.42,0)
        _RingCenter ("Ring Center", Vector) = (8,6,-76,0)
        _RingNormal ("Ring Plane Normal", Vector) = (0,1,0,0)
        _RingRadii ("Ring Inner / Outer Radius", Vector) = (31.5,44,0,0)
        _PlanetSphere ("Planet Center / Radius", Vector) = (8,6,-76,29)

        _PaleBand ("Pale Band", Color) = (0.94, 0.69, 0.39, 1)
        _DarkBand ("Dark Band", Color) = (0.22, 0.065, 0.045, 1)
        _StormColor ("Storm", Color) = (1.0, 0.28, 0.075, 1)
        _SunDirection ("Sun Direction", Vector) = (-0.62, 0.30, -0.72, 0)
        _CloudRelief ("Cloud Slopes / Upper Deck / Height", 2D) = "gray" {}
        _CloudLayerHeight ("Relative Cloud Height", Float) = 0.0012
        _CloudReliefStrength ("Cloud Relief Strength", Range(0,1)) = 0.75
        _WeatherMap ("Authored Weather", 2D) = "white" {}
        _ObservationTime ("Observation Time", Float) = 0
        _WeatherPulse ("Weather Grace", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        Cull Back
        ZWrite On
        Pass
        {
            Name "GreatWeather"
            Tags { "LightMode"="SRPDefaultUnlit" }
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #include "QuietWatchWeatherCommon.hlsl"

            TEXTURE2D(_WeatherMap);
            SAMPLER(sampler_WeatherMap);
            TEXTURE2D(_CloudRelief); SAMPLER(sampler_CloudRelief);

            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings
            {
                float4 positionCS:SV_POSITION;
                float3 normalWS:TEXCOORD0;
                float3 globe:TEXCOORD1;
                float3 positionWS:TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                VertexPositionInputs p = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(QWProjectionPosition(p.positionWS));
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.globe = normalize(input.normalOS);
                output.positionWS = p.positionWS;
                return output;
            }

            half4 frag(Varyings input):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float3 n = normalize(input.normalWS);
                float3 v = QWViewDirection(input.positionWS);
                float3 sun = normalize(_SunDirection.xyz);

                float3 globe = normalize(input.globe);
                float longitude = atan2(globe.x, -globe.z) * 0.15915494 + 0.5;
                float latitude = asin(clamp(globe.y, -1.0, 1.0)) * 0.31830989 + 0.5;

                // Stable cloud structure is authored offline. Only a very slow
                // periodic longitudinal flow is evaluated on the headset.
                float flow = _ObservationTime * 0.000006;
                float2 weatherUv = float2(longitude + flow * (0.75 + 0.25 * sin(latitude * 30.0)), latitude);
                float2 dx = ddx(weatherUv), dy = ddy(weatherUv);
                // atan2 wraps at the meridian. Unwrap gradients so that this
                // seam does not select the coarsest mip as a vertical stripe.
                dx.x -= round(dx.x);
                dy.x -= round(dy.x);
                float4 weather = SAMPLE_TEXTURE2D_GRAD(_WeatherMap, sampler_WeatherMap,
                    float2(frac(weatherUv.x), weatherUv.y), dx, dy);
                float4 relief = SAMPLE_TEXTURE2D_GRAD(_CloudRelief, sampler_CloudRelief, weatherUv, dx, dy);
                float cosLatitude = max(0.08, length(globe.xz));
                float3 east = normalize(float3(-globe.z, 0.0, globe.x) + float3(0.000001,0,0));
                float3 north = normalize(cross(east, globe));
                float3 cloudNormalOS = normalize(globe - _CloudReliefStrength *
                    0.125 * (east * (relief.r * 2.0 - 1.0) + north * (relief.g * 2.0 - 1.0)));
                float3 cloudNormal = TransformObjectToWorldNormal(cloudNormalOS);
                float3 sunOS = TransformWorldToObjectDir(sun);
                float sunDot = dot(n, sun);
                float2 shadowOffset = float2(dot(sunOS,east) * 0.15915494 / cosLatitude,
                    dot(sunOS,north) * 0.31830989) * _CloudLayerHeight / max(0.20,sunDot);
                float4 upperSunward = SAMPLE_TEXTURE2D_GRAD(_CloudRelief, sampler_CloudRelief,
                    weatherUv + shadowOffset, dx, dy);
                float cloudShadow = smoothstep(0.006,0.065,upperSunward.a-relief.a) * upperSunward.b;
                float3 color = lerp(weather.rgb,float3(0.82,0.78,0.67),relief.b*0.08);
                float storm = weather.a;

                float light = smoothstep(-0.04, 0.085, sunDot);
                float moonLight = QWMoonTransmission(input.positionWS);
                light *= QWRingTransmission(input.positionWS) * moonLight;
                light *= 1.0 - cloudShadow * 0.30;

                float viewDot = saturate(dot(n, v));
                float atmosphere = pow(1.0 - viewDot, 2.35);
                float forwardGlow = smoothstep(-0.18, 0.42, sunDot) * moonLight;
                // Reflected ring light keeps the nominal nightside legible in
                // the cabin; the direct sun still supplies the main contrast.
                color *= 0.018 + light * (0.045 + 1.18 * saturate(dot(cloudNormal, sun)));
                color += float3(0.62, 0.52, 0.39) * atmosphere * forwardGlow * 0.20;
                color += float3(0.12, 0.19, 0.36) * atmosphere * (1.0 - light) * 0.035;

                // A thin forward-scattering veil catches light over the limb
                // and within bright zones, separating upper haze from belts.
                float upperHaze = pow(1.0 - viewDot, 5.0) * smoothstep(-0.12, 0.55, sunDot) * moonLight;
                color += float3(0.82, 0.76, 0.62) * upperHaze * 0.14;
                color += _StormColor.rgb * storm * _WeatherPulse * 0.075;
                color += float3(0.88, 0.48, 0.20) * atmosphere * _WeatherPulse * 0.055;

                // Filmic compression preserves storm and shadow structure under
                // the cabin's restrained bloom without flattening the nightside.
                color = 1.0 - exp(-color * 1.58);
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
