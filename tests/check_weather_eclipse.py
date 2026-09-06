"""Independent rays and clearances for the authored eclipse; not a GPU test."""
import re
from pathlib import Path
import numpy as np
from check_weather_geometry import center, sun, normal, origin, radius, inner, outer, unit
source=(Path(__file__).resolve().parents[1]/'Assets/Scripts/QuietWatch/GreatWeatherEclipse.cs').read_text()
def constant(name):return float(re.search(r'\b'+name+r' = (-?[\d.]+)f',source)[1])
orbit=constant('OrbitRadius'); moon_radius=constant('MoonRadius'); solar=constant('SolarAngularRadius')
tangent=unit(np.cross(normal,sun))
def position(progress):
    angle=np.radians(constant('StartAngle')+(constant('EndAngle')-constant('StartAngle'))*progress)
    return center+orbit*(sun*np.cos(angle)+tangent*np.sin(angle))
def smooth(a,b,x):
    t=np.clip((x-a)/(b-a),0,1);return t*t*(3-2*t)
def transmission(point,moon):
    vector=moon-point;along=np.dot(vector,sun);separation=np.linalg.norm(vector-sun*along)
    penumbra=max(.025,max(along,0)*solar)
    return smooth(max(0,moon_radius-penumbra),moon_radius+penumbra,separation) if along>0 else 1
for p in np.linspace(0,1,1001):
    moon=position(p)
    assert abs(np.linalg.norm(moon-center)-orbit)<1e-8
    assert orbit-moon_radius>radius, 'Moon intersects giant'
    # Exact closest distance to the finite annulus, not just its infinite plane.
    height=np.dot(moon-center,normal)
    radial=np.linalg.norm(moon-center-normal*height)
    gap=np.hypot(height,max(inner-radial,radial-outer,0))
    assert gap>moon_radius+.1, 'Moon intersects ring sheet'
    toMoon=moon-origin
    assert np.linalg.norm(toMoon)>moon_radius
print('PASS: all 1001 orbital samples clear the giant, rings and observer')
start=position(0)
assert np.linalg.norm(np.cross(start-center,sun)) > radius+moon_radius, 'Quiet starts with an eclipse'
# At default hold-B preview fraction .55, the smooth event phase is .57475.
for progress in (.55*.55*(3-2*.55),1):
    moon=position(progress)
    perpendicular=moon-center-sun*np.dot(moon-center,sun)
    hit=center+perpendicular+sun*np.sqrt(radius*radius-np.dot(perpendicular,perpendicular))
    assert np.dot(hit-center,origin-hit)>0, 'Shadow is on the hidden hemisphere'
    assert transmission(hit,moon)==0
    assert transmission(hit+moon_radius*3*tangent,moon)>.99
    assert transmission(moon+sun*10,moon)==1, 'Shadow incorrectly faces toward the sun'
    # Both real eye rays must see the lit surface receiving the eclipse.
    for eye in (origin+[-.032,0,0],origin+[.032,0,0]):
        assert np.dot(hit-center,eye-hit)>0
print('PASS: readable preview/final shadow faces both eyes and matches the physical sun ray')
# The approximate finite-source edge should be continuous and monotonic.
moon=position(.6);point=moon-sun*20
values=[transmission(point+tangent*x,moon) for x in np.linspace(moon_radius-.2,moon_radius+.2,101)]
assert np.all(np.diff(values)>=0) and values[0]==0 and values[-1]==1
print('PASS: finite-source shadow edge is monotonic, bounded, and does not eclipse sunward points')

# Reference fixed-seat framing (Unity camera default: 60 degree vertical, 3:2 captures).
# This is a cone check; only Unity captures establish glazing/frame occlusion.
preview=position(.55*.55*(3-2*.55))
perpendicular=preview-center-sun*np.dot(preview-center,sun)
shadow=center+perpendicular+sun*np.sqrt(radius**2-np.dot(perpendicular,perpendicular))
seats=[(origin,[0,1.45,-7]),(np.array([1.42,1.22,-.10]),[.4,1.55,-7]),
       (np.array([2.05,.95,-1]),[.6,1.55,-7]),(np.array([-2.2,1.18,2]),[-.5,1.4,-7])]
for eye,target in seats:
    forward=unit(np.array(target)-eye);right=unit(np.cross(forward,[0,1,0]));up=unit(np.cross(right,forward))
    for point in (preview,shadow):
        direction=point-origin # All eyes use the same astronomical reference projection.
        depth=np.dot(direction,forward)
        assert depth>0
        assert abs(np.dot(direction,right))+moon_radius < depth*np.tan(np.radians(30))*1.5
        assert abs(np.dot(direction,up))+moon_radius < depth*np.tan(np.radians(30))
print('PASS: preview moon and shadow fit all four reference capture cones with radius margin')

# A broad camera cone is not enough: the cabin has four discrete panes with
# solid mullions between them. Project each astronomical ray back onto the
# sloped hull and require a conservative 6 cm clearance inside one pane. This
# caught the original M7 preview, whose moon sat behind framing in three seats.
slope_origin=np.array([0.,.75,-2.6])
slope_up=unit(np.array([0.,1.75,1.2]))
slope_normal=np.cross([1.,0.,0.],slope_up)
pane_spans=[(-2.88,-1.92),(-1.56,-.60),(-.24,.72),(1.42,2.78)]
def glazing_coordinates(eye,point):
    direction=point-eye
    distance=np.dot(slope_origin-eye,slope_normal)/np.dot(direction,slope_normal)
    hit=eye+direction*distance
    return hit[0],np.dot(hit-slope_origin,slope_up)
def clears_a_pane(uv,margin=.06):
    u,v=uv
    if not .35+margin < v < 1.88-margin:return False
    top_inset=max(0.,(v-1.74)/.14*.10)
    return any(a+top_inset+margin < u < b-top_inset-margin for a,b in pane_spans)
for eye,target in seats:
    assert clears_a_pane(glazing_coordinates(eye,preview)), 'Preview moon is hidden by cabin framing'
    assert clears_a_pane(glazing_coordinates(eye,shadow)), 'Preview shadow is hidden by cabin framing'
print('PASS: preview moon and shadow clear a real glazing pane from all four seats')

# Bound the projected shadow ellipse too; its edge is wider than a surface dot.
for theta in np.linspace(0,2*np.pi,65):
    offset=(tangent*np.cos(theta)+np.cross(sun,tangent)*np.sin(theta))*(moon_radius+.25)
    ray_origin=preview+offset;relative=ray_origin-center;along=np.dot(relative,sun)
    hit=ray_origin-sun*(along-np.sqrt(radius**2-(np.dot(relative,relative)-along**2)))
    for eye,target in seats:
        forward=unit(np.array(target)-eye);right=unit(np.cross(forward,[0,1,0]));up=unit(np.cross(right,forward))
        direction=hit-origin;depth=np.dot(direction,forward)
        assert abs(np.dot(direction,right))/(depth*np.tan(np.radians(30))*1.5)<.97
        assert abs(np.dot(direction,up))/(depth*np.tan(np.radians(30)))<.97
print('PASS: conservative eclipse edge clears the reference capture borders')
