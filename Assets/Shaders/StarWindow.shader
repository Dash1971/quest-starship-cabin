// Procedural starfield for the quarters observation glass.
// One unlit quad, three parallax star layers, capped twinkle (comfort:
// no flicker), optional slow nebula wash for the Nebula ambience mode.
Shader "StarshipCabin/StarWindow"
{
    Properties
    {
        _DeepColor ("Deep Space", Color) = (0.012, 0.020, 0.045, 1)
        _HazeColor ("Distant Haze", Color) = (0.045, 0.085, 0.160, 1)
        _StarColor ("Star Color", Color) = (0.955, 0.970, 1.0, 1)
        _NebulaColor ("Nebula Color", Color) = (0.240, 0.170, 0.360, 1)
        _NebulaColorB ("Nebula Color B", Color) = (0.100, 0.220, 0.300, 1)
        _Density ("Star Density", Range(0.1, 1.0)) = 0.55
        _Speed ("Scroll Speed", Float) = 0.018
        _Drift ("Lateral Drift", Float) = 0.0
        _Twinkle ("Twinkle Amount", Range(0, 0.35)) = 0.16
        _NebulaAmount ("Nebula Amount", Range(0, 1)) = 0.0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        Lighting Off
        ZWrite On

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _DeepColor;
            fixed4 _HazeColor;
            fixed4 _StarColor;
            fixed4 _NebulaColor;
            fixed4 _NebulaColorB;
            float _Density;
            float _Speed;
            float _Drift;
            float _Twinkle;
            float _NebulaAmount;

            // Scroll speeds are kept in the same value range the old particle
            // starfield used (0.006 .. 0.095); this constant maps them to a
            // gentle UV rate on the big quad.
            #define SPEED_SCALE 0.05

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float hash21(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            float2 hash22(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
                return frac(sin(p) * 43758.5453);
            }

            float vnoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);

                float a = hash21(i);
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + float2(1, 1));

                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float fbm(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.5;
                for (int i = 0; i < 4; i++)
                {
                    value += vnoise(p) * amplitude;
                    p = p * 2.03 + 17.17;
                    amplitude *= 0.5;
                }
                return value;
            }

            float starLayer(float2 uv, float scale, float parallax, float t)
            {
                float2 grid = uv * scale + float2(_Drift, -_Speed) * (t * SPEED_SCALE * scale * parallax);
                float2 cell = floor(grid);
                float2 f = frac(grid);

                float2 rnd = hash22(cell);
                float keep = step(1.0 - _Density * 0.55, hash21(cell * 1.71 + 3.13));
                float2 starPos = 0.18 + rnd * 0.64;

                float d = length(f - starPos);
                float size = lerp(0.030, 0.085, hash21(cell + 7.77) * hash21(cell + 7.77));
                float star = smoothstep(size, size * 0.25, d);

                // Slow, amplitude-capped twinkle: relaxation-safe, never strobes.
                float twinkle = 1.0 - _Twinkle * (0.5 + 0.5 * sin(t * (0.6 + rnd.x * 0.9) + rnd.y * 6.2831));

                return star * keep * twinkle;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float t = _Time.y;
                float2 uv = i.uv;

                // Base: deep space with a faint large-scale haze so black never reads flat.
                float haze = fbm(uv * 2.1 + 5.0) * 0.35;
                float3 col = lerp(_DeepColor.rgb, _HazeColor.rgb, haze);

                // Nebula mode: two-tone slow-drifting wash.
                if (_NebulaAmount > 0.001)
                {
                    float2 nuv = uv * 2.6 + float2(t * 0.004, t * -0.002);
                    float n1 = fbm(nuv);
                    float n2 = fbm(nuv * 1.7 + 31.7);
                    float mask = smoothstep(0.42, 0.85, n1);
                    float3 nebula = lerp(_NebulaColor.rgb, _NebulaColorB.rgb, n2);
                    col += nebula * mask * _NebulaAmount * 0.85;
                }

                // Three parallax star layers: near/bright to far/dim.
                float stars = 0.0;
                stars += starLayer(uv, 14.0, 1.00, t) * 1.00;
                stars += starLayer(uv + 11.31, 26.0, 0.62, t) * 0.62;
                stars += starLayer(uv + 47.77, 46.0, 0.36, t) * 0.40;

                col += _StarColor.rgb * stars;

                return fixed4(col, 1.0);
            }
            ENDCG
        }
    }

    Fallback "Unlit/Color"
}
