// Procedural banded gas giant for Quarters V2 Milestone 9.
Shader "StarshipCabin/Planet"
{
    Properties
    {
        _ColHi ("Band Highlight", Color) = (0.925, 0.847, 0.741, 1)
        _ColMid ("Band Mid", Color) = (0.784, 0.541, 0.373, 1)
        _ColLo ("Band Shadow", Color) = (0.482, 0.322, 0.251, 1)
        _StormColor ("Great Storm", Color) = (0.70, 0.38, 0.34, 1)
        _SunColor ("Sun Color", Color) = (1.0, 0.94, 0.86, 1)
        _AtmoWarm ("Atmosphere Warm", Color) = (1.0, 0.77, 0.55, 1)
        _AtmoCool ("Atmosphere Cool", Color) = (0.47, 0.80, 0.86, 1)
        _SunDir ("Sun Direction (world)", Vector) = (-0.55, 0.35, -0.76, 0)
        _BandScale ("Band Count", Float) = 7.0
        _Turbulence ("Turbulence", Range(0, 1)) = 0.55
        _StormLat ("Storm Latitude", Range(-1, 1)) = 0.28
        _StormLon ("Storm Longitude", Range(0, 6.2832)) = 2.1
        _RimPower ("Rim Power", Range(1, 8)) = 3.2
        _RimStrength ("Rim Strength (HDR)", Range(0, 3)) = 1.5
        _DayBoost ("Sunlit Boost (HDR)", Range(1, 3)) = 1.7
        _NightLevel ("Night Fill", Range(0, 0.3)) = 0.06
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        Lighting Off
        ZWrite On
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _ColHi, _ColMid, _ColLo, _StormColor, _SunColor, _AtmoWarm, _AtmoCool;
            float4 _SunDir;
            float _BandScale, _Turbulence, _StormLat, _StormLon, _RimPower, _RimStrength, _DayBoost, _NightLevel;

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 oPos : TEXCOORD0;
                float3 wN : TEXCOORD1;
                float3 wV : TEXCOORD2;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.oPos = normalize(v.vertex.xyz);
                o.wN = normalize(UnityObjectToWorldNormal(v.normal));
                float3 wp = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.wV = normalize(_WorldSpaceCameraPos - wp);
                return o;
            }

            float hash(float2 p) { return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453); }

            float vnoise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float a = hash(i), b = hash(i + float2(1, 0)), c = hash(i + float2(0, 1)), d = hash(i + float2(1, 1));
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float fbm(float2 p)
            {
                float v = 0.0, a = 0.5;
                for (int k = 0; k < 5; k++) { v += vnoise(p) * a; p = p * 2.02 + 11.1; a *= 0.5; }
                return v;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 n = normalize(i.oPos);
                float lat = n.y;
                float lon = atan2(n.z, n.x);

                float warp = (fbm(float2(lon * 1.6, lat * 3.0)) - 0.5) * _Turbulence * 0.5;
                float bands = sin((lat + warp) * _BandScale * 3.14159);
                float t = bands * 0.5 + 0.5;
                float3 albedo = lerp(_ColLo.rgb, _ColMid.rgb, smoothstep(0.0, 0.6, t));
                albedo = lerp(albedo, _ColHi.rgb, smoothstep(0.55, 1.0, t));

                float detail = fbm(float2(lon * 3.0, (lat + warp) * 10.0));
                albedo *= 0.85 + 0.30 * detail;

                float dLon = abs(atan2(sin(lon - _StormLon), cos(lon - _StormLon)));
                float2 sd = float2(dLon * 0.7, (lat - _StormLat) * 1.8);
                float storm = smoothstep(0.5, 0.0, length(sd));
                albedo = lerp(albedo, _StormColor.rgb, storm * 0.9);

                float ndl = dot(i.wN, normalize(_SunDir.xyz));
                float day = smoothstep(-0.15, 0.35, ndl);
                float3 lit = albedo * _SunColor.rgb * (_NightLevel + day * _DayBoost);

                float fres = pow(1.0 - saturate(dot(i.wV, i.wN)), _RimPower);
                float3 atmo = lerp(_AtmoCool.rgb, _AtmoWarm.rgb, saturate(day + 0.15));
                lit += atmo * fres * _RimStrength * (0.25 + day);

                return fixed4(lit, 1.0);
            }
            ENDCG
        }
    }

    Fallback "Unlit/Color"
}
