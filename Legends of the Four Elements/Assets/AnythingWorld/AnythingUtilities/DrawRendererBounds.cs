using UnityEngine;
namespace AnythingWorld.Utilities
{
    public class DrawRendererBounds : MonoBehaviour
    {
        // Draws a wireframe box around the selected object,
        // indicating world space bounding volume.
        public void OnDrawGizmosSelected()
        {
            var r = GetComponentInChildren<Renderer>();
            if (r == null)
                return;
            var bounds = r.bounds;
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(bounds.center, bounds.extents * 2);
        }
    }

}
