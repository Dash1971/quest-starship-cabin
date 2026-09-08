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

var cruiseClock = new VistaTimeline(780,8);
cruiseClock.Reset(false,false);cruiseClock.Advance(30);
Near(cruiseClock.DriftTravel,0,"cruise remains still before explicit start");
cruiseClock.SetModes(false,true);cruiseClock.Advance(12);
Near(cruiseClock.DriftTravel,12-2*(1-Math.Exp(-6)),"cruise starts with integrated easing",1e-8);
var atStop=cruiseClock.DriftTravel;var speedAtStop=cruiseClock.DriftSpeed;
cruiseClock.SetModes(false,false);
Near(cruiseClock.DriftTravel,atStop,"stop does not jump star positions");
cruiseClock.Advance(30);
Near(cruiseClock.DriftTravel-atStop,2*speedAtStop*(1-Math.Exp(-15)),"cruise decelerates without resetting",1e-8);
var afterStop=cruiseClock.DriftTravel;cruiseClock.Advance(60);
Near(cruiseClock.DriftTravel,afterStop,"stopped cruise settles",1e-6);

// Test motion through Unity's left-handed camera basis: looking -Z means right=-X.
var catalogue=FirstQuestionField.Catalogue();
Check(catalogue.Length==24576 && catalogue.Length*4==98304,"full-sky field has a bounded 98,304-vertex budget");
Check(catalogue.All(s=>Math.Sqrt((double)s.Y*s.Y+(double)s.Z*s.Z)>=7999 && s.Flux>0 && s.Sigma>=.5 && s.Scale is .25f or .5f or 1f),"all stellar transverse distances, cell scales and filtered cores are bounded");
var forward=V3.Normalize(new V3(0,0,-1));var cameraRight=V3.Normalize(V3.Cross(V3.UnitY,forward));
var wrong=V3.Dot(new V3(-72,0,0),cameraRight);
Check(wrong>0,"regression reproduces old -X translation as screen-RIGHT");
var everyStarMovesLeft=true;var fixedDepth=true;var coherent=true;var continuous=true;
for(var i=0;i<catalogue.Length;i++)
{
    var a=FirstQuestionField.At(catalogue[i],i,0);var b=FirstQuestionField.At(catalogue[i],i,1);
    if(a.Visibility>.99 && b.Visibility>.99)
    {
        if(a.Z<0) everyStarMovesLeft &= -b.X/(-b.Z)<-a.X/(-a.Z);
        fixedDepth &= a.Y==b.Y && a.Z==b.Z;
        coherent &= Math.Abs(b.X-a.X-FirstQuestionField.Speed)<1e-5;
    }
    var edge=(FirstQuestionField.Period*catalogue[i].Scale*.5-catalogue[i].X)/FirstQuestionField.Speed;
    var before=FirstQuestionField.At(catalogue[i],i,edge-1e-5);
    var after=FirstQuestionField.At(catalogue[i],i,edge+1e-5);
    continuous &= before.Visibility<1e-8 && after.Visibility<1e-8;
}
Check(everyStarMovesLeft,"ALL visible stars move screen-left toward the correct relative-space heading");
Check(fixedDepth && coherent,"every point shares +X translation; no independent angular scroll or approach");
Check(continuous,"every star recycles only at zero opacity");
foreach(var seconds in new[]{60.0,600,3600,7200})
{
    var allFinite=true;
    for(var i=0;i<catalogue.Length;i+=13)
    {
        var a=FirstQuestionField.At(catalogue[i],i,seconds);
        var b=FirstQuestionField.At(catalogue[i],i,seconds+.02);
        allFinite &= double.IsFinite(a.X+a.Y+a.Z) && a.Visibility>=0 && a.Visibility<=1;
        if(a.Visibility>.99 && b.Visibility>.99) allFinite &= b.X>a.X && a.Z==b.Z;
    }
    Check(allFinite,$"stellar flow remains coherent after {seconds/60:0} minutes, including sector changes");
}
var cometPosition=FirstQuestionField.CometAt(36,0);var cometLater=FirstQuestionField.CometAt(46,10);
Near(cometLater.X-cometPosition.X,FirstQuestionField.Speed*10,"comet shares the same cabin-relative translation");
Check(FirstQuestionField.CometAt(-1,0).Visibility==0 && FirstQuestionField.CometAt(0,0).Visibility==0
    && FirstQuestionField.CometAt(36,0).Visibility==1 && FirstQuestionField.CometAt(120,0).Visibility==0,"distant comet fades gently over two minutes");
Check(FirstQuestionField.CometTailDegrees>=5 && FirstQuestionField.CometTailDegrees<=7 && FirstQuestionField.CometDuration>=120,"comet has a readable tail while preserving the slow two-minute passage");
var cometClock=new VistaTimeline(FirstQuestionField.CometDelay,FirstQuestionField.CometDuration);
cometClock.Reset(true,false);cometClock.Preview(.3,false);Near(cometClock.EventAge,36,"hold-B starts in a readable distant-comet phase");
cometClock.Advance(1);Near(cometClock.EventAge,37,"distant-comet preview runs at real time");
cometClock.SetModes(false,false);Near(cometClock.EventAge,-1,"Quiet cancels the distant comet immediately");
var eventFrames=new VistaTimeline(FirstQuestionField.CometDelay,FirstQuestionField.CometDuration);
var eventSeek=new VistaTimeline(FirstQuestionField.CometDelay,FirstQuestionField.CometDuration);
eventFrames.Reset(true,false);eventFrames.SetModes(true,true);eventSeek.Reset(true,false);eventSeek.SetModes(true,true);
for(var frame=0;frame<836*72;frame++)eventFrames.Advance(1.0/72);
eventSeek.Advance(836);
Near(eventFrames.DriftAtEventStart,eventSeek.DriftAtEventStart,"comet travel origin is identical in 72 Hz playback and direct capture",1e-6);
eventFrames.Preview(.3,false);Near(eventFrames.DriftAtEventStart,eventFrames.DriftTravel,"late hold-B brings comet into view at current cruise position");
if(args.Length>1)
{
    var model=new {period=FirstQuestionField.Period,speed=FirstQuestionField.Speed,nearRadius=FirstQuestionField.NearDepth,farRadius=FirstQuestionField.FarDepth,stars=catalogue,
        comet=FirstQuestionField.CometAt(36,0),cometTailDegrees=FirstQuestionField.CometTailDegrees,
        cometHalfWidthDegrees=FirstQuestionField.CometHalfWidthDegrees,
        checkpoints=new[]{0d,12,228,456,912,3600,7200}.Select(seconds=>new {seconds,
            positions=catalogue.Select((star,index)=>FirstQuestionField.At(star,index,seconds-2*(1-Math.Exp(-seconds/2)))).ToArray()}).ToArray()};
    File.WriteAllText(args[1],System.Text.Json.JsonSerializer.Serialize(model,new System.Text.Json.JsonSerializerOptions{IncludeFields=true}));
}

// A hold-B/replay/Quiet command must not teleport fleet attitude or angular velocity.
var maneuver=new FormationManeuver();
foreach(var target in new[]{.575,0,1,.575,0})
{
    var value=maneuver.Value;var velocity=maneuver.Velocity;
    maneuver.Advance(0,target);
    Near(maneuver.Value,value,"formation retarget preserves attitude");
    Near(maneuver.Velocity,velocity,"formation retarget preserves angular velocity");
    var previous=maneuver.Value;
    for(var frame=0;frame<12*72;frame++)
    {
        maneuver.Advance(1d/72,target);
        if(Math.Abs(maneuver.Value-previous)>.006)throw new Exception("Fleet manoeuvre stepped visibly");
        previous=maneuver.Value;
    }
    Check(Math.Abs(maneuver.Value-target)<.001,"formation reaches the requested manoeuvre smoothly");
}
var ma=new FormationManeuver();var mb=new FormationManeuver();
ma.Advance(8,.8);for(var i=0;i<720;i++)mb.Advance(8d/720,.8);
Near(ma.Value,mb.Value,"fleet smoothing is frame-partition independent for a held target");
Near(ma.Velocity,mb.Velocity,"fleet angular velocity is frame-partition independent");
foreach(var age in new[]{.25,40.0,84,110})
{
    ma.SeekScheduled(age,84);mb.Reset();double time=0;
    while(time<age)
    {
        var dt=Math.Min(1d/90,age-time);time+=dt;
        var t=Math.Clamp(time/84,0,1);mb.Advance(dt,t*t*(3-2*t));
    }
    Near(ma.Value,mb.Value,"scheduled fleet capture agrees with 90 Hz playback at "+age, .00003);
}

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
Console.WriteLine($"Completed {checks} clock, ray and stellar-field checks.");
