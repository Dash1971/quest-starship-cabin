using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
using StarshipCabin.QuietWatch;

namespace StarshipCabin.EditorTools
{
    internal static class QuietWatchBuildValidation
    {
        public static string SourceHash()
        {
            // Generated materials/scenes and import metadata are excluded;
            // generator code, shaders and authored source assets are included.
            var paths = Directory.GetFiles("Assets", "*", SearchOption.AllDirectories)
                .Where(path => new[] { ".cs", ".shader", ".hlsl", ".png", ".fbx", ".obj", ".blend" }
                    .Contains(Path.GetExtension(path).ToLowerInvariant()))
                .OrderBy(path => path.Replace('\\', '/'), StringComparer.Ordinal);
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            foreach (var path in paths)
            {
                var data = File.ReadAllBytes(path);
                hash.AppendData(Encoding.UTF8.GetBytes(path.Replace('\\', '/') + "\n" + data.Length + "\n"));
                hash.AppendData(data);
            }
            return BitConverter.ToString(hash.GetHashAndReset()).Replace("-", "").ToLowerInvariant();
        }

        public static GeneratedVistaStamp RequireCurrentScene(bool requireBake)
        {
            var stamp = UnityEngine.Object.FindFirstObjectByType<GeneratedVistaStamp>();
            if (stamp == null || stamp.SourceHash != SourceHash())
                throw new InvalidOperationException("Generated scene is stale. Regenerate and bake with Starship Cabin/Quiet Watch/Regenerate, Bake and Build Review APK.");
            if (requireBake && (Lightmapping.isRunning || stamp.BakedSourceHash != stamp.SourceHash
                || LightmapSettings.lightmaps.Length == 0 || LightmapSettings.lightmaps.Any(map => map.lightmapColor == null)))
                throw new InvalidOperationException("Current scene has no verified completed lightmap bake. Run Bake Quarters Lighting and save before building.");
            return stamp;
        }
    }
}
