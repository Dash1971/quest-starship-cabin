using UnityEngine;

namespace StarshipCabin.QuietWatch
{
    // Source identity travels with the generated scene and the player build.
    public sealed class GeneratedVistaStamp : MonoBehaviour
    {
        [SerializeField] private string sourceHash;
        [SerializeField] private string bakedSourceHash;
        public string SourceHash => sourceHash;
        public string BakedSourceHash => bakedSourceHash;
        public void SetSource(string value) { sourceHash = value; bakedSourceHash = string.Empty; }
        public void MarkBaked() => bakedSourceHash = sourceHash;
    }
}
