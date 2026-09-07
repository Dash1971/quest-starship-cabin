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
            // Resolve from the Unity project rather than the launcher's working
            // directory. Include generator inputs and import settings outside
            // Assets so the stamp actually covers the claimed source surface.
            var root = Directory.GetParent(Application.dataPath)?.FullName
                ?? throw new InvalidOperationException("Cannot resolve Unity project root.");
            var extensions = new[] { ".cs", ".shader", ".hlsl", ".png", ".fbx", ".obj", ".blend", ".py", ".json" };
            var roots = new[]
                {
                    "Assets/Art/QuietWatch",
                    "Assets/Editor",
                    "Assets/Scripts",
                    "Assets/Shaders",
                    "ArtSource",
                    "tools"
                }
                .Select(path => Path.Combine(root, path))
                .Where(Directory.Exists);
            var sourcePaths = roots.SelectMany(path => Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                .Where(path => extensions.Contains(Path.GetExtension(path).ToLowerInvariant()))
                .Concat(new[]
                {
                    // The manifest is authored input. Unity rewrites the lock
                    // and several ProjectSettings files during import/bake, so
                    // those mutable outputs cannot define scene identity; the
                    // checked-in generator code that configures them is hashed.
                    Path.Combine(root, "Packages/manifest.json")
                }.Where(File.Exists))
                .Distinct()
                .ToArray();
            var authoredArtRoot = Path.Combine(root, "Assets/Art/QuietWatch") + Path.DirectorySeparatorChar;
            var importMetadata = sourcePaths
                .Where(path => path.StartsWith(authoredArtRoot, StringComparison.Ordinal))
                .Select(path => path + ".meta")
                .Where(File.Exists);
            var paths = sourcePaths.Concat(importMetadata)
                .OrderBy(path => Path.GetRelativePath(root, path).Replace('\\', '/'), StringComparer.Ordinal);
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            foreach (var path in paths)
            {
                var data = File.ReadAllBytes(path);
                var relative = Path.GetRelativePath(root, path).Replace('\\', '/');
                hash.AppendData(Encoding.UTF8.GetBytes(relative + "\n" + data.Length + "\n"));
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
            if (requireBake)
            {
                QuartersDecor.ValidateChessLighting(true);
                QuietWatchDeskLighting.Validate(true);
            }
            return stamp;
        }
    }
}
