using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using StarshipCabin.QuietWatch;

namespace StarshipCabin.EditorTools
{
    /// <summary>Real Unity scene checks; run after regeneration, before captures.</summary>
    public static class QuietWatchReviewChecks
    {
        [MenuItem("Starship Cabin/Quiet Watch/Run Review Scene Checks")]
        public static void Run()
        {
            const string scenePath = "Assets/Scenes/Cabin_Quarters_V2.unity";
            EditorSceneManager.OpenScene(scenePath);
            try
            {
                QuietWatchBuildValidation.RequireCurrentScene(false);
                var vistas = UnityEngine.Object.FindObjectsByType<VistaEnvironment>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None);
                Require(vistas.Length == 5, "Expected all five vistas.");
                foreach (var vista in vistas) vista.gameObject.SetActive(false);
                var weather = vistas.OfType<AuthoredVista>().Single(v => v.VistaId == "great-weather");
                weather.gameObject.SetActive(true);
                weather.Enter(LifeMode.Living, MotionMode.Still);
                var moon = weather.transform.Find("Moon in Ring Shadow");
                var origin = moon.localPosition;
                var ring = weather.transform.Find("Planetary Rings");
                var planet = weather.transform.Find("Ringed Giant");
                var bodies = weather.GetComponentsInChildren<MeshRenderer>();
                var reference = planet.GetComponent<MeshRenderer>().sharedMaterial;
                foreach (var body in bodies)
                {
                    var material = body.sharedMaterial;
                    Require(material.shader.isSupported && !ShaderUtil.ShaderHasError(material.shader), "Weather shader failed: " + material.shader.name);
                    Require(material.GetFloat("_DistanceScale") == 1000000f, "Mixed exterior distance scales.");
                    foreach (var property in new[] { "_SunDirection", "_RingCenter", "_RingNormal", "_RingRadii", "_PlanetSphere", "_DistanceOrigin" })
                        Require(material.GetVector(property) == reference.GetVector(property), "Shared weather parameter differs: " + property);
                    Require(body.GetComponent<DistantVistaBounds>() != null, "Missing displaced-vertex bounds.");
                }
                var normal = ring.rotation * Vector3.forward;
                var sharedNormal = (Vector3)reference.GetVector("_RingNormal");
                Require(Vector3.Dot(normal, sharedNormal) > 0.9999f, "Shadow plane differs from ring geometry.");
                var sun = ((Vector3)reference.GetVector("_SunDirection")).normalized;
                float ShadowRayRadius(Vector3 position)
                {
                    var distance = Vector3.Dot(ring.position - position, normal) / Vector3.Dot(sun, normal);
                    Require(distance > 0f, "Moon must be behind the sun-facing ring plane.");
                    return (position + sun * distance - ring.position).magnitude;
                }
                Require(Mathf.Abs(ShadowRayRadius(moon.position) - QuietWatchExteriorBuilder.MoonShadowRadius) < 0.001f, "Moon does not start behind the authored ring band.");
                weather.PreviewAt(weather.GraceNoteAtSeconds + weather.GraceDurationSeconds, LifeMode.Living, MotionMode.Still);
                Require(ShadowRayRadius(moon.position) > QuietWatchExteriorBuilder.RingOuter + QuietWatchExteriorBuilder.MoonDiameter * 0.5f, "Whole moon must clear the outer ring shadow.");
                weather.Exit();
                weather.gameObject.SetActive(true);
                weather.Enter(LifeMode.Living, MotionMode.Still);
                Require((moon.localPosition - origin).sqrMagnitude < 1e-8f, "Reentry cached a preview pose as the origin.");
                weather.PreviewAt(1020, LifeMode.Living, MotionMode.Still);
                var first = moon.localPosition;
                weather.PreviewAt(980, LifeMode.Living, MotionMode.Still);
                weather.PreviewAt(1020, LifeMode.Living, MotionMode.Still);
                Require((moon.localPosition - first).sqrMagnitude < 1e-8f, "Capture depends on previous seek order.");
                weather.ApplyComfort(LifeMode.Quiet, MotionMode.Still);
                Require((moon.localPosition - origin).sqrMagnitude < 1e-8f, "Quiet did not reset the event pose.");
                var mesh = planet.GetComponent<MeshFilter>().sharedMesh;
                var vertices = mesh.vertices;
                var triangles = mesh.triangles;
                for (var i = 0; i < triangles.Length; i += 3)
                {
                    var a = vertices[triangles[i]]; var b = vertices[triangles[i + 1]]; var c = vertices[triangles[i + 2]];
                    Require(Vector3.Dot(Vector3.Cross(b - a, c - a), a + b + c) > 0f, "Planet triangle faces inward or is degenerate.");
                }
                weather.Exit();
                foreach (var vista in vistas.OfType<AuthoredVista>().Where(v => v.VistaId != "great-weather"))
                {
                    if (vista.VistaId == "harbour" || vista.VistaId == "long-formation")
                        Require(Mathf.Abs(vista.transform.lossyScale.x - QuietWatchExteriorBuilder.FleetScale) < 0.001f,
                            "Fleet/station physical scale was lost.");
                    Require(vista.GetComponent<VistaBackdropLayers>() != null, "Missing deterministic backdrop layers.");
                    foreach (var body in vista.GetComponentsInChildren<DistantVistaBounds>(true))
                    {
                        var renderer = body.GetComponent<MeshRenderer>();
                        Require(renderer.GetComponent<MeshFilter>().sharedMesh != null, "Generated backdrop mesh was overwritten.");
                        Require(renderer.sharedMaterial.shader.isSupported && !ShaderUtil.ShaderHasError(renderer.sharedMaterial.shader),
                            "Backdrop shader failed: " + renderer.sharedMaterial.shader.name);
                        Require(Mathf.Abs(renderer.sharedMaterial.GetVector("_PlanetSphere").w *
                            renderer.sharedMaterial.GetFloat("_DistanceScale") - 6371000f) < 2f, "Backdrop physical radius mismatch.");
                    }
                }
                Require(Camera.main.farClipPlane >= 5000f, "Camera clips distant backdrops.");
                foreach (var piece in new[] { "Chess Pieces White", "Chess Pieces Black" })
                {
                    var material = GameObject.Find(piece).GetComponent<Renderer>().sharedMaterial;
                    Require(material.GetFloat("_Smoothness") <= 0.1f && material.GetFloat("_Metallic") == 0f
                        && material.IsKeywordEnabled("_SPECULARHIGHLIGHTS_OFF")
                        && material.IsKeywordEnabled("_ENVIRONMENTREFLECTIONS_OFF"), "Chess material regained sharp reflections.");
                }
                var formation = vistas.OfType<AuthoredVista>().Single(v => v.VistaId == "long-formation");
                formation.gameObject.SetActive(true);
                formation.Enter(LifeMode.Quiet, MotionMode.Still);
                var rig = formation.transform.Find("Formation Flight Rig");
                var command = rig.Find("Command Ship Resolute");
                foreach (var detailName in new[] { "Room-scale occupied decks", "Hull service markings" })
                    Require(command.Find(detailName).localScale == Vector3.one, "Hull details do not inherit fleet scale.");
                formation.PreviewAt(0, LifeMode.Quiet, MotionMode.Still);
                var formationStart = rig.localPosition;
                formation.PreviewAt(10, LifeMode.Quiet, MotionMode.Still);
                Require(Vector3.Distance(formationStart, rig.localPosition) > 0.5f,
                    "Formation must remain visibly underway in Quiet/Still.");
                formation.ApplyComfort(LifeMode.Living, MotionMode.Still);
                Require(formation.PreviewGraceNote(), "Formation event preview must work in Still.");
                formation.Exit();
                QuietWatchTrafficValidation.ValidateOpenScene();
                Debug.Log("QUIET_WATCH_REVIEW_CHECKS PASS: scene identity, weather materials/geometry, shadow path, event reset, formation travel/preview, reentry and seek order. Stereo rendering and device performance still require Quest review.");
            }
            finally
            {
                EditorSceneManager.OpenScene(scenePath);
            }
        }

        private static void Require(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }
    }
}
