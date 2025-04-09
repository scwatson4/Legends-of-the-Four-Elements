using UnityEngine;

namespace AnythingWorld.Behaviour.Tree
{
    /// <summary>
    /// This is a special type to represent a key binding, with a value.
    /// The BlackboardKeyValuePairPropertyDrawer takes care of updating the rendered value field
    /// when the key type changes.
    /// </summary>
    [System.Serializable]
    public class BlackboardKeyValuePair
    {
        [SerializeReference]
        public BlackboardKey key;

        [SerializeReference]
        public BlackboardKey value;

        /// <summary>
        /// Writes the value from one key to another.
        /// </summary>
        public void WriteValue()
        {
            if (key != null && value != null)
            {
                key.CopyValueFrom(value);
            }
        }
    }
}
