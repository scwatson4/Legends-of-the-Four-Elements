using UnityEngine;

namespace AnythingWorld.Behaviour.Tree
{
    /// <summary>
    /// Abstract base class for a key in a Blackboard, handling serialization and value copying.
    /// It serves as the foundation for creating typed keys that can store various types of data within
    /// a blackboard system.
    /// </summary>
    [System.Serializable]
    public abstract class BlackboardKey : ISerializationCallbackReceiver
    {
        public string name;
        public System.Type underlyingType;
        public string typeName;

        public BlackboardKey(System.Type underlyingType)
        {
            this.underlyingType = underlyingType;
            typeName = this.underlyingType.FullName;
        }

        /// <summary>
        /// Before serialization, update typeName with the assembly qualified name of the type.
        /// </summary>
        public void OnBeforeSerialize()
        {
            typeName = underlyingType.AssemblyQualifiedName;
        }

        /// <summary>
        /// After deserialization, retrieve the System.Type based on typeName.
        /// </summary>
        public void OnAfterDeserialize()
        {
            underlyingType = System.Type.GetType(typeName);
        }

        // Abstract method to copy the value from another BlackboardKey.
        public abstract void CopyValueFrom(BlackboardKey key);
        // Abstract method to compare two BlackboardKeys for equality.
        public abstract bool Equals(BlackboardKey key);

        /// <summary>
        /// Static method to create a new BlackboardKey instance of a specified type.
        /// </summary>
        public static BlackboardKey CreateKey(System.Type type)
        {
            return System.Activator.CreateInstance(type) as BlackboardKey;
        }

    }

    /// <summary>
    /// Generic subclass of BlackboardKey, storing a typed value. This class allows for the creation of keys
    /// that can hold specific types of values, enhancing type safety and ease of use within the blackboard system.
    /// </summary>
    [System.Serializable]
    public abstract class BlackboardKey<T> : BlackboardKey
    {
        public T value;

        public BlackboardKey() : base(typeof(T))
        {
        }

        /// <summary>
        /// Overrides ToString to provide a formatted string representation of the key-value pair.
        /// </summary>
        public override string ToString()
        {
            return $"{name} : {value}";
        }

        /// <summary>
        /// Copies the value from another BlackboardKey of the same type.
        /// </summary>
        public override void CopyValueFrom(BlackboardKey key)
        {
            if (key.underlyingType == underlyingType)
            {
                BlackboardKey<T> other = key as BlackboardKey<T>;
                this.value = other.value;
            }
        }

        /// <summary>
        /// Checks if another BlackboardKey of the same type is equal to this one.
        /// </summary>
        public override bool Equals(BlackboardKey key)
        {
            if (key.underlyingType == underlyingType)
            {
                BlackboardKey<T> other = key as BlackboardKey<T>;
                return this.value.Equals(other.value);
            }
            return false;
        }
    }
}
