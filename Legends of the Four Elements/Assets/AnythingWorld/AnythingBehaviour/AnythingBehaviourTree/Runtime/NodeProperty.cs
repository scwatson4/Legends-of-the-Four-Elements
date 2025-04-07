using UnityEngine;

namespace AnythingWorld.Behaviour.Tree
{
    /// <summary>
    /// Represents a reference to a BlackboardKey within a node, without specifying the data type of the value
    /// </summary>
    [System.Serializable]
    public class NodeProperty
    {
        [SerializeReference]
        public BlackboardKey reference; 
    }

    /// <summary>
    /// Extends NodeProperty to allow for typed interactions with the behavior tree's blackboard. Holds a default value
    /// of a specific type when the blackboard key is not set or available.
    /// </summary>
    [System.Serializable]
    public class NodeProperty<T> : NodeProperty
    {
        public T defaultValue;
        private BlackboardKey<T> _typedKey;

        public NodeProperty(){}

        public NodeProperty(T defaultValue)
        {
            this.defaultValue = defaultValue;
        }

        public static implicit operator T(NodeProperty<T> instance) => instance.Value;
        
        public T Value
        {
            set
            {
                if (typedKey != null)
                {
                    typedKey.value = value;
                }
                else
                {
                    defaultValue = value;
                }
            }
            get
            {
                if (typedKey != null)
                {
                    return typedKey.value;
                }
                else
                {
                    return defaultValue;
                }
            }
        }
        
        private BlackboardKey<T> typedKey
        {
            get
            {
                if (_typedKey == null && reference != null)
                {
                    _typedKey = reference as BlackboardKey<T>;
                }
                return _typedKey;
            }
        }

        // Method to create a copy of the NodeProperty.
        public NodeProperty<T> CreateCopy()
        {
            var copy = new NodeProperty<T>
            {
                defaultValue = defaultValue,
                reference = reference
            };

            return copy;
        }
    }
}
