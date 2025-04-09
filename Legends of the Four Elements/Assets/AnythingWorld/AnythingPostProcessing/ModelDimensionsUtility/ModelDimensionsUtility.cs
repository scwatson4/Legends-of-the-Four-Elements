using UnityEngine;

namespace AnythingWorld.PostProcessing
{
    public static class ModelDimensionsUtility
    {
        public static bool TryGetDimensions(Transform tr, out Vector3 extents, out Vector3 center)
        {
            extents = Vector3.zero;
            center = Vector3.zero;

            var colliders = tr.GetComponentsInChildren<Collider>();

            if (colliders.Length != 0)
            {
                var overallBounds = colliders[0].bounds;
                
                for (int i = 1; i < colliders.Length; i++)
                {
                    overallBounds.Encapsulate(colliders[i].bounds);
                }
                
                extents = overallBounds.extents;
                center = overallBounds.center;

                return true;
            }
            
            var renderers = tr.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return false;
            }
            
            var totalBounds = renderers[0].bounds;
                
            for (int i = 1; i < renderers.Length; i++)
            {
                totalBounds.Encapsulate(renderers[i].bounds);
            }
                
            extents = totalBounds.extents;
            center = totalBounds.center;

            return true;
        }
    }
}
