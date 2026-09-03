using UnityEngine;

namespace StarshipCabin.QuietWatch
{
    /// <summary>Named, repeatable camera pose used for milestone comparison captures.</summary>
    public sealed class VistaCapturePoint : MonoBehaviour
    {
        [SerializeField] private string captureName;
        public string CaptureName => captureName;

        public void Configure(string value)
        {
            captureName = value;
        }
    }
}
