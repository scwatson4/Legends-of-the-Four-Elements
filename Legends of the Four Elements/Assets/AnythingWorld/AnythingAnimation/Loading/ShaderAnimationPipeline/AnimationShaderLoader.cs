using AnythingWorld.Utilities.Data;
using UnityEngine;

namespace AnythingWorld.Animation
{
    /// <summary>
    /// Provides methods to load and switch shaders for model animations.
    /// </summary>
    public static class AnimationShaderLoader
    {
        /// <summary>
        /// Loads the appropriate shader for the model based on its behavior.
        /// </summary>
        /// <param name="data">The model data containing behavior information.</param>
        public static void Load(ModelData data)
        {
            data.Debug($"Finding shader for behaviour {data.json.behaviour}");
            var shaderAnimationType = ParseAnimationType(data.json.behaviour);
            data.Debug(shaderAnimationType.ToString());
            SwitchShader(data.model, shaderAnimationType);
        }

        /// <summary>
        /// Parses the behavior string to determine the corresponding shader.
        /// </summary>
        /// <param name="behaviour">The behavior string from the model data.</param>
        /// <returns>The corresponding shader for the behavior.</returns>
        private static Shader ParseAnimationType(string behaviour)
        {
            switch (behaviour)
            {
                case "swim":
                case "swim3":
                case "swim2":
                    return Shader.Find("Anything World/Animation/Fish Vertical Animation");
                case "wriggle":
                    return Shader.Find("Anything World/Animation/Wriggle Animation");
                case "crawl":
                    return Shader.Find("Anything World/Animation/Crawler Animation");
                case "slither":
                    return Shader.Find("Anything World/Animation/Slither Animation");
                case "slithervertical":
                    return Shader.Find("Anything World/Animation/Slither Vertical Animation");
                default:
                    return null;
            }
        }

        /// <summary>
        /// Switches the shader of the model's materials to the specified shader.
        /// </summary>
        /// <param name="model">The model GameObject whose shader will be switched.</param>
        /// <param name="inputShader">The shader to apply to the model's materials.</param>
        private static void SwitchShader(GameObject model, Shader inputShader)
        {
            var meshRenderer = model.GetComponentInChildren<MeshRenderer>();
            if (meshRenderer != null)
            {
                foreach (var mat in meshRenderer.sharedMaterials)
                {
                    mat.shader = inputShader;
                }
            }
        }
        
        /// <summary>
        /// Selects a method to set the shader property depending on shader variable type and sets it.
        /// </summary>
        /// <typeparam name="T">The type of the editable property.</typeparam>
        /// <param name="model">The model GameObject whose shader will be switched.</param>
        /// <param name="inputShader">The shader to apply to the model's materials.</param>
        /// <param name="editableProperty">The editable property to set on the shader.</param>
        private static void SwitchShader<T>(GameObject model, Shader inputShader, 
            ShaderEditableProperty<T> editableProperty)
        {
            var meshRenderer = model.GetComponentInChildren<MeshRenderer>();
            if (meshRenderer == null) return;

            foreach (var material in meshRenderer.sharedMaterials)
            {
                material.shader = inputShader;
                switch (editableProperty.Variable)
                {
                    case float f:
                        material.SetFloat(editableProperty.Property, f);
                        break;
                    case int i:
                        material.SetInt(editableProperty.Property, i);
                        break;
                    case Color c:
                        material.SetColor(editableProperty.Property, c);
                        break;
                    default:
                        Debug.LogWarning($"Shader Property Editing of type {typeof(T).Name} is not supported");
                        break;
                }
            }
        }
        
        private struct ShaderEditableProperty<T>
        {
            public string Property;
            public T Variable;
        }
    }
}