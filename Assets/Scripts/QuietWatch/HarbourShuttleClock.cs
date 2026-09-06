using System;

namespace StarshipCabin.QuietWatch
{
    /// <summary>A continuous round trip with eased arrivals and a dwell at both ends.</summary>
    public static class HarbourShuttleClock
    {
        // One cycle: 40% outward, 10% docked, 40% backing out, 10% at origin.
        // Orientation stays along the corridor while reversing, as a service
        // craft under manoeuvring thrusters; no instantaneous 180-degree turn.
        public static double Phase(double cycles)
        {
            var cycle = cycles - Math.Floor(cycles);
            if (cycle < 0.4) return Smooth(cycle / 0.4);
            if (cycle < 0.5) return 1;
            if (cycle < 0.9) return 1 - Smooth((cycle - 0.5) / 0.4);
            return 0;
        }

        private static double Smooth(double t) => t * t * (3 - 2 * t);
    }
}
