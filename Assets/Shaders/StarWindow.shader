// Procedural starfield for the quarters observation glass — V2 beauty pass.
// One unlit quad. Three parallax star layers with per-star colour temperature,
// soft halos on the brightest stars, a faint galactic band, occasional
// shooting stars (frequency follows the ambience mode's motion), capped
// twinkle (comfort: never strobes), and a slow two-tone nebula wash for the
// Nebula mode. Works under Built-in RP and URP (single SRPDefaultUnlit pass).
Shader "StarshipCabin/StarWindow"
{
    Properties
    {
        _DeepColor ("Deep Space", Color) = (0.012, 0.020, 0.045, 1)
        _HazeColor ("Distant Haze", Color) = (0.045, 0.085, 0.160, 1)
        _StarColor ("Star Color", Color) = (0.955, 0.970, 1.0, 1)
        _TintWarm ("Warm Star Tint", Color) = (1.0, 0.83, 0.66, 1)
        _TintCool ("Cool Star Tint", Color) = (0.70, 0.80, 1.0, 1)
        _BandColor ("Galactic Band", Color) = (0.115, 0.125, 0.210, 1)
        _BandStrength ("Band Strength", Range(0, 1)) = 0.55
        _NebulaColor ("Nebula Color", Color) = (0.240, 0.170, 0.360, 1)
        _NebulaColorB ("Nebula Color B", Color) = (0.100, 0.220, 0.300, 1)
        _Density ("Star Density", Range(0.1, 1.0)) = 0.55
        _Speed ("Scroll Speed", Float) = 0.018
        _Drift ("Lateral Drift", Float) = 0.0
        _Twinkle ("Twinkle Amount", Range(0, 0.35)) = 0.20
        _NebulaAmount ("Nebula Amount", Range(0, 1)) = 0.0
        _Meteors ("Shooting Stars", Range(0, 1)) = 1.0
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
            fixed4 _TintWarm;
            fixed4 _TintCool;
            fixed4 _BandColor;
            float _BandStrength;
            fixed4 _NebulaColor;
            fixed4 _NebulaColorB;
            float _Density;
            float _Speed;
            float _Drift;
            float _Twinkle;
            float _NebulaAmount;
            float _Meteors;

            // Mode speeds arrive in the historical 0.006..0.095 range; this maps
            // them to UV rates. Raised from 0.05 (V1) so Orbit visibly glides
            // while Drift stays near-still.
            #define SPEED_SCALE 0.12

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

            // Signed distance from the galactic band spine (a tilted line in UV
            // space, roughly matching the slope's visible region diagonal).
            float bandMask(float2 uv)
            {
                float2 spineDir = normalize(float2(0.88, 0.47));
                float2 rel = uv - float2(1.56, 1.05);
                float s = dot(rel, float2(-spineDir.y, spineDir.x));
                return exp(-s * s * 9.0);
            }

            // Per-star colour temperature: most stars near-white, tails of the
            // hash distribution go warm or cool.
            float3 starTint(float t)
            {
                float3 tint = float3(1.0, 1.0, 1.0);
                tint = lerp(tint, _TintWarm.rgb, saturate((0.32 - t) * 4.0));
                tint = lerp(tint, _TintCool.rgb, saturate((t - 0.68) * 4.0));
                return tint;
            }

            float3 starLayer(float2 uv, float scale, float parallax, float t, float band)
            {
                // Milestone 5: stars stream laterally toward +u (toward the
                // sleep alcove) so the ship reads as flying forward past a side
                // window, not descending. _Drift adds a touch more lateral
                // motion in Orbit; there is no vertical component.
                float2 grid = uv * scale + float2(-(_Speed + _Drift * 0.35), 0.0) * (t * SPEED_SCALE * scale * parallax);
                float2 cell = floor(grid);
                float2 f = frac(grid);

                float2 rnd = hash22(cell);
                // Denser inside the galactic band.
                float density = _Density * 0.55 * (1.0 + band * 1.3);
                float keep = step(1.0 - density, hash21(cell * 1.71 + 3.13));
                float2 starPos = 0.18 + rnd * 0.64;

                float d = length(f - starPos);
                float sizeRand = hash21(cell + 7.77);
                float size = lerp(0.030, 0.085, sizeRand * sizeRand);
                float star = smoothstep(size, size * 0.25, d);

                // Soft halo on the brightest stars only.
                float bigness = smoothstep(0.55, 1.0, sizeRand);
                float halo = smoothstep(size * 4.5, size * 0.8, d) * 0.20 * bigness;

                // Slow, amplitude-capped twinkle: relaxation-safe, never strobes.
                float twinkle = 1.0 - _Twinkle * (0.5 + 0.5 * sin(t * (0.6 + rnd.x * 0.9) + rnd.y * 6.2831));

                return starTint(hash21(cell + 21.3)) * (star + halo) * keep * twinkle;
            }

            // One shooting-star "lane": each period may (or may not) fire a
            // single streak with a hashed start point and heading; the streak
            // lives for the first ~9% of the period. Branchless.
            float meteorLane(float2 uv, float t, float lane, float period)
            {
                float pt = t / period + lane * 0.37;
                float idx = floor(pt);
                float f = frac(pt);

                float2 r = hash22(float2(idx * 3.71 + lane * 17.9, lane * 7.3 + 1.7));
                float active = step(0.45, hash21(float2(idx * 5.13, lane * 11.1))); // ~55% of periods fire

                float life = f / 0.09;                     // 0..1 across the flight
                float alive = step(life, 1.0);

                float2 start = float2(lerp(0.35, 2.55, r.x), lerp(1.15, 2.25, r.y));
                float side = sign(hash21(float2(idx, lane * 3.3)) - 0.5);
                float2 dir = normalize(float2(side * lerp(0.55, 1.0, r.y), lerp(-0.62, -0.30, r.x)));

                float2 head = start + dir * life * 1.15;

                float2 toUv = uv - head;
                float along = dot(toUv, -dir);
                float perp = abs(dot(toUv, float2(-dir.y, dir.x)));

                float tail = smoothstep(0.28, 0.0, along) * step(0.0, along);
                float core = smoothstep(0.010, 0.0018, perp);
                float fade = sin(saturate(life) * 3.14159);

                return core * tail * fade * active * alive;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float t = _Time.y;
                float2 uv = i.uv;
                float band = bandMask(uv);

                // Base: deep space, faint large-scale haze, galactic band glow.
                float haze = fbm(uv * 2.1 + 5.0) * 0.35;
                float3 col = lerp(_DeepColor.rgb, _HazeColor.rgb, haze);
                col += _BandColor.rgb * band * (0.45 + fbm(uv * 6.3 + 11.0) * 0.55) * _BandStrength;

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
                float3 stars = float3(0.0, 0.0, 0.0);
                stars += starLayer(uv, 14.0, 1.00, t, band) * 1.00;
                stars += starLayer(uv + 11.31, 26.0, 0.62, t, band) * 0.62;
                stars += starLayer(uv + 47.77, 46.0, 0.36, t, band) * 0.40;
                col += _StarColor.rgb * stars;

                // Shooting stars: two staggered lanes; more motion in the
                // ambience mode (higher _Speed) means more frequent meteors.
                float period = lerp(30.0, 12.0, saturate(_Speed * 9.0));
                float meteors = meteorLane(uv, t, 0.0, period)
                              + meteorLane(uv, t, 1.0, period * 1.31);
                col += _StarColor.rgb * meteors * 1.6 * _Meteors;

                return fixed4(col, 1.0);
            }
            ENDCG
        }
    }

    Fallback "Unlit/Color"
}
