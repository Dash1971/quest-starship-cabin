using System;
using System.Collections.Generic;
using V3 = System.Numerics.Vector3;

namespace StarshipCabin.EditorTools
{
    /// <summary>Engine-independent, two-sided triangle rays for deterministic editor baking.</summary>
    internal sealed class QuietWatchOcclusionBvh
    {
        internal struct Triangle
        {
            public V3 A, B, C;
            public Triangle(V3 a, V3 b, V3 c) { A=a; B=b; C=c; }
            public V3 Center => (A+B+C)/3f;
        }
        private struct Node { public V3 Min, Max; public int Start, Count, Left, Right; }
        private sealed class CenterComparer : IComparer<Triangle>
        {
            private readonly int axis;
            public CenterComparer(int value) { axis=value; }
            public int Compare(Triangle a, Triangle b) => Coordinate(a.Center,axis).CompareTo(Coordinate(b.Center,axis));
        }
        private readonly Triangle[] triangles;
        private readonly List<Node> nodes = new List<Node>();
        public QuietWatchOcclusionBvh(Triangle[] input)
        {
            triangles=(Triangle[])input.Clone();
            if (triangles.Length>0) Build(0,triangles.Length);
        }
        private static float Coordinate(V3 v,int axis) => axis==0?v.X:axis==1?v.Y:v.Z;
        private int Build(int start,int count)
        {
            var min=new V3(float.PositiveInfinity);var max=new V3(float.NegativeInfinity);
            for(var i=start;i<start+count;i++)
            {
                var t=triangles[i];min=V3.Min(min,V3.Min(t.A,V3.Min(t.B,t.C)));max=V3.Max(max,V3.Max(t.A,V3.Max(t.B,t.C)));
            }
            var index=nodes.Count;nodes.Add(default);
            var node=new Node {Min=min,Max=max,Start=start,Count=count,Left=-1,Right=-1};
            if(count>8)
            {
                var span=max-min;var axis=span.X>=span.Y&&span.X>=span.Z?0:span.Y>=span.Z?1:2;
                Array.Sort(triangles,start,count,new CenterComparer(axis));
                var half=count/2;node.Left=Build(start,half);node.Right=Build(start+half,count-half);node.Count=0;
            }
            nodes[index]=node;return index;
        }
        public bool Blocked(V3 origin,V3 direction,float distance) => nodes.Count>0&&distance>0&&Visit(0,origin,direction,distance);
        private bool Visit(int index,V3 origin,V3 direction,float distance)
        {
            var node=nodes[index];var enter=0f;var exit=distance;
            for(var axis=0;axis<3;axis++)
            {
                var o=Coordinate(origin,axis);var d=Coordinate(direction,axis);
                var lo=Coordinate(node.Min,axis);var hi=Coordinate(node.Max,axis);
                if(Math.Abs(d)<1e-10f) { if(o<lo||o>hi)return false;continue; }
                var a=(lo-o)/d;var b=(hi-o)/d;
                enter=Math.Max(enter,Math.Min(a,b));exit=Math.Min(exit,Math.Max(a,b));if(enter>exit)return false;
            }
            if(node.Count==0)return Visit(node.Left,origin,direction,distance)||Visit(node.Right,origin,direction,distance);
            for(var i=node.Start;i<node.Start+node.Count;i++)if(Hit(triangles[i],origin,direction,distance))return true;
            return false;
        }
        internal static bool Hit(Triangle triangle,V3 origin,V3 direction,float maximum)
        {
            var e1=triangle.B-triangle.A;var e2=triangle.C-triangle.A;var p=V3.Cross(direction,e2);
            var determinant=V3.Dot(e1,p);if(Math.Abs(determinant)<1e-9f)return false;
            var t=origin-triangle.A;var u=V3.Dot(t,p)/determinant;if(u<0||u>1)return false;
            var q=V3.Cross(t,e1);var v=V3.Dot(direction,q)/determinant;if(v<0||u+v>1)return false;
            var length=V3.Dot(e2,q)/determinant;return length>1e-5f&&length<maximum;
        }
    }
}
