using UnityEngine;

namespace AnythingWorld.Behaviour.Tree
{
    /// <summary>
    /// The context is a shared object every node has access to.
    /// Commonly used components and subsystems should be stored here.
    /// It will be somewhat specific to your game exactly what to add here.
    /// Feel free to extend this class. 
    /// </summary>
    public class Context
    {
        public GameObject GameObject;
        public Transform Transform;
        public Rigidbody Rb;
        
        // Add other game specific systems here.

        public static Context CreateFromGameObject(GameObject gameObject) 
        {
            // Fetch all commonly used components.
            Context context = new Context();
            context.GameObject = gameObject;
            context.Transform = gameObject.transform;
            context.Rb = gameObject.GetComponent<Rigidbody>();
            
            return context;
        }
    }
}
