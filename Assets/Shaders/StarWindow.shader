// Procedural starfield for the quarters observation glass — V3 "dark sky" pass.
// Design goal (headset feedback): the awe of a truly dark night sky / Hubble
// frames. That comes from MANY faint point stars with a power-law brightness
// distribution and a rare brilliant few — never from large discs. V3:
//  - stars are near-point cores; brightness varies wildly, size barely
//  - four parallax layers down to a fine "dust of stars"
//  - the brightest few get soft halos + 4-point diffraction spikes (the
//    Hubble signature), on the nearest layer only
//  - the galactic band gains dark dust lanes and much higher faint-star
//    density inside it
//  - soft filmic tone map so brilliant stars bloom instead of clipping
// Motion (M5), shooting stars (M4), nebula mode, and the capped comfort
// twinkle are preserved. Works under Built-in RP and URP (SRPDefaultUnlit).
Shader "StarshipCabin/StarWindow"
{
    Properties
    {
        _DeepColor ("Deep Space", Color) = (0.010, 0.016, 0.038, 1)
        _HazeColor ("Distant Haze", Color) = (0.040, 0.075, 0.145, 1)
        _StarColor ("Star Color", Color) = (0.955, 0.970, 1.0, 1)
        _TintWarm ("Warm Star Tint", Color) = (1.0, 0.80, 0.60, 1)
        _TintCool ("Cool Star Tint", Color) = (0.66, 0.78, 1.0, 1)
        _BandColor ("Galactic Band", Color) = (0.135, 0.140, 0.215, 1)
        _BandStrength ("Band Strength", Range(0, 1)) = 0.6
        _NebulaColor ("Nebula Color", Color) = (0.240, 0.170, 0.360, 1)
        _NebulaColorB ("Nebula Color B", Color) = (0.100, 0.220, 0.300, 1)
        _Density ("Star Density", Range(0.1, 1.0)) = 0.72
        _Speed ("Scroll Speed", Float) = 0.018
        _Drift ("Lateral Drift", Float) = 0.0
        _Twinkle ("Twinkle Amount", Range(0, 0.35)) = 0.18
        _NebulaAmount ("Nebula Amount", Range(0, 1)) = 0.0
        _Meteors ("Shooting Stars", Range(0, 1)) = 1.0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Background" }
        Lighting Off
        ZWrite Off // M9 fix: the starfield is a backdrop — never occludes the planet. Render-order only; fragment output unchanged.

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

            float bandMask(float2 uv)
            {
                float2 spineDir = normalize(float2(0.88, 0.47));
                float2 rel = uv - float2(1.56, 1.05);
                float s = dot(rel, float2(-spineDir.y, spineDir.x));
                return exp(-s * s * 7.0);
            }

            float3 starTint(float t)
            {
                float3 tint = float3(1.0, 1.0, 1.0);
                tint = lerp(tint, _TintWarm.rgb, saturate((0.30 - t) * 4.0));
                tint = lerp(tint, _TintCool.rgb, saturate((t - 0.70) * 4.0));
                return tint;
            }

            // spikes: 1 = this layer's brilliant stars get halo + diffraction
            // spikes (nearest layer only, so the sky doesn't get busy).
            float3 starLayer(float2 uv, float scale, float parallax, float t, float band, float gain, float spikes)
            {
                // M5 direction: lateral stream toward the sleep alcove.
                float2 grid = uv * scale + float2(-(_Speed + _Drift * 0.35), 0.0) * (t * SPEED_SCALE * scale * parallax);
                float2 cell = floor(grid);
                float2 f = frac(grid);

                float2 rnd = hash22(cell);
                // Much denser inside the galactic band — that's what makes it a
                // river of stars rather than a fog.
                float density = saturate(_Density * 0.42 * (1.0 + band * 2.2));
                float keep = step(1.0 - density, hash21(cell * 1.71 + 3.13));
                float2 starPos = 0.18 + rnd * 0.64;

                float2 offset = f - starPos;
                float d = length(offset);

                // Power-law brightness: a dust of faint stars, a rare brilliant few.
                float m = hash21(cell + 7.77);
                float brightness = 0.07 + 2.6 * pow(m, 6.0);

                // Near-point cores: size barely varies (real stars are points).
                float size = lerp(0.012, 0.026, hash21(cell + 3.31));
                float core = smoothstep(size, size * 0.25, d);

                // Bright stars barely twinkle; faint ones shimmer gently. Still
                // amplitude-capped for comfort.
                float steadiness = saturate(brightness * 0.8);
                float twinkle = 1.0 - _Twinkle * (1.0 - steadiness) * (0.5 + 0.5 * sin(t * (0.6 + rnd.x * 0.9) + rnd.y * 6.2831));

                float3 tint = starTint(hash21(cell + 21.3));
                float3 col = tint * core * brightness;

                // Hubble treatment for the brilliant few (top ~3.5%): soft halo
                // + 4-point diffraction spikes.
                float brilliant = step(0.965, m) * spikes;
                if (brilliant > 0.5)
                {
                    float halo = smoothstep(0.15, 0.0, d) * 0.20;
                    float spike =
                        smoothstep(0.0035, 0.0, abs(offset.x)) * smoothstep(0.13, 0.0, abs(offset.y)) +
                        smoothstep(0.0035, 0.0, abs(offset.y)) * smoothstep(0.13, 0.0, abs(offset.x));
                    col += tint * (halo + spike * 0.55) * saturate(brightness);
                }

                return col * keep * twinkle * gain;
            }

            float meteorLane(float2 uv, float t, float lane, float period)
            {
                float pt = t / period + lane * 0.37;
                float idx = floor(pt);
                float f = frac(pt);

                float2 r = hash22(float2(idx * 3.71 + lane * 17.9, lane * 7.3 + 1.7));
                float active = step(0.45, hash21(float2(idx * 5.13, lane * 11.1)));

                float life = f / 0.09;
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

                // Base: deep space + faint haze.
                float haze = fbm(uv * 2.1 + 5.0) * 0.32;
                float3 col = lerp(_DeepColor.rgb, _HazeColor.rgb, haze);

                // Galactic band glow, carved by dark dust lanes — the structure
                // that makes the Milky Way read as a river, not a smear.
                float dust = smoothstep(0.52, 0.80, fbm(uv * 5.1 + 3.3)) * band;
                col += _BandColor.rgb * band * (0.45 + fbm(uv * 6.3 + 11.0) * 0.55) * _BandStrength * (1.0 - dust * 0.8);

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

                // Four parallax layers: brilliant near field down to a fine dust
                // of stars. Spikes on the nearest layer only.
                float3 stars = float3(0.0, 0.0, 0.0);
                stars += starLayer(uv, 14.0, 1.00, t, band, 1.00, 1.0);
                stars += starLayer(uv + 11.31, 26.0, 0.62, t, band, 0.75, 0.0);
                stars += starLayer(uv + 47.77, 46.0, 0.36, t, band, 0.50, 0.0);
                stars += starLayer(uv + 73.21, 72.0, 0.22, t, band, 0.30, 0.0);
                col += _StarColor.rgb * stars;

                // Shooting stars (M4): frequency follows the mode's motion.
                float period = lerp(30.0, 12.0, saturate(_Speed * 9.0));
                float meteors = meteorLane(uv, t, 0.0, period)
                              + meteorLane(uv, t, 1.0, period * 1.31);
                col += _StarColor.rgb * meteors * 1.6 * _Meteors;

                // Soft filmic curve: brilliant stars bloom, blacks stay deep.
                col = 1.0 - exp(-col * 1.35);

                return fixed4(col, 1.0);
            }
            ENDCG
        }
    }

    Fallback "Unlit/Color"
}
