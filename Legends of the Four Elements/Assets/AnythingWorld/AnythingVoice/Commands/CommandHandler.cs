using AnythingWorld.Utilities;
using System.Collections.Generic;
using AnythingWorld.Utilities.Data;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AnythingWorld.Voice
{
    /// <summary>
    /// Utility class for handling parsed speech commands.
    /// </summary>
    public static class CommandHandler
    {
        public static DefaultBehaviourPreset BehaviourPreset;

        /// <summary>
        /// Iterate through actions returned in parsed speech command and handle according to their command schema. 
        /// </summary>
        /// <param name="command"></param>
        public static void ParseCommand(ParsedSpeechCommand command, DefaultBehaviourPreset behaviourPreset = null)
        {
            if (behaviourPreset != null)
            {
                BehaviourPreset = behaviourPreset;
            }

            foreach (var action in command.actions)
            {
                ParseAction(action);
            }
        }

        /// <summary>
        /// Do action on a specific model only.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="model"></param>
        public static void ParseActionModel(Action action, Model model, DefaultBehaviourPreset behaviourPreset = null)
        {
            if (behaviourPreset != null)
            {
                BehaviourPreset = behaviourPreset;
            }
            switch (action.actionEnum)
            {
                case CommandSchema.ActionSchema.none:
                    break;
                case CommandSchema.ActionSchema.move_model:
                    MoveModel(model);
                    break;
                case CommandSchema.ActionSchema.add_model:
                    AddModel(model);
                    break;
                case CommandSchema.ActionSchema.remove_model:
                    RemoveModel(model);
                    break;
            }
        }
        
        public static void ParseAction(Action action)
        {
            switch (action.actionEnum)
            {
                case CommandSchema.ActionSchema.none:
                    break;
                case CommandSchema.ActionSchema.move_model:
                    MoveModel(action.models.ToArray());
                    break;
                case CommandSchema.ActionSchema.add_model:
                    AddModel(action.models.ToArray());
                    break;
                case CommandSchema.ActionSchema.remove_model:
                    RemoveModel(action.models.ToArray());
                    break;
            }
        }
        
        public static void OpenScene(string path)
        {
            if (Application.isPlaying)
            {
                OpenScenePlay(path);
            }
            else
            {
#if UNITY_EDITOR
                OpenSceneInEditor(path);
#endif
            }
        }
        
        private static void OpenScenePlay(string sceneName)
        {
            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        }
        
        private static void OpenSceneInEditor(string path)
        {
#if UNITY_EDITOR
            EditorSceneManager.OpenScene(path);
#endif
        }

        /// <summary>
        /// Find action models in scene and destroy if found.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="modelList"></param>
        private static void RemoveModel(params Model[] modelList)
        {
            foreach (var model in modelList)
            {
                for (int i = 0; i < model.number; i++)
                {
                    if (TryFindFirstMatchingModel(model.name, out var foundObject))
                    {
                        Object.DestroyImmediate(foundObject);
                    }
                    else
                    {
                        break;
                    }
                }

            }
        }

        /// <summary>
        /// For each model reposition according to the location parameter.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="modelList"></param>
        private static void MoveModel(params Model[] modelList) 
        {
            //For each references model in this action
            foreach (var model in modelList)
            {
                for (int i = 0; i < model.number; i++)
                {
                    if (model.location == null)
                    {
                        continue;
                    }
                    
                    // Find all matching objects
                    if (!TryFindingAllMatchingObjects(model.name, out var matchingObjects))
                    {
                        continue;
                    }
                    
                    var movableObjects = model.number;
                            
                    for (int j = 0; j < movableObjects; j++)
                    {
                        // If first object then position relative to the object
                        if (i == 0)
                        {
                            var position = GetNewRelativePosition(model);
                            matchingObjects[i].transform.position = position;
                        }
                        else
                        {
                            // Otherwise place near object
                            var position = matchingObjects[i - 1].transform.position + Vector3.left;
                            matchingObjects[i].transform.position = position;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// For each model create and position in scene (if location given)
        /// </summary>
        /// <param name="action"></param>
        /// <param name="modelList"></param>
        private static void AddModel(params Model[] modelList)
        {
            foreach (var model in modelList)
            {
                for (int i = 0; i < model.number; i++)
                {
                    var inputParams = new RequestParams()
                        .SetAddCollider(true)
                        .SetAddRigidbody(true)
                        .SetDefaultBehaviourPreset(BehaviourPreset)
                        .SetPlaceOnGround(true);
                        
                    if (model.location != null)
                    {
                        inputParams.SetPosition(GetNewRelativePosition(model));
                    }
                    else
                    {
                        inputParams.SetPlaceOnGrid(true);
                    }

                    AnythingMaker.Make(model.name, inputParams);
                }
            }
        }
        
        /// <summary>
        /// Find model specified in relative location and generate position.
        /// If no model found, create one and generate relative position.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private static Vector3 GetNewRelativePosition(Model model)
        {
            if (model.location.model_name == null)
            {
                return CalculateRelativePosition(null, model.location.relativeEnum);
            }

            if (TryFindFirstMatchingModel(model.location.model_name, out GameObject relativeToGameObject))
            {
                // if matching relative to game object found in scene, calculate relative positioning
                return CalculateRelativePosition(relativeToGameObject, model.location.relativeEnum);
            }

            var requestParams = new RequestParams()
                .SetDefaultBehaviour(true)
                .SetAddCollider(true)
                .SetAddRigidbody(true)
                .SetPlaceOnGround(true);

            // Else make object and get relative
            var newRelativeObject = AnythingMaker.Make(model.location.model_name, requestParams);
            return CalculateRelativePosition(newRelativeObject, model.location.relativeEnum);
        }

        private static Vector3 GetNewRelativePosition(Model model, GameObject relativeToModel)
        {
            return CalculateRelativePosition(relativeToModel, model.location.relativeEnum);
        }
        
        /// <summary>
        /// Calculate position relative to a game object given a location keyword.
        /// </summary>
        /// <param name="relativeToGameObject"></param>
        /// <param name="location"></param>
        /// <returns></returns>
        private static Vector3 CalculateRelativePosition(GameObject relativeToGameObject, CommandSchema.RelativeLocationSchema location)
        {
            //Find relevant camera (editor viewport in editor, main camera in runtime)
            Vector3 outputLocation = Vector3.zero;
            Camera camera = null;
#if UNITY_EDITOR
            if (UnityEditor.SceneView.lastActiveSceneView.camera != null)
            {
                camera = UnityEditor.SceneView.lastActiveSceneView.camera;
            }
            else if (Camera.main != null)
            {
                camera = null;
            }
#else
            camera = Camera.main;
#endif
            Vector3 relativeObjectLocation = Vector3.zero;

            if (relativeToGameObject != null)
            {
                relativeObjectLocation = relativeToGameObject.transform.position;
            }

            switch (location)
            {
                case CommandSchema.RelativeLocationSchema.none:
                    break;
                case CommandSchema.RelativeLocationSchema.here:
                    if (camera != null)
                    {
                        return camera.transform.position + camera.transform.forward;
                    }
                    else
                    {
                        Debug.LogWarning("No camera found for relative positioning, positioning at origin.");
                        return Vector3.zero;
                    }

                case CommandSchema.RelativeLocationSchema.near: //Return random location on unit circle of game object
                    var unitPosition = UnityEngine.Random.insideUnitCircle.normalized;
                    var relativeObject = relativeObjectLocation;
                    outputLocation = new Vector3(relativeObject.x + unitPosition.x, relativeObject.y, relativeObject.z + unitPosition.y);
                    break;
                case CommandSchema.RelativeLocationSchema.far:
                    unitPosition = UnityEngine.Random.insideUnitCircle.normalized;
                    var offset = new Vector3(unitPosition.x, 0, unitPosition.y);
                    return relativeObjectLocation + (offset * 10);
                case CommandSchema.RelativeLocationSchema.front:
                    return relativeObjectLocation - new Vector3(camera.transform.forward.x, 0, camera.transform.forward.z);
                case CommandSchema.RelativeLocationSchema.behind:
                    return relativeObjectLocation + new Vector3(camera.transform.forward.x, 0, camera.transform.forward.z);
                case CommandSchema.RelativeLocationSchema.above:
                    return relativeObjectLocation + Vector3.up;
                case CommandSchema.RelativeLocationSchema.below:
                    return relativeObjectLocation + Vector3.down;
                case CommandSchema.RelativeLocationSchema.left:
                    return relativeObjectLocation - new Vector3(camera.transform.right.x, 0, camera.transform.right.z);
                case CommandSchema.RelativeLocationSchema.right:
                    return relativeObjectLocation + new Vector3(camera.transform.right.x, 0, camera.transform.right.z);
            }
            return outputLocation;
        }
        
        /// <summary>
        /// Try to find object in scene that match name or entity and return found GameObject.
        /// </summary>
        /// <param name="_searchKeyword"></param>
        /// <param name="matchingObject"></param>
        /// <returns></returns>
        private static bool TryFindFirstMatchingModel(string _searchKeyword, out GameObject matchingObject)
        {
            _searchKeyword = _searchKeyword.ToLower();
            if (_searchKeyword == null || _searchKeyword == "")
            {
                matchingObject = null;
                return false;
            }
            var activeModels = GameObject.FindObjectsByType<ModelDataInspector>(FindObjectsSortMode.None);
            //For each model in scene if entity contains or matches query return gameobject 
            foreach (var model in activeModels)
            {
                if (model.entity.ToLower() == null) continue;
                //If name match
                if (model.name.ToLower() == _searchKeyword)
                {
                    matchingObject = model.gameObject;
                    return true;
                }

            }
            //exact entity match
            foreach (var model in activeModels)
            {
                if (model.entity.ToLower() == _searchKeyword)
                {
                    matchingObject = model.gameObject;
                    return true;
                }

            }
            //name contains match
            foreach (var model in activeModels)
            {
                if (model?.name.ToLower() != null)
                {
                    if (model.name.ToLower().Contains(_searchKeyword))
                    {
                        matchingObject = model.gameObject;
                        return true;
                    }
                }
            }

            //tag contains match
            foreach (var model in activeModels)
            {
                foreach (var tag in model?.tags)
                {
                    if (tag.ToLower() == _searchKeyword)
                    {
                        matchingObject = model.gameObject;
                        return true;
                    }
                }
            }

            Debug.Log("couldn't find matching object");
            matchingObject = null;
            return false;
        }

        private static bool TryFindingAllMatchingObjects(string _searchKeyword, out GameObject[] matchingObjects)
        {
            _searchKeyword = _searchKeyword.ToLower();
            var matchingGameObjects = new List<GameObject>();
            if (string.IsNullOrEmpty(_searchKeyword))
            {
                matchingObjects = matchingGameObjects.ToArray();
                return false;
            }
            var activeModels = GameObject.FindObjectsByType<ModelDataInspector>(FindObjectsSortMode.None);
            // For each model in scene if entity contains or matches query return gameobject 
            foreach (var model in activeModels)
            {
                if (model.entity == null) continue;
                // If name matches
                if (model.name.ToLower() == _searchKeyword)
                {
                    matchingGameObjects.Add(model.gameObject);
                    continue;
                }
                if (model.entity == _searchKeyword)
                {
                    matchingGameObjects.Add(model.gameObject);
                    continue;
                }
                if (model?.name.ToLower() != null)
                {
                    if (model.name.Contains(_searchKeyword))
                    {
                        matchingGameObjects.Add(model.gameObject);
                        continue;
                    }
                }
                foreach (var tag in model?.tags)
                {
                    if (tag.ToLower() == _searchKeyword)
                    {
                        matchingGameObjects.Add(model.gameObject);
                        continue;
                    }
                }
            }

            matchingObjects = matchingGameObjects.ToArray();
            return matchingGameObjects.Count > 0;
        }
    }
}
