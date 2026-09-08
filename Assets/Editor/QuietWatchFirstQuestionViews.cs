using System.Collections.Generic;
using UnityEngine;
using StarshipCabin.QuietWatch;

namespace StarshipCabin.EditorTools
{
    /// <summary>Share seated, upward and standing views between real renders and the GPU motion audit.</summary>
    internal static class QuietWatchFirstQuestionViews
    {
        internal struct View
        {
            public string Name;
            public Vector3 Position;
            public Quaternion Rotation;
        }

        internal static IEnumerable<View> All(VistaCapturePoint[] points)
        {
            foreach (var point in points)
            {
                yield return new View { Name=point.CaptureName, Position=point.transform.position, Rotation=point.transform.rotation };
                yield return new View { Name=point.CaptureName+" upward", Position=point.transform.position,
                    Rotation=point.transform.rotation*Quaternion.Euler(-45f,0,0) };
            }
            var positions = new[] { new Vector3(0,1.7f,-.8f), new Vector3(-1.25f,1.65f,-1.1f), new Vector3(1.4f,1.65f,-.8f) };
            var names = new[] { "Standing upper window", "Standing left window", "Standing right window" };
            for (var i=0; i<positions.Length; i++)
                yield return new View { Name=names[i], Position=positions[i],
                    Rotation=Quaternion.LookRotation(new Vector3(.2f,2.15f,-2.1f)-positions[i]) };
        }
    }
}
