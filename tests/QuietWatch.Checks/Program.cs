using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using StarshipCabin.QuietWatch;
using StarshipCabin.EditorTools;
using V3 = System.Numerics.Vector3;

var checks = 0;
void Check(bool ok, string label)
{
    if (!ok) throw new Exception("FAIL: " + label);
    checks++;
    Console.WriteLine("PASS: " + label);
}
void Near(double a, double b, string label, double tolerance = 1e-8) => Check(Math.Abs(a - b) < tolerance, label);
VistaTimeline Clock() { var value = new VistaTimeline(900, 240); value.Reset(true, true); return value; }
var clock = Clock();
Check(!clock.Advance(899.9), "event does not start before dwell");
Check(clock.Advance(0.2), "event starts on crossing dwell");
Near(clock.EventAge, 0.1, "event preserves boundary overshoot");
Check(!clock.Advance(20), "event start fires once");
var pose = clock.Progress;
clock.SetModes(false, false);
Near(clock.Progress, 0, "Quiet resets an underway event");
clock.Advance(7200);
Near(clock.Progress, 0, "Quiet keeps event reset across two hours");
var distance = clock.DriftTravel;
clock.SetModes(true, true);
Near(clock.DriftTravel, distance, "Drift mode does not rebase accumulated position");
Near(clock.Progress, 0, "returning to Living starts with no event pose");
Check(clock.Preview(), "event can be previewed after a mode reset");
Check(clock.Progress > 0.5, "preview jumps directly to a readable composition");
clock.Advance(1000);
Near(clock.Progress, 1, "event reaches stable final pose");
Check(!clock.Advance(10000) && clock.Preview(), "completed event can be replayed deliberately");
clock.Reset(true, true);
Near(clock.Progress, 0, "reentry resets event");
Near(clock.DriftTravel, 0, "reentry resets drift");
Check(clock.EventAge < 0, "reentry permits a new event");
var single = Clock(); var split = Clock();
single.Advance(7200); split.Advance(7200);
single.SetModes(false, false); split.SetModes(false, false);
single.Advance(20);
for (var i = 0; i < 2000; i++) split.Advance(0.01);
Near(single.DriftTravel, split.DriftTravel, "drift easing independent of frame partition", 1e-6);
Near(single.LivingTravel, split.LivingTravel, "traffic easing independent of frame partition", 1e-6);
Near(single.QuietTravel + single.LivingTravel, single.Elapsed, "traffic clock conserves elapsed observation time");
var invalid = Clock();
invalid.Advance(double.NaN); invalid.Advance(double.PositiveInfinity); invalid.Advance(-2); invalid.Advance(0);
Near(invalid.Elapsed, 0, "invalid deltas cannot corrupt clock");
var seek = Clock(); var run = Clock();
seek.Seek(1000, true, true);
for (var i = 0; i < 72000; i++) run.Advance(1d / 72);
Near(run.Progress, seek.Progress, "capture seek agrees with 72 Hz event simulation", 1e-7);
Near(run.DriftTravel, seek.DriftTravel, "capture seek agrees with 72 Hz drift simulation", 1e-6);
var still = Clock();
still.Advance(1200, false);
Check(still.EventAge < 0, "disabled event motion suppresses scheduling");
still.Advance(901, true);
var frozen = still.Progress;
still.Advance(10, false);
Near(still.Progress, frozen, "disabled event motion freezes an underway event");
var dwell = Clock(); dwell.Advance(899); dwell.SetModes(false, false); dwell.Advance(10); dwell.SetModes(true, false);
Check(!dwell.Advance(1), "return to Living requires a fresh uninterrupted dwell before first event");
var comet = Clock(); Check(comet.Preview(0, false), "short comet supports real-time preview"); comet.Advance(1);
Near(comet.EventAge, 1, "short preview does not accelerate the comet clock");

// Exercise the actual runtime shuttle clock, including both cycle boundaries.
Near(HarbourShuttleClock.Phase(0), 0, "shuttle starts at origin");
Near(HarbourShuttleClock.Phase(.45), 1, "shuttle dwells inside berth");
Near(HarbourShuttleClock.Phase(.95), 0, "shuttle rests at origin");
Near(HarbourShuttleClock.Phase(.2), HarbourShuttleClock.Phase(.7), "outbound and return share a corridor");
foreach (var boundary in new[] { .4, .5, .9, 1.0 })
    Near(HarbourShuttleClock.Phase(boundary - 1e-6), HarbourShuttleClock.Phase(boundary + 1e-6),
        "shuttle position continuous at " + boundary, 1e-8);
Near(HarbourShuttleClock.Phase(.23), HarbourShuttleClock.Phase(10000.23), "shuttle does not accumulate cycle drift", 1e-10);
Check(Enumerable.Range(0, 10001).All(i => HarbourShuttleClock.Phase(i / 10000d) is >= 0 and <= 1),
    "shuttle never leaves its audited corridor");

// Real bake ray code: bounded rays, both faces, parallel rays, then BVH vs brute force.
var face = new QuietWatchOcclusionBvh.Triangle(new V3(-1,-1,0),new V3(1,-1,0),new V3(0,1,0));
var one = new QuietWatchOcclusionBvh(new[] { face });
Check(one.Blocked(new V3(0,0,-1),V3.UnitZ,2), "bake ray reaches front face");
Check(one.Blocked(new V3(0,0,1),-V3.UnitZ,2), "bake ray reaches back face");
Check(!one.Blocked(new V3(0,0,-1),V3.UnitZ,.5f), "local occlusion respects distance");
Check(!one.Blocked(new V3(0,0,-1),V3.UnitX,10), "parallel ray does not divide by zero");
var random = new Random(871);
V3 RandomPoint() => new V3((float)random.NextDouble()*8-4,(float)random.NextDouble()*8-4,(float)random.NextDouble()*8-4);
var triangles = Enumerable.Range(0,150).Select(_ => new QuietWatchOcclusionBvh.Triangle(RandomPoint(),RandomPoint(),RandomPoint())).ToArray();
var treeBvh = new QuietWatchOcclusionBvh(triangles);
var agrees = true;
for (var i=0;i<1000;i++)
{
    var origin=RandomPoint();var direction=V3.Normalize(RandomPoint());
    agrees &= treeBvh.Blocked(origin,direction,3)==triangles.Any(t=>QuietWatchOcclusionBvh.Hit(t,origin,direction,3));
}
Check(agrees,"BVH agrees with all-triangle rays for 1000 seeded queries");

// Parser diagnostics are deliberately separate from Unity API/type checking.
var root = Path.GetFullPath(args.Length > 0 ? args[0] : ".");
var files = Directory.GetFiles(Path.Combine(root, "Assets"), "*.cs", SearchOption.AllDirectories);
foreach (var file in files)
{
    foreach (var symbols in new[] { new[] { "UNITY_EDITOR", "UNITY_ANDROID" }, new[] { "UNITY_ANDROID" } })
    {
        var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(file),
            new CSharpParseOptions(LanguageVersion.CSharp9, preprocessorSymbols: symbols), path: file);
        var errors = tree.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        if (errors.Length != 0) throw new Exception(string.Join("\n", errors.Select(e => e.ToString())));
    }
}
Console.WriteLine($"PASS: C# syntax in {files.Length} source files (Editor/Android symbols; not Unity compilation)");
Console.WriteLine($"Completed {checks} clock and ray checks.");
