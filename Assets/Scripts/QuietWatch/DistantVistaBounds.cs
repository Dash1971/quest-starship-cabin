using UnityEngine;

namespace StarshipCabin.QuietWatch
{
    /// <summary>
    /// The distant projection displaces vertices by the room-scale eye offset.
    /// Expand CPU culling bounds accordingly; renderer overrides are not saved
    /// by Unity, so reapply them in both the editor and the player.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
    public sealed class DistantVistaBounds : MonoBehaviour
    {
        private void OnEnable()
        {
            var mesh = GetComponent<MeshFilter>().sharedMesh;
            if (mesh == null) return;
            var scale = transform.lossyScale;
            var minimumScale = Mathf.Max(0.001f,
                Mathf.Min(Mathf.Abs(scale.x), Mathf.Min(Mathf.Abs(scale.y), Mathf.Abs(scale.z))));
            var bounds = mesh.bounds;
            bounds.Expand(24f / minimumScale); // 12 m in every direction around the cabin.
            GetComponent<MeshRenderer>().localBounds = bounds;
        }

        private void OnDisable() => GetComponent<MeshRenderer>().ResetLocalBounds();
    }
}
