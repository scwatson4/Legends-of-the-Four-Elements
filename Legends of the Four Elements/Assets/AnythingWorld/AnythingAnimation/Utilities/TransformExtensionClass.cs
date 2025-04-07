using System.Collections.Generic;
using UnityEngine;

namespace AnythingWorld.Animation
{
    /// <summary>
    /// A static class that provides an extension method to the Transform class.
    /// </summary>
    public static class TransformExtensionClass
    {
        /// <summary>
        /// Searches for a child transform with the specified name recursively starting from the parent transform.
        /// </summary>
        /// <param name="aParent">The parent transform to start the search from.</param>
        /// <param name="aName">The name of the child transform to find.</param>
        /// <returns>The child transform with the specified name if found; otherwise, null.</returns>
        public static Transform FindDeepChild(this Transform aParent, string aName)
        {
            Queue<Transform> queue = new Queue<Transform>();
            queue.Enqueue(aParent);
            while (queue.Count > 0)
            {
                var c = queue.Dequeue();
                if (c.name == aName)
                    return c;
                foreach (Transform t in c)
                    queue.Enqueue(t);
            }
            return null;
        }
    }
}

