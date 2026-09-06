using UnityEngine;

namespace StarshipCabin.QuietWatch
{
    /// <summary>Station keep-out sphere or oriented box used only for route audit.</summary>
    public sealed class HarbourClearanceVolume : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float radius = 1f;
        [SerializeField] private string volumeLabel = "Station structure";
        [SerializeField] private Vector3 boxSize;

        public float Radius => radius;
        public string VolumeLabel => volumeLabel;

        public void ConfigureBox(string label, Vector3 size)
        {
            volumeLabel = label;
            boxSize = size;
        }

        public float SignedDistance(Vector3 worldPoint)
        {
            if (boxSize == Vector3.zero)
                return Vector3.Distance(worldPoint, transform.position) - radius * Mathf.Abs(transform.lossyScale.x);
            // Generated station transforms have uniform positive scale.
            var point = transform.InverseTransformPoint(worldPoint);
            var q = new Vector3(Mathf.Abs(point.x), Mathf.Abs(point.y), Mathf.Abs(point.z)) - boxSize * 0.5f;
            return (Vector3.Max(q, Vector3.zero).magnitude + Mathf.Min(Mathf.Max(q.x, Mathf.Max(q.y, q.z)), 0f))
                * Mathf.Abs(transform.lossyScale.x);
        }

        public void Configure(string label, float clearance)
        {
            volumeLabel = string.IsNullOrWhiteSpace(label) ? name : label;
            radius = Mathf.Max(0.1f, clearance);
            boxSize = Vector3.zero;
        }
    }
}
