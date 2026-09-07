using System;

namespace StarshipCabin.QuietWatch
{
    /// <summary>Original stellar catalogue and one translation model; independent of Unity for projection tests.</summary>
    public static class FirstQuestionField
    {
        public const int StarCount = 12288;
        public const float Period = 196608f;
        public const float Speed = 96f;
        public const float NearDepth = 8000f;
        public const float FarDepth = 44000f;
        public const float CometDuration = 120f;
        public const float CometDelay = 780f;
        public const float CometTailDegrees = 2.1f;
        public const float CometHalfWidthDegrees = .18f;

        public struct Star
        {
            public float X, Y, Z, R, G, B, Flux, Sigma;
        }
        public struct Position
        {
            public double X, Y, Z, Visibility;
        }

        private static uint Mix(uint value)
        {
            unchecked
            {
                value ^= value >> 16; value *= 0x7feb352d;
                value ^= value >> 15; value *= 0x846ca68b;
                return value ^ (value >> 16);
            }
        }
        private static float Unit(uint value) => (Mix(value) & 0xffffff) / 16777216f;
        private static float Range(float a, float b, float u) => a + (b - a) * u;
        private static double Smooth(double a, double b, double value)
        {
            var t = Math.Max(0, Math.Min(1, (value-a)/(b-a)));
            return t*t*(3-2*t);
        }

        public static Star[] Catalogue()
        {
            var stars = new Star[StarCount];
            for (var i=0; i<stars.Length; i++)
            {
                var seed = (uint)i * 11 + 71029u;
                var depth = Range(NearDepth, FarDepth, Unit(seed));
                var luminance = Unit(seed+3);
                var temperature = Unit(seed+4);
                var star = new Star {
                    X = Range(-Period*.5f, Period*.5f, Unit(seed+1)),
                    Y = Range(-.72f, .72f, Unit(seed+2))*depth,
                    Z = -depth,
                    R = temperature < .5f ? 1f : Range(1f,.72f,(temperature-.5f)*2),
                    G = temperature < .5f ? Range(.74f,1f,temperature*2) : Range(1f,.84f,(temperature-.5f)*2),
                    B = temperature < .5f ? Range(.48f,1f,temperature*2) : 1f,
                    Flux = .065f + 3.4f*(float)Math.Pow(luminance,8),
                    Sigma = .50f + .43f*(float)Math.Pow(luminance,12)
                };
                // Sparse distant associations are made of individual stars, never a fog card.
                // Their actual spatial positions share the same cruise translation as every other source.
                if (i >= StarCount-144)
                {
                    var compact = i >= StarCount-32;
                    var spread = compact ? 250f : 1500f;
                    var radius = Math.Sqrt(Unit(seed+5));
                    var angle = Unit(seed+6)*Math.PI*2;
                    star.X = (compact ? 14800f : 8700f) + (float)(Math.Cos(angle)*radius)*spread;
                    star.Y = (compact ? 13500f : 9000f) + (float)(Math.Sin(angle)*radius)*spread*.62f;
                    star.Z = (compact ? -39000f : -30000f) + Range(-spread,spread,Unit(seed+7));
                    star.Flux *= compact ? .21f : .46f;
                }
                stars[i] = star;
            }
            // Two restrained warm/cool doubles give a recognisable reference for watching travel.
            stars[0] = new Star { X=5800,Y=5100,Z=-23000,R=1,G=.85f,B=.66f,Flux=3.1f,Sigma=.91f };
            stars[1] = new Star { X=5878,Y=5145,Z=-23000,R=.8f,G=.89f,B=1,Flux=1.15f,Sigma=.61f };
            stars[2] = new Star { X=-6700,Y=2600,Z=-18000,R=1,G=.96f,B=.88f,Flux=2.4f,Sigma=.78f };
            stars[3] = new Star { X=-6795,Y=2630,Z=-18000,R=.78f,G=.87f,B=1,Flux=.8f,Sigma=.55f };
            return stars;
        }

        public static Position At(Star star, int index, double easedSeconds)
        {
            var travel = Math.Max(0,easedSeconds)*Speed;
            var cycle = Math.Floor((star.X+travel+Period*.5)/Period);
            var x = star.X+travel-cycle*Period;
            double y=star.Y, z=star.Z;
            if (cycle!=0)
            {
                // Recycle only at the zero-opacity edges. Changing depth there avoids a repeated
                // constellation loop. Integer hashing is bit-identical to the vertex shader.
                var seed = unchecked((uint)(index+1) ^ (uint)cycle*0x9e3779b9u);
                var depth = Range(NearDepth,FarDepth,Unit(seed));
                y = Range(-.72f,.72f,Unit(seed+1))*depth;
                z = -depth;
            }
            return new Position { X=x, Y=y, Z=z, Visibility=1-Smooth(Period*.40,Period*.5,Math.Abs(x)) };
        }

        // Shared event placement for the editor projection audit and the real renderer.
        // The intrinsic orbital change is <0.5 degree over two minutes, not a meteor streak.
        public static Position CometAt(double eventAge, double travelSinceAppearance)
        {
            return new Position { X=11000+Math.Max(0,travelSinceAppearance)*Speed,
                Y=7000+Math.Max(0,eventAge)*2.0, Z=-36000,
                Visibility=eventAge<0 ? 0 : Smooth(0,16,eventAge)*(1-Smooth(94,120,eventAge)) };
        }
    }
}
