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
                CheckEclipse(weather);
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
                QuartersDecor.ValidateChessLighting(false);
                Require(UnityEngine.Object.FindObjectsByType<QuietWatchSelectorPanel>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 0
                    && GameObject.Find("Quiet Watch Selector") == null && GameObject.Find("Console Pad") == null, "Backdrop sign must not be generated.");
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
                CheckHarbour(vistas.OfType<AuthoredVista>().Single(v => v.VistaId == "harbour"));
                CheckCinematic(vistas);
                QuietWatchTrafficValidation.ValidateOpenScene();
                Debug.Log("QUIET_WATCH_REVIEW_CHECKS PASS: scene identity, weather materials/geometry, shadow path, event reset, formation travel/preview, reentry and seek order. Stereo rendering and device performance still require Quest review.");
            }
            finally
            {
                EditorSceneManager.OpenScene(scenePath);
            }
        }

        private static void CheckCinematic(VistaEnvironment[] vistas)
        {
            Require(Camera.main.farClipPlane >= 20000f, "Deep planetary proxies exceed the camera range.");
            foreach (var v in vistas) v.gameObject.SetActive(false);
            var blue = vistas.OfType<AuthoredVista>().Single(v => v.VistaId == "blue-morning");
            blue.gameObject.SetActive(true); blue.Enter(LifeMode.Living, MotionMode.Still);
            blue.PreviewAt(60f, LifeMode.Living, MotionMode.Still);
            var clouds = blue.GetComponentsInChildren<Renderer>().Where(r => r.sharedMaterial.shader.name == "StarshipCabin/QuietWatchCloudDeck").ToArray();
            Require(clouds.Length == 2, "Missing raised clouds or aurora.");
            foreach (var cloud in clouds)
            {
                var block = new MaterialPropertyBlock(); cloud.GetPropertyBlock(block);
                Require(Mathf.Abs(block.GetFloat("_ObservationTime")-60f)<.001f, "Atmospheric layers lost the deterministic clock.");
                Require(cloud.GetComponent<DistantVistaBounds>() != null, "Cloud layer misses stereo bounds.");
            }
            blue.Exit();
            var hulls = vistas.SelectMany(v => v.GetComponentsInChildren<MeshRenderer>(true))
                .Where(r => r.sharedMaterials.Any(m => m.shader.name == "StarshipCabin/QuietWatchHull")).ToArray();
            Require(hulls.Length > 10, "Geometric hull lighting bake is missing.");
            var shaded = 0; var shadowed = 0;
            foreach (var hull in hulls)
            {
                var mesh = hull.GetComponent<MeshFilter>().sharedMesh; var colors = mesh.colors;
                Require(colors.Length == mesh.vertexCount, "Missing baked vertex visibility.");
                foreach (var color in colors)
                {
                    Require(color.r >= 0 && color.r <= 1 && color.g >= .23f && color.g <= 1, "Invalid baked occlusion values.");
                    if (color.g < .9f) shaded++;
                    if (color.r < .5f) shadowed++;
                }
                foreach (var material in hull.sharedMaterials)
                    Require(material.shader.isSupported && !ShaderUtil.ShaderHasError(material.shader), "Exterior hull shader failed.");
            }
            Require(shaded > 0 && shadowed > 0, "Bake produced no structural occlusion/shadows.");
            var stars = UnityEngine.Object.FindAnyObjectByType<StarWindowSurface>();
            var sky = stars.GetComponent<Renderer>();
            Require(sky.sharedMaterial.GetTexture("_GalacticMap") != null, "Missing authored galactic panorama.");
            var first = vistas.OfType<FirstQuestionVista>().Single(); first.gameObject.SetActive(true);
            first.Enter(LifeMode.Quiet, MotionMode.Still);
            var skyBlock = new MaterialPropertyBlock(); sky.GetPropertyBlock(skyBlock);
            Require(Mathf.Abs(skyBlock.GetFloat("_GalacticGain")) < .001f, "First Question must be dust-free.");
            var cruise=first.GetComponentsInChildren<Renderer>().Single(r=>r.sharedMaterial.shader.name=="StarshipCabin/QuietWatchCruiseStars");
            Require(cruise.sharedMaterial.shader.isSupported && !ShaderUtil.ShaderHasError(cruise.sharedMaterial.shader),"Cruise star shader failed.");
            float CruiseTravel() { var b=new MaterialPropertyBlock();cruise.GetPropertyBlock(b);return b.GetFloat("_Travel"); }
            first.PreviewAt(0,LifeMode.Quiet,MotionMode.Still);Require(CruiseTravel()==0,"Cruise must begin stationary.");
            first.PreviewAt(12,LifeMode.Quiet,MotionMode.Drift);var travel=CruiseTravel();
            Require(travel>1000 && travel<1400,"Cruise depth travel is missing or incorrectly eased.");
            first.ApplyComfort(LifeMode.Living,MotionMode.Drift);Require(CruiseTravel()==travel,"Life toggle rebases cruise.");
            first.PreviewAt(30,LifeMode.Quiet,MotionMode.Drift);first.PreviewAt(12,LifeMode.Quiet,MotionMode.Drift);
            Require(Mathf.Abs(CruiseTravel()-travel)<.001f,"Cruise capture depends on previous pose.");
            first.PreviewAt(600,LifeMode.Quiet,MotionMode.Still);Require(CruiseTravel()==0,"Still stars drift.");
            foreach(var fleet in vistas.OfType<AuthoredVista>().Where(v=>v.VistaId=="harbour" || v.VistaId=="long-formation"))
            {
                Require(!fleet.GetComponentsInChildren<Transform>(true).Any(t=>t.name.StartsWith("Drive Glow")),"Detached generic drive spheres returned.");
                foreach(var group in fleet.GetComponentsInChildren<LODGroup>(true).Where(g=>g.name!="Kilometre Harbour Sector"))
                    foreach(var lod in group.GetLODs())
                        Require(lod.renderers.SelectMany(r=>r.sharedMaterials).Any(m=>m.HasProperty("_EmissionColor") && m.GetColor("_EmissionColor").b>2f),"Imported engine aperture emission missing in a ship LOD.");
            }
            Require(GameObject.Find("Personal Desk Computer")!=null && GameObject.Find("Computer Recessed Screen")!=null,"Personal computer is missing.");
            foreach(var book in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name.StartsWith("Book:") && t.parent != null && t.parent.name=="Furnishings"))
                Require(book.GetComponentsInChildren<MeshFilter>().Length>=3,"Book lost its separate binding/pages.");
            first.Exit(); blue.gameObject.SetActive(true); blue.Enter(LifeMode.Quiet, MotionMode.Still);
            sky.GetPropertyBlock(skyBlock);
            Require(skyBlock.GetFloat("_GalacticGain") < .1f, "Galactic exposure leaks into a planetary vista.");
            blue.Exit();
        }

        private static void CheckHarbour(AuthoredVista harbour)
        {
            harbour.gameObject.SetActive(true);
            harbour.Enter(LifeMode.Living, MotionMode.Still);
            var station = harbour.transform.Find("Kilometre Harbour Sector");
            var lods = station.GetComponent<LODGroup>().GetLODs();
            Require(lods.Length == 3, "Harbour must retain three LODs.");
            Require(station.GetComponent<LODGroup>().size * station.lossyScale.x > 1000f,"Harbour lost its kilometre-scale silhouette.");
            Require(harbour.GetComponentsInChildren<HarbourTrafficRoute>(true).Length==6,"Missing port traffic lanes.");
            foreach (var lod in lods)
            {
                var districts = lod.renderers.Where(r => r.name.StartsWith("Harbour Districts")).ToArray();
                Require(districts.Length is >= 4 and <= 5, "District meshes are missing or unbatched.");
                Require(districts.Any(r=>r.name.EndsWith("Surface3") && r.sharedMaterial.shader.name=="Universal Render Pipeline/Unlit"),"Occupied decks lost luminous windows in a LOD.");
                foreach (var renderer in districts)
                {
                    Require(renderer.gameObject.layer == QuietWatchArtAssetBuilder.ExteriorLayer, "District missed exterior lighting layer.");
                    Require(renderer.sharedMaterial.shader.isSupported && !ShaderUtil.ShaderHasError(renderer.sharedMaterial.shader),
                        "District shader failed.");
                    var mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                    Require(mesh != null && mesh.vertexCount < (renderer.name.EndsWith("Surface3") ? 40000 : 20000), "District geometry exceeds its per-surface budget.");
                    var vertices = mesh.vertices; var triangles = mesh.triangles;
                    for (var i = 0; i < triangles.Length; i += 3)
                        Require(Vector3.Cross(vertices[triangles[i+1]]-vertices[triangles[i]],
                            vertices[triangles[i+2]]-vertices[triangles[i]]).sqrMagnitude > 1e-14f,
                            "Degenerate harbour district triangle.");
                }
            }
            var tender = harbour.GetComponentsInChildren<HarbourTrafficRoute>(true).Single(r => r.IsShuttle);
            Require(tender.AvailableInQuiet, "Service traffic must be visible without waiting for an event.");
            harbour.PreviewAt(32f, LifeMode.Living, MotionMode.Still);
            tender.Evaluate(1f, out var berth, out _, out _);
            Require(Vector3.Distance(tender.transform.localPosition, berth) < 0.001f, "Tender does not dwell in the berth at 32 seconds.");
            var docked = tender.transform.localPosition;
            harbour.ApplyComfort(LifeMode.Quiet, MotionMode.Still);
            Require(Vector3.Distance(tender.transform.localPosition, docked) < 0.001f, "Life switch teleports tender.");
            harbour.PreviewAt(90f, LifeMode.Living, MotionMode.Still);
            harbour.PreviewAt(32f, LifeMode.Living, MotionMode.Still);
            Require(Vector3.Distance(tender.transform.localPosition, docked) < 0.001f, "Harbour seek depends on previous pose.");
            harbour.Exit();
        }

        private static void CheckEclipse(AuthoredVista weather)
        {
            var eclipse = weather.GetComponent<GreatWeatherEclipse>();
            Require(eclipse != null, "Missing clock-driven eclipse.");
            var moon = weather.transform.Find("Eclipse Moon");
            Require(moon != null && Mathf.Abs(moon.lossyScale.x * 0.5f - GreatWeatherEclipse.MoonRadius) < 0.0001f,
                "Eclipse geometry and shadow radius differ.");
            weather.Enter(LifeMode.Living, MotionMode.Still);
            var baseline = moon.localPosition;
            Require(weather.PreviewGraceNote(), "Eclipse preview must work in Still.");
            const float previewPhase = 0.55f * 0.55f * (3f - 2f * 0.55f);
            Require(Vector3.Distance(moon.localPosition, eclipse.PositionAt(previewPhase)) < 0.001f,
                "Hold-B preview does not seek the readable eclipse phase.");
            void CheckReceivers()
            {
                var receivers = weather.GetComponentsInChildren<Renderer>()
                    .Where(r => r.sharedMaterial.shader.name == "StarshipCabin/QuietWatchGasGiant"
                        || r.sharedMaterial.shader.name == "StarshipCabin/QuietWatchRings"
                        || r.sharedMaterial.shader.name == "StarshipCabin/QuietWatchAtmosphere").ToArray();
                Require(receivers.Length == 3, "Planet, rings and atmosphere must share the eclipse.");
                foreach (var receiver in receivers)
                {
                    var block = new MaterialPropertyBlock();
                    receiver.GetPropertyBlock(block);
                    var sphere = block.GetVector("_OccultorSphere");
                    Require(Vector3.Distance((Vector3)sphere, moon.position) < 0.0001f
                        && Mathf.Abs(sphere.w - GreatWeatherEclipse.MoonRadius) < 0.0001f,
                        "Eclipse shadow detached from the moving moon.");
                    var companion = weather.transform.Find("Far Moon");
                    var staticSphere = receiver.sharedMaterial.GetVector("_CompanionSphere");
                    Require(Vector3.Distance((Vector3)staticSphere, companion.position) < .001f
                        && Mathf.Abs(staticSphere.w - companion.lossyScale.x * .5f) < .001f,
                        "Companion moon geometry and shared shadow differ.");
                    Require(Mathf.Abs(receiver.sharedMaterial.GetFloat("_SolarAngularRadius")
                        - GreatWeatherEclipse.SolarAngularRadius) < 1e-6f, "Mismatched eclipse penumbra.");
                }
            }
            CheckReceivers();
            var middle = weather.GraceNoteAtSeconds + weather.GraceDurationSeconds * 0.75f;
            weather.PreviewAt(middle, LifeMode.Living, MotionMode.Still);
            var expected = moon.localPosition;
            weather.PreviewAt(weather.GraceNoteAtSeconds, LifeMode.Living, MotionMode.Still);
            weather.PreviewAt(middle, LifeMode.Living, MotionMode.Still);
            Require(Vector3.Distance(expected, moon.localPosition) < 0.0001f, "Eclipse depends on seek order.");
            CheckReceivers();
            weather.ApplyComfort(LifeMode.Quiet, MotionMode.Still);
            Require(Vector3.Distance(baseline, moon.localPosition) < 0.0001f, "Quiet leaves the eclipse in progress.");
            CheckReceivers();
            var relief = weather.transform.Find("Ringed Giant").GetComponent<Renderer>().sharedMaterial.GetTexture("_CloudRelief");
            Require(relief != null, "Missing authored cloud relief.");
            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(relief)) as TextureImporter;
            Require(importer != null && !importer.sRGBTexture && importer.mipmapEnabled, "Cloud relief import must be linear with mipmaps.");
        }

        private static void Require(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }
    }
}
