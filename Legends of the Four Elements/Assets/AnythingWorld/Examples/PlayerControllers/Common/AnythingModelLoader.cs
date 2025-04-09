using AnythingWorld;
using AnythingWorld.Behaviour;
using AnythingWorld.Utilities.Data;
using UnityEngine;

namespace AnythingWorld.Editor
{
    /// <summary>
    /// MonoBehaviour loading a model by ID from the Anything World database.
    /// </summary>
    public class AnythingModelLoader : MonoBehaviour
    {
        public string modelID;

        /// <summary>
        /// Starts loading in the mesh for the player controller.
        /// </summary>
        void Start()
        {
            if (!string.IsNullOrEmpty(modelID))
            {
                var requestParams = new RequestParams()
                    .SetAddRigidbody(false)
                    .SetAddBehaviour(false)
                    .SetAddCollider(false)
                    .SetParent(transform)
                    .SetOnSuccessAction(x =>
                    {
                        x.linkedObject.name = "Mesh";
                        if (GetComponent<PlayerCoreMovement>())
                        {
                            GetComponent<PlayerCoreMovement>().FitCharacterControllerToMesh(x.linkedObject);
                        }
                        if (GetComponent<PlayerClickMovement>())
                        {
                            GetComponent<PlayerClickMovement>().FitNavMeshAgentToMesh(x.linkedObject);
                        }

                        if (GetComponent<AnythingAnimationProcessor>())
                        {
                            GetComponent<AnythingAnimationProcessor>().SetAnimator(x.linkedObject);
                        }
                    });

                AnythingMaker.MakeById(modelID, requestParams);
            }
            else
            {
                Debug.LogWarning($"No \"Model ID\" given, default mesh set for {name}");
                GameObject mesh = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                Destroy(mesh.GetComponent<Collider>());
                mesh.name = "Mesh";
                mesh.transform.parent = transform;
                mesh.transform.localPosition = new Vector3(0f, mesh.GetComponent<MeshRenderer>().bounds.extents.y, 0f);

                if (GetComponent<PlayerCoreMovement>())
                {
                    GetComponent<PlayerCoreMovement>().FitCharacterControllerToMesh(mesh);
                }

                if (GetComponent<PlayerClickMovement>())
                {
                    GetComponent<PlayerClickMovement>().FitNavMeshAgentToMesh(mesh);
                }
            }
        }
    }

}