using System;

namespace StarshipCabin.QuietWatch
{
    /// <summary>
    /// Engine-independent, injectable observation clock. Mode changes affect
    /// future velocity, never the accumulated pose. Quiet pauses a started
    /// event rather than rewinding the world. One event is allowed per entry.
    /// </summary>
    public sealed class VistaTimeline
    {
        private readonly double delay;
        private readonly double duration;
        private bool living;
        private bool drifting;
        private bool preview;

        public double Elapsed { get; private set; }
        public double LivingElapsed { get; private set; }
        public double LivingTravel { get; private set; }
        public double QuietTravel { get; private set; }
        public double DriftTravel { get; private set; }
        public double Activity { get; private set; }
        public double DriftSpeed { get; private set; }
        public double EventAge { get; private set; } = -1;
        public double Progress => EventAge < 0 ? 0 : Smooth(EventAge / duration);

        public VistaTimeline(double delaySeconds, double durationSeconds)
        {
            delay = Math.Max(0, delaySeconds);
            duration = Math.Max(0.001, durationSeconds);
        }

        public void Reset(bool isLiving, bool isDrifting)
        {
            living = isLiving;
            drifting = isDrifting;
            Activity = living ? 1 : 0;
            DriftSpeed = drifting ? 1 : 0;
            Elapsed = LivingElapsed = LivingTravel = QuietTravel = DriftTravel = 0;
            EventAge = -1;
            preview = false;
        }

        public void SetModes(bool isLiving, bool isDrifting)
        {
            if (living != isLiving) LivingElapsed = 0;
            living = isLiving;
            drifting = isDrifting;
        }

        /// <returns>True only on the frame a scheduled event starts.</returns>
        public bool Advance(double seconds, bool allowEvent = true)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds <= 0) return false;
            Elapsed += seconds;
            var activity = Activity;
            LivingTravel += Integrate(ref activity, living ? 1 : 0, seconds);
            Activity = activity;
            QuietTravel = Elapsed - LivingTravel;
            var speed = DriftSpeed;
            DriftTravel += Integrate(ref speed, drifting ? 1 : 0, seconds);
            DriftSpeed = speed;
            if (!living || !allowEvent) return false;

            var started = false;
            var eventSeconds = seconds;
            if (EventAge < 0)
            {
                LivingElapsed += seconds;
                if (LivingElapsed < delay) return false;
                eventSeconds = Math.Min(seconds, LivingElapsed - delay);
                EventAge = 0;
                started = true;
            }
            EventAge = Math.Min(duration, EventAge + eventSeconds * (preview ? 8 : 1));
            return started;
        }

        // A review runs from the current state at 8x event speed. No mid-event
        // teleport; a completed event can be replayed by re-entering the vista.
        public bool Preview(bool accelerated = true)
        {
            if (!living || EventAge >= duration) return false;
            preview = accelerated;
            if (EventAge < 0) EventAge = 0;
            return true;
        }

        public void Seek(double seconds, bool isLiving, bool isDrifting, bool allowEvent = true)
        {
            Reset(isLiving, isDrifting);
            Advance(Math.Max(0, seconds), allowEvent);
        }

        private static double Integrate(ref double current, double target, double dt)
        {
            const double settleSeconds = 2;
            var decay = Math.Exp(-dt / settleSeconds);
            var area = target * dt + (current - target) * settleSeconds * (1 - decay);
            current = target + (current - target) * decay;
            return area;
        }

        private static double Smooth(double value)
        {
            value = Math.Max(0, Math.Min(1, value));
            return value * value * (3 - 2 * value);
        }
    }
}
