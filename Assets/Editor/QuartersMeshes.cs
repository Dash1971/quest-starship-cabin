using System.Collections.Generic;
using UnityEngine;

namespace StarshipCabin.EditorTools
{
    /// <summary>
    /// Flat-shaded procedural mesh accumulator plus contour helpers for the
    /// quarters geometry. Every face duplicates its vertices so the result is
    /// crisply flat-shaded, which suits the panelled interior style and keeps
    /// lightmap unwrapping trivial.
    /// </summary>
    public class MeshDraft
    {
        private readonly List<Vector3> vertices = new();
        private readonly List<Vector3> normals = new();
        private readonly List<Vector2> uvs = new();
        private readonly List<int> triangles = new();

        public void AddTriangle(Vector3 a, Vector3 b, Vector3 c)
        {
            var normal = Vector3.Cross(b - a, c - a).normalized;
            var start = vertices.Count;

            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);

            for (var i = 0; i < 3; i++)
            {
                normals.Add(normal);
            }

            uvs.Add(PlanarUv(a, normal));
            uvs.Add(PlanarUv(b, normal));
            uvs.Add(PlanarUv(c, normal));

            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
        }

        public void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            AddTriangle(a, b, c);
            AddTriangle(a, c, d);
        }

        public void AddQuadOriented(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 desiredNormal)
        {
            var winding = Vector3.Cross(b - a, c - a);
            if (Vector3.Dot(winding, desiredNormal) >= 0f)
            {
                AddQuad(a, b, c, d);
            }
            else
            {
                AddQuad(d, c, b, a);
            }
        }

        public void AddConvexPolygon(IList<Vector3> points, Vector3 desiredNormal)
        {
            if (points.Count < 3)
            {
                return;
            }

            var ordered = new List<Vector3>(points);
            if (Vector3.Dot(NewellNormal(ordered), desiredNormal) < 0f)
            {
                ordered.Reverse();
            }

            for (var i = 1; i < ordered.Count - 1; i++)
            {
                AddTriangle(ordered[0], ordered[i], ordered[i + 1]);
            }
        }

        /// <summary>Planar ring between two aligned closed contours (e.g. a window frame face).</summary>
        public void AddRing(IList<Vector3> inner, IList<Vector3> outer, Vector3 desiredNormal)
        {
            var count = Mathf.Min(inner.Count, outer.Count);
            for (var i = 0; i < count; i++)
            {
                var next = (i + 1) % count;
                AddQuadOriented(inner[i], inner[next], outer[next], outer[i], desiredNormal);
            }
        }

        /// <summary>
        /// Side wall between two closed contours (an extrusion skirt). Faces are
        /// oriented outward from the contour centroid, or inward when
        /// <paramref name="faceOutward"/> is false (e.g. a window reveal).
        /// </summary>
        public void AddSkirt(IList<Vector3> from, IList<Vector3> to, bool faceOutward)
        {
            var count = Mathf.Min(from.Count, to.Count);
            var centroid = Vector3.zero;
            for (var i = 0; i < count; i++)
            {
                centroid += from[i];
            }
            centroid /= count;

            for (var i = 0; i < count; i++)
            {
                var next = (i + 1) % count;
                var p0 = from[i];
                var p1 = from[next];
                var p2 = to[next];
                var p3 = to[i];

                var faceNormal = Vector3.Cross(p1 - p0, p3 - p0);
                var mid = (p0 + p1 + p2 + p3) * 0.25f;
                var radial = mid - centroid;
                var pointsOutward = Vector3.Dot(faceNormal, radial) > 0f;

                if (pointsOutward == faceOutward)
                {
                    AddQuad(p0, p1, p2, p3);
                }
                else
                {
                    AddQuad(p3, p2, p1, p0);
                }
            }
        }

        public Mesh ToMesh(string name)
        {
            var mesh = new Mesh
            {
                name = name,
                indexFormat = vertices.Count > 65000
                    ? UnityEngine.Rendering.IndexFormat.UInt32
                    : UnityEngine.Rendering.IndexFormat.UInt16
            };

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Vector3 NewellNormal(IList<Vector3> pts)
        {
            var normal = Vector3.zero;
            for (var i = 0; i < pts.Count; i++)
            {
                var current = pts[i];
                var next = pts[(i + 1) % pts.Count];
                normal.x += (current.y - next.y) * (current.z + next.z);
                normal.y += (current.z - next.z) * (current.x + next.x);
                normal.z += (current.x - next.x) * (current.y + next.y);
            }
            return normal.normalized;
        }

        private static Vector2 PlanarUv(Vector3 v, Vector3 n)
        {
            var ax = Mathf.Abs(n.x);
            var ay = Mathf.Abs(n.y);
            var az = Mathf.Abs(n.z);

            if (ax >= ay && ax >= az)
            {
                return new Vector2(v.z, v.y);
            }

            return ay >= az ? new Vector2(v.x, v.z) : new Vector2(v.x, v.y);
        }
    }

    public static class QuartersMeshes
    {
        /// <summary>
        /// Convex quad contour (bl→br→tr→tl, counter-clockwise) with rounded
        /// corners. Returns 4 × (cornerSegments + 1) points. Pass a small radius
        /// (e.g. 0.02) for a near-rectangle so point counts stay aligned with a
        /// rounded inner contour.
        /// </summary>
        public static List<Vector2> RoundedQuadContour(
            Vector2 bl, Vector2 br, Vector2 tr, Vector2 tl, float radius, int cornerSegments)
        {
            var corners = new[] { bl, br, tr, tl };
            var result = new List<Vector2>(4 * (cornerSegments + 1));

            for (var i = 0; i < 4; i++)
            {
                var prev = corners[(i + 3) % 4];
                var current = corners[i];
                var next = corners[(i + 1) % 4];

                var toPrev = (prev - current).normalized;
                var toNext = (next - current).normalized;
                var interiorAngle = Vector2.Angle(toPrev, toNext) * Mathf.Deg2Rad;
                var half = interiorAngle * 0.5f;

                var tangentDistance = radius / Mathf.Tan(half);
                var centerDistance = radius / Mathf.Sin(half);
                var bisector = (toPrev + toNext).normalized;
                var center = current + bisector * centerDistance;

                var arcStart = current + toPrev * tangentDistance;
                var arcEnd = current + toNext * tangentDistance;

                var startAngle = Mathf.Atan2(arcStart.y - center.y, arcStart.x - center.x) * Mathf.Rad2Deg;
                var endAngle = Mathf.Atan2(arcEnd.y - center.y, arcEnd.x - center.x) * Mathf.Rad2Deg;
                var delta = Mathf.DeltaAngle(startAngle, endAngle);

                for (var s = 0; s <= cornerSegments; s++)
                {
                    var t = s / (float)cornerSegments;
                    var angle = (startAngle + delta * t) * Mathf.Deg2Rad;
                    result.Add(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
                }
            }

            return result;
        }

        /// <summary>Maps 2D contour points into a 3D plane frame with an offset along the plane normal.</summary>
        public static List<Vector3> MapToPlane(
            IList<Vector2> contour, Vector3 origin, Vector3 uDir, Vector3 vDir, Vector3 normal, float offset)
        {
            var result = new List<Vector3>(contour.Count);
            foreach (var p in contour)
            {
                result.Add(origin + uDir * p.x + vDir * p.y + normal * offset);
            }
            return result;
        }

        /// <summary>Axis-aligned box with chamfered long edges (octagonal cross-section, extruded along local Z).</summary>
        public static Mesh ChamferedBox(string name, float sizeX, float sizeY, float sizeZ, float chamfer)
        {
            var hx = sizeX * 0.5f;
            var hy = sizeY * 0.5f;
            var hz = sizeZ * 0.5f;
            var ch = Mathf.Min(chamfer, Mathf.Min(hx, hy) * 0.9f);

            var profile = new List<Vector2>
            {
                new(hx - ch, -hy),
                new(hx, -hy + ch),
                new(hx, hy - ch),
                new(hx - ch, hy),
                new(-hx + ch, hy),
                new(-hx, hy - ch),
                new(-hx, -hy + ch),
                new(-hx + ch, -hy)
            };

            var front = new List<Vector3>(profile.Count);
            var back = new List<Vector3>(profile.Count);
            foreach (var p in profile)
            {
                front.Add(new Vector3(p.x, p.y, hz));
                back.Add(new Vector3(p.x, p.y, -hz));
            }

            var draft = new MeshDraft();
            draft.AddSkirt(front, back, faceOutward: true);
            draft.AddConvexPolygon(front, Vector3.forward);
            draft.AddConvexPolygon(back, Vector3.back);
            return draft.ToMesh(name);
        }

        /// <summary>Single quad with explicit 0..1 UVs (used by the star window surface).</summary>
        public static Mesh UvQuad(string name, Vector3 p00, Vector3 p10, Vector3 p11, Vector3 p01)
        {
            var normal = Vector3.Cross(p10 - p00, p01 - p00).normalized;
            var mesh = new Mesh { name = name };
            mesh.SetVertices(new List<Vector3> { p00, p10, p11, p01 });
            mesh.SetNormals(new List<Vector3> { normal, normal, normal, normal });
            mesh.SetUVs(0, new List<Vector2>
            {
                new(0f, 0f), new(1f, 0f), new(1f, 1f), new(0f, 1f)
            });
            mesh.SetTriangles(new[] { 0, 1, 2, 0, 2, 3 }, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
