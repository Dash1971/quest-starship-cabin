// Thin transparent planetary ring for Quarters V2 Milestone 9.
Shader "StarshipCabin/PlanetRing"
{
    Properties
    {
        _RingInnerTint ("Inner Tint", Color) = (0.89, 0.83, 0.70, 1)
        _RingOuterTint ("Outer Tint", Color) = (0.62, 0.55, 0.42, 1)
        _Density ("Band Density", Float) = 60
        _Opacity ("Opacity", Range(0, 1)) = 0.55
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _RingInnerTint, _RingOuterTint;
            float _Density, _Opacity;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float hash(float x) { return frac(sin(x * 127.1) * 43758.5453); }

            fixed4 frag(v2f i) : SV_Target
            {
                float r = saturate(i.uv.x);
                float band = hash(floor(r * _Density));
                float gap = step(0.18, band);
                float3 col = lerp(_RingInnerTint.rgb, _RingOuterTint.rgb, r);
                float a = _Opacity * gap * (0.5 + 0.5 * band);
                a *= smoothstep(0.0, 0.05, r) * (1.0 - smoothstep(0.95, 1.0, r));
                return fixed4(col, a);
            }
            ENDCG
        }
    }

    Fallback "Unlit/Transparent"
}
