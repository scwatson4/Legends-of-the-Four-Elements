using UnityEngine;

namespace AnythingWorld.Behaviour
{
    /// <summary>
    /// MonoBehaviour setting animator parameters of a model.
    /// </summary>
    public class AnythingAnimationProcessor : MonoBehaviour
    {
        private Animator animator;
        private GameObject mesh;

        /// <summary>
        /// Locally sets the Animator of the model to be a local variable. Throws an error if no Animator is found on either the model or its children.
        /// </summary>
        /// <param name="anythingObject">The model to get the Animator from</param>
        public void SetAnimator(GameObject anythingObject)
        {
            mesh = anythingObject;
            if (animator == null && !mesh.GetComponentInChildren<Animator>())
            {
                Debug.LogError($"No Animator is attached to the {name}!");
                Debug.Break();
            }
            animator = mesh.GetComponentInChildren<Animator>();
        }

        /// <summary>
        /// Gets the model's animator.
        /// </summary>
        /// <returns>The animator</returns>
        public Animator GetAnimator() => animator;

        /// <summary>
        /// Gets the model itself.
        /// </summary>
        /// <returns>The model</returns>
        public GameObject GetMesh() => mesh;

        /// <summary>
        /// Sets the speed parameter to switch between the idle, walk, and run animations.
        /// </summary>
        /// <param name="speed">The value to set the speed parameter to</param>
        public void SetSpeed(float speed) => animator.SetFloat("Speed", speed);

        /// <summary>
        /// Triggers the "Jump" trigger of the Animator.
        /// </summary>
        public void Jump()
        {
            animator.SetTrigger("Jump");
        }

        /// <summary>
        /// Sets the Animator to be falling.
        /// </summary>
        public void Fall() => animator.SetBool("Falling", true);

        /// <summary>
        /// Sets the Animator to not be falling.
        /// </summary>
        public void Land() => animator.SetBool("Falling", false);
    }
}