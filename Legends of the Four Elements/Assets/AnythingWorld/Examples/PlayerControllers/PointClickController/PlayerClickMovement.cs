using System.Linq;

using UnityEngine;
using UnityEngine.AI;
namespace AnythingWorld.Behaviour
{
    /// <summary>
    /// MonoBehaviour handling the point-and-click based controller.
    /// </summary>
    public class PlayerClickMovement : MonoBehaviour
    {
        #region Public Variables
        public Camera NavMeshCamera;
        #endregion Public Variables

        #region Private Variables
        private NavMeshAgent _navAgent;
        private RaycastHit _lastPointHit;
        private AnythingAnimationProcessor _animationProcessor;

        private Vector3 _cameraOffset;
        #endregion Private Variables

        /// <summary>
        /// Sets up the player controller's different variables.
        /// </summary>
        void Awake()
        {
            if (_navAgent == null && !GetComponent<NavMeshAgent>())
            {
                Debug.LogError($"{name} does not have a NavMeshAgent component attached to it!");
                Debug.Break();
            }

            if (NavMeshCamera == null)
            {
                Debug.LogError($"No Camera is added to {name}!");
                Debug.Break();
            }

            _cameraOffset = NavMeshCamera.transform.position;
            _navAgent = GetComponent<NavMeshAgent>();

            if (GetComponent<AnythingAnimationProcessor>()) _animationProcessor = GetComponent<AnythingAnimationProcessor>();
        }

        /// <summary>
        /// Polls the player for any updated position to move towards.
        /// </summary>
        void Update()
        {
            _lastPointHit = MoveToPoint();
            NavMeshCamera.transform.position = Vector3.MoveTowards(NavMeshCamera.transform.position, transform.position + _cameraOffset, 1f);

            if (_animationProcessor?.GetAnimator()) _animationProcessor.SetSpeed(Mathf.Clamp01(_navAgent.velocity.magnitude));
        }

        /// <summary>
        /// Get the point on a map for the NavMeshAgent to move towards.
        /// </summary>
        /// <returns>The point of the map the player should move towards</returns>
        RaycastHit MoveToPoint()
        {
            if (Input.GetMouseButtonDown(0))
            {
                RaycastHit hit;
                Ray ray = NavMeshCamera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out hit))
                {
                    if (hit.collider != null && _navAgent.enabled)
                    {
                        _navAgent.destination = hit.point;
                        return hit;
                    }
                }
            }
            return _lastPointHit;
        }

        /// <summary>
        /// Fits the NavMesh agent's collider to the mesh.
        /// </summary>
        /// <param name="meshObject">The model to fit the collider around</param>
        public void FitNavMeshAgentToMesh(GameObject meshObject)
        {
            if (_navAgent != null)
            {
                if (meshObject.GetComponentInChildren<SkinnedMeshRenderer>())
                {
                    SkinnedMeshRenderer skinnedRenderer = meshObject.GetComponentInChildren<SkinnedMeshRenderer>();
                    meshObject.transform.localPosition = new Vector3(0f, skinnedRenderer.sharedMesh.bounds.size.y * meshObject.transform.localScale.y / 2f, 0f);

                    skinnedRenderer.sharedMesh.RecalculateBounds();

                    _navAgent.height = (skinnedRenderer.sharedMesh.bounds.size.y * meshObject.transform.localScale.y);
                    _navAgent.radius = Mathf.Max(skinnedRenderer.sharedMesh.bounds.extents.x * meshObject.transform.localScale.x, skinnedRenderer.sharedMesh.bounds.extents.z * meshObject.transform.localScale.z);

                    _navAgent.enabled = true;
                    return;
                }

                if (meshObject.GetComponentsInChildren<MeshFilter>().Any())
                {
                    MeshFilter[] meshFilters = meshObject.GetComponentsInChildren<MeshFilter>();

                    var totalBounds = new Bounds(Vector3.zero, Vector3.zero);
                    var meshCenter = Vector3.zero;

                    foreach (var mFilter in meshFilters)
                    {
                        var mMesh = mFilter.sharedMesh;
                        meshCenter += mMesh.bounds.center;
                    }
                    meshCenter /= meshFilters.Length;
                    totalBounds.center = meshCenter;

                    foreach (var mFilter in meshFilters)
                    {
                        var mMesh = mFilter.sharedMesh;
                        if (totalBounds.size == Vector3.zero)
                            totalBounds = mMesh.bounds;
                        else
                            totalBounds.Encapsulate(mMesh.bounds);
                    }

                    _navAgent.height = (totalBounds.size.y * meshObject.transform.localScale.y);
                    _navAgent.radius = Mathf.Max(totalBounds.extents.x * meshObject.transform.localScale.x, totalBounds.extents.z * meshObject.transform.localScale.z);

                    _navAgent.enabled = true;
                    return;
                }
            }
        }
    }
}