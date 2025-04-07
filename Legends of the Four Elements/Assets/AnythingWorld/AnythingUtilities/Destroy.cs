using UnityEngine;

namespace AnythingWorld.Utilities
{
    /// <summary>
    /// Provides methods to destroy GameObjects and MonoBehaviours depending on Editor or Build context.
    /// </summary>
    public static class Destroy
    {
        /// <summary>
        /// Destroys the specified GameObject.
        /// </summary>
        /// <param name="model">The GameObject to destroy.</param>
        public static void GameObject(GameObject model)
        {
#if UNITY_EDITOR
            Object.DestroyImmediate(model);
#else
            Object.Destroy(model);
#endif
        }

        /// <summary>
        /// Destroys the specified MonoBehaviour.
        /// </summary>
        /// <param name="script">The MonoBehaviour to destroy.</param>
        public static void MonoBehaviour(MonoBehaviour script)
        {
#if UNITY_EDITOR
            Object.DestroyImmediate(script);
#else
            Object.Destroy(script);
#endif
        }
    }
}