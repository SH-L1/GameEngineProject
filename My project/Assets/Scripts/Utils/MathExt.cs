using UnityEngine;

namespace VoidEater.Utils
{
    public static class MathExt
    {
        public static float DiskArea(float radius)
        {
            radius = Mathf.Max(0.001f, radius);
            return Mathf.PI * radius * radius;
        }

        public static float BoundsVolume(Bounds bounds)
        {
            Vector3 size = bounds.size;
            return Mathf.Max(0.001f, size.x * size.y * size.z);
        }

        public static float HorizontalRadius(Bounds bounds)
        {
            Vector3 extents = bounds.extents;
            return Mathf.Max(extents.x, extents.z);
        }
    }
}
