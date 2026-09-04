using System;
using UnityEngine;

namespace StarshipCabin.QuietWatch
{
    /// <summary>
    /// An authored, allocation-free harbour flight corridor. The route is
    /// sampled at build time against explicit station clearance volumes and at
    /// runtime provides only position/tangent evaluation—no Quest physics cost.
    /// </summary>
    public sealed class HarbourTrafficRoute : MonoBehaviour
    {
        [SerializeField] private Vector3[] points = Array.Empty<Vector3>();
        [SerializeField, Min(8f)] private float livingDuration = 48f;
        [SerializeField, Min(8f)] private float quietDuration = 96f;
        [SerializeField, Range(0f, 1f)] private float phaseOffset;
        [SerializeField, Min(0.1f)] private float clearanceRadius = 2f;
        [SerializeField, Range(0f, 18f)] private float bankDegrees = 7f;
        [SerializeField] private bool availableInQuiet;
        [SerializeField] private bool graceRoute;

        public Vector3[] Points => points;
        public float LivingDuration => livingDuration;
        public float QuietDuration => quietDuration;
        public float PhaseOffset => phaseOffset;
        public float ClearanceRadius => clearanceRadius;
        public float BankDegrees => bankDegrees;
        public bool AvailableInQuiet => availableInQuiet;
        public bool IsGraceRoute => graceRoute;

        public void Configure(
            Vector3[] routePoints, float livingSeconds, float quietSeconds,
            float offset, float radius, float maximumBank,
            bool quiet, bool grace)
        {
            points = routePoints ?? Array.Empty<Vector3>();
            livingDuration = Mathf.Max(8f, livingSeconds);
            quietDuration = Mathf.Max(8f, quietSeconds);
            phaseOffset = Mathf.Repeat(offset, 1f);
            clearanceRadius = Mathf.Max(0.1f, radius);
            bankDegrees = Mathf.Clamp(maximumBank, 0f, 18f);
            availableInQuiet = quiet;
            graceRoute = grace;
        }

        public float PhaseAt(float elapsed, bool living)
        {
            var duration = living ? livingDuration : quietDuration;
            return Mathf.Repeat(elapsed / duration + phaseOffset, 1f);
        }

        public void Evaluate(float phase, out Vector3 position, out Vector3 tangent, out float curvature)
        {
            if (points == null || points.Length == 0)
            {
                position = transform.localPosition;
                tangent = Vector3.forward;
                curvature = 0f;
                return;
            }
            if (points.Length == 1)
            {
                position = points[0];
                tangent = Vector3.forward;
                curvature = 0f;
                return;
            }

            var p = Mathf.Clamp01(phase);
            position = Sample(p);
            const float step = 0.0035f;
            var before = Sample(Mathf.Max(0f, p - step));
            var after = Sample(Mathf.Min(1f, p + step));
            tangent = after - before;
            if (tangent.sqrMagnitude < 0.000001f)
            {
                tangent = points[points.Length - 1] - points[0];
            }

            var earlier = Sample(Mathf.Max(0f, p - step * 2f));
            var later = Sample(Mathf.Min(1f, p + step * 2f));
            var incoming = (position - earlier).normalized;
            var outgoing = (later - position).normalized;
            curvature = Vector3.Cross(incoming, outgoing).y * 5.5f;
        }

        private Vector3 Sample(float phase)
        {
            var segmentCount = points.Length - 1;
            var scaled = Mathf.Clamp01(phase) * segmentCount;
            var segment = Mathf.Min(Mathf.FloorToInt(scaled), segmentCount - 1);
            var t = segment == segmentCount - 1 && phase >= 1f ? 1f : scaled - segment;
            var p0 = points[Mathf.Max(0, segment - 1)];
            var p1 = points[segment];
            var p2 = points[segment + 1];
            var p3 = points[Mathf.Min(points.Length - 1, segment + 2)];
            var t2 = t * t;
            var t3 = t2 * t;
            return 0.5f * ((2f * p1)
                + (-p0 + p2) * t
                + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2
                + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }
    }
}
