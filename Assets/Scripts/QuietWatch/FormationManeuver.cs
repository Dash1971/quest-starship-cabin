using System;

namespace StarshipCabin.QuietWatch
{
    /// <summary>Preserves fleet attitude and angular velocity when an event is previewed, replayed or cancelled.</summary>
    public sealed class FormationManeuver
    {
        public double Value { get; private set; }
        public double Velocity { get; private set; }

        public void Reset() { Value = Velocity = 0; }

        public void Advance(double seconds, double target)
        {
            if (seconds <= 0 || double.IsNaN(seconds) || double.IsInfinity(seconds)) return;
            // Exact critically damped response to a held target. A preview changes
            // the destination, never the visible pose or its current velocity.
            const double omega = 1.0;
            var offset = Value - target;
            var slope = Velocity + omega * offset;
            var decay = Math.Exp(-omega * seconds);
            Value = target + (offset + slope * seconds) * decay;
            Velocity = (Velocity - omega * slope * seconds) * decay;
        }

        public void SeekScheduled(double age, double duration)
        {
            Reset();
            if (age <= 0) return;
            // Match actual 72 Hz playback for deterministic captures, including
            // settling after completion, without simulating hours of waiting.
            var end = Math.Min(age, duration + 80);
            for (double time = 0; time < end;)
            {
                var dt = Math.Min(1.0 / 72, end - time);
                time += dt;
                var u = Math.Max(0, Math.Min(1, time / duration));
                Advance(dt, u * u * (3 - 2 * u));
            }
        }
    }
}
