using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AnythingWorld.Behaviour.Tree
{
    /// <summary>
    /// Manages the runtime instance of a BehaviourTree, handling its initialization, update cycle,
    /// and blackboard overrides.
    /// </summary>
    [AddComponentMenu("BehaviourTreeEditor/BehaviourTreeRunner")]
    public class BehaviourTreeInstanceRunner : MonoBehaviour
    {
        public BehaviourTree behaviourTree;
        [Tooltip("Run behaviour tree validation at startup (Can be disabled for release)")]
        public bool validate = true;
        public List<BlackboardKeyValuePair> blackboardOverrides = new List<BlackboardKeyValuePair>();
        private bool _isInitialized;

        /// <summary>
        /// Clones the assigned BehaviourTree asset if it is not a clone already.
        /// </summary>
        private void OnValidate()
        {
            if (!behaviourTree || !isActiveAndEnabled)
            {
                return;
            }

            var isCloned = behaviourTree.name.Contains("(Clone)");
            
            if (isCloned && _isInitialized)
            {
                return;
            }

            var btName = behaviourTree.name;
            if (ValidateTree())
            {
                behaviourTree = behaviourTree.Clone();
                
                if (Application.isPlaying)
                {
                    var context = CreateBehaviourTreeContext();
                    behaviourTree.Bind(context);
                }
                else
                {
                    blackboardOverrides = blackboardOverrides.
                        Where(x => behaviourTree.blackboard.Find(x.key.name) != null).ToList();
                }
            }

            if (isCloned)
            {
                behaviourTree.name = btName;
            }

            _isInitialized = true;
        }
        
        /// <summary>
        /// Creates BT context from game object and applies overrides when entering play mode.
        /// </summary>
        private void OnEnable()
        {
            if (!behaviourTree)
            {
                return;
            }

            var context = CreateBehaviourTreeContext();
            behaviourTree.Bind(context);
            ApplyKeyOverrides();
        }
        
        /// <summary>
        /// Regularly updates the behaviour tree during gameplay.
        /// </summary>
        private void Update()
        {
            if (behaviourTree)
            {
                behaviourTree.Update();
            }
        }
        
        /// <summary>
        /// Initializes the BehaviourTreeInstanceRunner with the designated BehaviourTree asset.
        /// </summary>
        public void InitializeTree()
        {
            if (ValidateTree())
            {
                behaviourTree = behaviourTree.Clone();
                
                if (Application.isPlaying)
                {
                    var context = CreateBehaviourTreeContext();
                    behaviourTree.Bind(context);
                }
            }   

            _isInitialized = true;
        }
        
        /// <summary>
        /// Finds a blackboard key of a specific type by name.
        /// </summary>
        public BlackboardKey<T> FindBlackboardKey<T>(string keyName)
        {
            if (behaviourTree)
            {
                return behaviourTree.blackboard.Find<T>(keyName);
            }
            return null;
        }

        
        /// <summary>
        /// Sets a value in the blackboard for a given key.
        /// </summary>
        public void SetBlackboardValue<T>(string keyName, T value)
        {
            if (behaviourTree)
            {
                behaviourTree.blackboard.SetValue(keyName, value);
            }
        }

        /// <summary>
        /// Retrieves a value from the blackboard by key name.
        /// </summary>
        public T GetBlackboardValue<T>(string keyName)
        {
            if (behaviourTree)
            {
                return behaviourTree.blackboard.GetValue<T>(keyName);
            }
            return default(T);
        }
        
        /// <summary>
        /// Creates a context for the behaviour tree based on the current GameObject.
        /// </summary>
        /// <returns></returns>
        private Context CreateBehaviourTreeContext()
        {
            return Context.CreateFromGameObject(gameObject);
        }

        /// <summary>
        /// Validates the assigned BehaviourTree for correctness and non-recursiveness.
        /// </summary>
        /// <returns></returns>
        private bool ValidateTree()
        {
            if (!behaviourTree)
            {
                Debug.LogWarning($"No BehaviourTree assigned to {name}, assign a behaviour tree in the inspector");
                return false;
            }

            bool isValid = true;
            if (validate)
            {
                string cyclePath;
                isValid = !IsRecursive(behaviourTree, out cyclePath);

                if (!isValid)
                {
                    Debug.LogError($"Failed to create recursive behaviour tree. Found cycle at: {cyclePath}");
                }
            }

            return isValid;
        }

        /// <summary>
        /// Checks if the behaviour tree or any of its sub-trees are recursive.
        /// </summary>
        private bool IsRecursive(BehaviourTree tree, out string cycle)
        { 
            List<string> treeStack = new List<string>();
            HashSet<BehaviourTree> referencedTrees = new HashSet<BehaviourTree>();

            bool cycleFound = false;
            string cyclePath = "";

            Action<Node> traverse = null;
            traverse = node =>
            {
                if (!cycleFound)
                {
                    if (node is SubTree subtree && subtree.treeAsset != null)
                    {
                        treeStack.Add(subtree.treeAsset.name);
                        if (referencedTrees.Contains(subtree.treeAsset))
                        {
                            int index = 0;
                            foreach (var tree in treeStack)
                            {
                                index++;
                                if (index == treeStack.Count)
                                {
                                    cyclePath += $"{tree}";
                                }
                                else
                                {
                                    cyclePath += $"{tree} -> ";
                                }
                            }

                            cycleFound = true;
                        }
                        else
                        {
                            referencedTrees.Add(subtree.treeAsset);
                            BehaviourTree.Traverse(subtree.treeAsset.rootNode, traverse);
                            referencedTrees.Remove(subtree.treeAsset);
                        }
                        treeStack.RemoveAt(treeStack.Count - 1);
                    }
                }
            };
            treeStack.Add(tree.name);

            referencedTrees.Add(tree);
            BehaviourTree.Traverse(tree.rootNode, traverse);
            referencedTrees.Remove(tree);

            treeStack.RemoveAt(treeStack.Count - 1);
            cycle = cyclePath;
            return cycleFound;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Draws custom gizmos in the Unity editor if the BehaviourTreeInstanceRunner is selected.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (!behaviourTree)
            {
                return;
            }

            BehaviourTree.Traverse(behaviourTree.rootNode, (n) =>
            {
                if (n.drawGizmos)
                {
                    n.OnDrawGizmosSelectedTree();
                }
            });
        }
#endif
        /// <summary>
        /// Applies blackboard key overrides to the running behaviour tree.
        /// </summary>
        private void ApplyKeyOverrides()
        {
            foreach (var pair in blackboardOverrides)
            {
                var targetKey = behaviourTree.blackboard.Find(pair.key.name);
                var sourceKey = pair.value;
                if (targetKey != null && sourceKey != null)
                {
                    targetKey.CopyValueFrom(sourceKey);
                }
            }
        }
    }
}
