using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace AnythingWorld.Behaviour.Tree
{
    /// <summary>
    /// Utility class providing methods to create fields in the Unity Inspector based on specific types.
    /// </summary>
    public static class PropertyDrawerUtils
    {
        /// <summary>
        /// Generates a VisualElement field based on the provided type and binding path.
        /// </summary>
        public static VisualElement GetFieldByType(Type propertyType, string bindingPath)
        {
            if (propertyType == typeof(Vector4))
            {
                return new Vector4Field
                {
                    bindingPath = bindingPath
                };
            }
            if (propertyType == typeof(Bounds))
            {
                return new BoundsField()
                {
                    bindingPath = bindingPath
                };
            }
            if (propertyType == typeof(BoundsInt))
            {
                return new BoundsIntField()
                {
                    bindingPath = bindingPath
                };
            }

            var defaultValueField = new PropertyField
            {
                bindingPath = bindingPath
            };

            return defaultValueField;
        }
    }
}
