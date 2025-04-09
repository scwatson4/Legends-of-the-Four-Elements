using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;

namespace AnythingWorld.Utilities
{
    public enum GridMode { FIT, CLIP }

    public static class GridArea
    {
        public static bool initialized = false;

        public static Vector3 origin = Vector3.zero;
        public static Vector2 areaSize = new Vector2(1f, 1f);
        public static Vector3 areaForward = new Vector3(0, 0, 1);
        public static Vector3 areaRight = new Vector3(1, 0, 0);
        public static Vector2 objectsDistance = new Vector2(1f, 1f);
        public static int objectsPerRow = 10;
        public static GridMode gridMode = GridMode.FIT;
        public static bool canGrow = false;
        public static bool showPositions = false;
        public static bool randomOffset = false;
        public static bool placeOnGround = false;
        // grid settings backup
        public static bool hasBackup = false;
        public static Vector3 backupGridOrigin = Vector3.zero;
        public static float backupCellWidth = 1f;
        public static int backupCellCount = 10;

        private static List<GameObject> objects = new List<GameObject>();
        private static List<Vector3> positions = new List<Vector3>();
        private static int seed = 12345;

        /// <summary>
        /// Clear all data
        /// </summary>
        public static void Clear()
        {
            initialized = false;
            hasBackup = false;

            origin = Vector3.zero;
            areaSize = new Vector2(1f, 1f);
            areaForward = new Vector3(0, 0, 1);
            areaRight = new Vector3(1, 0, 0);
            objectsDistance = new Vector2(1f, 1f);
            objectsPerRow = 10;
            gridMode = GridMode.FIT;
            canGrow = false;
            showPositions = false;
            randomOffset = false;
            placeOnGround = false;

            // grid settings backup
            backupGridOrigin = Vector3.zero;
            backupCellWidth = 1f;
            backupCellCount = 10;

            // clean any previous data in list
            positions.Clear();
            objects.Clear();
        }

        /// <summary>
        /// Init the area with the data provided, instead of using a gameObject
        /// </summary>
        public static void Init(Vector3 _origin, Vector3 _size, Vector3 _right, Vector3 _forward)
        {
            origin = _origin;
            areaSize = _size;
            areaForward = _forward;
            areaRight = _right;

            initialized = true;
        }

        /// <summary>
        /// Init the area using the bounds of a GameObject
        /// </summary>
        public static void GetSizeFromObject(GameObject obj)
        {
            if (!obj) return;

            Bounds bounds = CalculateChildrenBounds(obj);
            origin = bounds.center - bounds.extents;
            areaSize.x = bounds.size.x;
            areaSize.y = bounds.size.z;

            // get the rotation
            areaForward = obj.transform.forward;
            areaRight = obj.transform.right;

            // get the rotated origin point
            Vector3 dir = -bounds.extents;
            dir = obj.transform.TransformDirection(dir);
            origin = bounds.center + dir;

            // mark as initialized with an object
            initialized = true;
        }

        /// <summary>
        /// Enable the fit mode, providing the number of models per row
        /// </summary>
        public static void SetFitMode(int _objectsPerRow)
        {
            gridMode = GridMode.FIT;
            objectsPerRow = _objectsPerRow;

            if (objectsPerRow <= 0)
                objectsPerRow = 1;

            // calculate parameters of distance
            UpdateCalculations();
        }

        /// <summary>
        /// Enable the clip mode, providing the distance per model
        /// </summary>
        public static void SetClipMode(Vector2 _objectsDistance)
        {
            gridMode = GridMode.CLIP;
            objectsDistance = _objectsDistance;

            // calculate parameters of distance
            UpdateCalculations();
        }

        /// <summary>
        /// Calculate all parameters in function of the enabled mode
        /// </summary>
        public static void UpdateCalculations()
        {
            switch (gridMode)
            {
                case GridMode.FIT:
                    // calculate the distance between objects
                    float x = areaSize.x / (objectsPerRow - 1);
                    float y = areaSize.y;
                    int rows = (int) Math.Ceiling((double) objects.Count / objectsPerRow);
                    if (rows > 1)
                        y = (float) (areaSize.y / (rows - 1));
                    objectsDistance.x = x;
                    objectsDistance.y = y;
                    break;

                case GridMode.CLIP:
                    // calculate how many objects per row fit the area
                    objectsPerRow = (int) (Math.Round(areaSize.x, 3) / Math.Round(objectsDistance.x, 3) + 1);
                    break;
            }
        }

        /// <summary>
        /// Enable/Disable the random offset mode to apply to each model
        /// </summary>
        public static void SetRandomMode(bool enable)
        {
            randomOffset = enable;
            seed = (int) Time.time * 1000;
            UnityEngine.Random.InitState(seed);
        }

        /// <summary>
        /// Enable/Disable the visualization as red lines of the future positions
        /// </summary>
        public static void SetShowPositionsMode(bool enable)
        {
            showPositions = enable;
        }

        /// <summary>
        /// Enable/Disable the mode of placing models on the ground
        /// </summary>
        public static void SetPlaceOnGroundMode(bool enable)
        {
            placeOnGround = enable;
        }

        /// <summary>
        /// Enable/Disable the grow mode when adding new models in clip mode
        /// </summary>
        public static void SetGrowMode(bool enable)
        {
            canGrow = enable;
        }

        /// <summary>
        /// Add a new model into the grid. If none are passed as parameter, a cube is used
        /// </summary>
        public static Vector3 AddModel(GameObject obj = null)
        {
            if (!obj)
            {
                // random color and scale
                UnityEngine.Random.InitState((int) Time.time * 1000);
                obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obj.GetComponent<Renderer>().material.color = new Color(UnityEngine.Random.Range(0f,1f), UnityEngine.Random.Range(0f,1f),  UnityEngine.Random.Range(0f,1f), 1.0f);
                obj.transform.localScale = new Vector3(UnityEngine.Random.Range(1f,3f), UnityEngine.Random.Range(1f,3f), UnityEngine.Random.Range(1f,3f));
            }

            objects.Add(obj);
            RearrangeObjects();
            return obj.transform.position;
        }

        /// <summary>
        /// Remove the last model placed into the grid area
        /// </summary>
        public static void RemoveModel()
        {
            if (objects.Count == 0) return;

            UnityEngine.Object.DestroyImmediate(objects[objects.Count - 1]);
            objects.RemoveAt(objects.Count - 1);

            RearrangeObjects();
        }

        /// <summary>
        /// Rearrange all models added. This is needed for the fit mode to arrange rows, or also then changing the PlaceOnGround and IgnoreCollision options
        /// </summary>
        public static void RearrangeObjects()
        {
            // same seed for repeat the same random values
            UnityEngine.Random.InitState(seed);

            // check for invalid objects
            RemoveDestroyedObjects();

            switch (gridMode)
            {
                default:
                case GridMode.FIT:
                    RearrangeObjectsFitting();
                    break;

                case GridMode.CLIP:
                    RearrangeObjectsClipping();
                    break;
            }

            UpdateCalculations();
        }

        /// <summary>
        /// Rearrange models in fit mode
        /// </summary>
        private static void RearrangeObjectsFitting()
        {
            // calculate the distance between objects
            float x = areaSize.x;
            float y = areaSize.y;
            int rows = (int) Math.Ceiling((double) objects.Count / objectsPerRow);
            if (objectsPerRow > 1)
                x = areaSize.x / (objectsPerRow - 1);
            if (rows > 1)
                y = areaSize.y / (rows - 1);

            // update values
            objectsDistance.x = x;
            objectsDistance.y = y;

            // rearrange all objects
            int i = 0;
            int j = 0;
            Vector3 pos;
            foreach (GameObject obj in objects)
            {
                // calc position
                pos = origin;
                pos += areaRight * (i * objectsDistance.x);
                pos += areaForward * (j * objectsDistance.y);
                // apply random if enabled
                ApplyRandomOffset(i, j, ref pos);
                // apply boundary offset to collide with the area mesh
                ApplyBoundaryOffset(i, j, ref pos);
                // place object
                obj.transform.position = pos;
                // reposition the model
                CheckToPlaceOnGround(obj);
                // new row
                ++i;
                if (i >= objectsPerRow)
                {
                    i = 0;
                    ++j;
                }
            }

            // preview future positions
            CalculateFuturePositions(i, j);
        }

        /// <summary>
        /// Rearrange models in clip mode
        /// </summary>
        private static void RearrangeObjectsClipping()
        {
            // rearrange all objects
            int i = 0;
            int j = 0;
            Vector3 pos;
            foreach (GameObject obj in objects)
            {
                // calc position
                pos = origin;
                pos += areaRight * (i * objectsDistance.x);
                pos += areaForward * (j * objectsDistance.y);
                // apply random if enabled
                ApplyRandomOffset(i, j, ref pos);
                // apply boundary offset to collide with the area mesh
                ApplyBoundaryOffset(i, j, ref pos);
                // place object
                obj.transform.position = pos;
                // reposition the model
                CheckToPlaceOnGround(obj);
                // new row
                ++i;
                if (i >= objectsPerRow)
                {
                    i = 0;
                    ++j;
                }
            }

            // preview future positions
            CalculateFuturePositions(i, j);
        }

        /// <summary>
        /// Check if the model needs to be place on the ground
        /// </summary>
        private static void CheckToPlaceOnGround(GameObject obj)
        {
            if (!placeOnGround) return;

            // hide the object to the raycast
            obj.SetActive(false);

            // put the object in the ground, through the ray a bit above the position
            RaycastHit hit;
            if (Physics.Raycast(obj.transform.position + new Vector3(0f, 0.5f, 0f), Vector3.down, out hit, 100f))
            {
                Bounds bb = CalculateChildrenBounds(obj);
                obj.transform.position = new Vector3(obj.transform.position.x, hit.point.y - (bb.center.y - obj.transform.position.y) + bb.extents.y, obj.transform.position.z);
            }

            // reenable object again
            obj.SetActive(true);
        }

        /// <summary>
        /// Return the bounds of the GameObject and all its children
        /// </summary>
        private static Bounds CalculateChildrenBounds(GameObject obj)
        {
            // neutralize any rotation
            Quaternion currentRotation = obj.transform.rotation;
            obj.transform.rotation = Quaternion.identity;

            Bounds bounds = new Bounds();
            foreach (Renderer renderer in obj.GetComponentsInChildren<Renderer>())
            {
                if (bounds.size == Vector3.zero)
                    bounds = renderer.bounds;
                else
                    bounds.Encapsulate(renderer.bounds);
            }

            // restore the rotation
            obj.transform.rotation = currentRotation;

            return bounds;
        }

        /// <summary>
        /// Calculate all next positions to show as preview if the option is enabled
        /// </summary>
        private static void CalculateFuturePositions(int i, int j)
        {
            // clean any previous position in list
            positions.Clear();

             if (!showPositions) return;

            // calculate capacity of the area
            int cols, rows;
            switch (gridMode)
            {
                default:
                case GridMode.FIT:
                    cols = (int) Math.Round((areaSize.x / objectsDistance.x) + 1);
                    rows = (int) Math.Round((areaSize.y / objectsDistance.y) + 1);
                    break;
                case GridMode.CLIP:
                    cols = (int) (Math.Round(areaSize.x, 8) / Math.Round(objectsDistance.x, 8) + 1);
                    rows = (int) (Math.Round(areaSize.y, 8) / Math.Round(objectsDistance.y, 8) + 1);
                    break;
            }

            Vector3 pos;
            while (j < rows)
            {
                // calc position
                pos = origin;
                pos += areaRight * (i * objectsDistance.x);
                pos += areaForward * (j * objectsDistance.y);
                // apply random if enabled
                ApplyRandomOffset(i, j, ref pos);
                // save position to show
                positions.Add(pos);
                // new row
                ++i;
                if (i >= cols)
                {
                    i = 0;
                    ++j;
                }
            }
        }

        /// <summary>
        /// Apply a random offset to each model
        /// </summary>
        private static void ApplyRandomOffset(int i, int j, ref Vector3 pos)
        {
            if (!randomOffset) return;

            int rows = (int) Math.Round((areaSize.y / objectsDistance.y)) + 1;

            float x, z;

            // x offset
            if (i == 0)
                x = UnityEngine.Random.Range(0f, 1f);
            else if (i == objectsPerRow - 1)
                x = UnityEngine.Random.Range(-1f, 0f);
            else
                x = UnityEngine.Random.Range(-1f, 1f);

            // z offset
            if (j == 0)
                z = UnityEngine.Random.Range(0f, 1f);
            else if (j == rows - 1)
                z = UnityEngine.Random.Range(-1f, 0f);
            else
                z = UnityEngine.Random.Range(-1f, 1f);

            // apply offsets
            pos += areaRight * x * (objectsDistance.x / 2f);
            pos += areaForward * z * (objectsDistance.y / 2f);

        }

        /// <summary>
        /// Apply boundary offset to make the RayCast always collide with the area mesh (avoiding float inaccuracies)
        /// </summary>
        private static void ApplyBoundaryOffset(int i, int j, ref Vector3 pos)
        {
            if (!placeOnGround) return;

            int rows = (int) Math.Round((areaSize.y / objectsDistance.y)) + 1;

            float x, z;

            // x offset
            if (i == 0)
                x = 0.005f;
            else if (i == objectsPerRow - 1)
                x = -0.005f;
            else
                x = 0;

            // z offset
            if (j == 0)
                z = 0.005f;
            else if (j == rows - 1)
                z = -0.005f;
            else
                z = 0;

            // apply offsets
            pos += areaRight * x * (objectsDistance.x / 2f);
            pos += areaForward * z * (objectsDistance.y / 2f);

        }

        /// <summary>
        /// Return false only if we are in clip mode and all rows are full of models and the option to grow is disabled
        /// </summary>
        public static bool CanAddMoreModels()
        {
            if (gridMode != GridMode.CLIP || objectsPerRow == 0 || canGrow) return true;

            // calculate capacity of the area
            int cols = (int) (Math.Round(areaSize.x, 3) / Math.Round(objectsDistance.x, 3) + 1);
            int rows = (int) (Math.Round(areaSize.y, 3) / Math.Round(objectsDistance.y, 3) + 1);

            return (objects.Count < cols * rows);
        }

        /// <summary>
        /// Remove all existing models and unassign the GameObject
        /// </summary>
        public static void Reset()
        {
            // remove any created model
            foreach (GameObject obj in objects)
                UnityEngine.Object.DestroyImmediate(obj);
            objects.Clear();

            // clean any previous position in list
            positions.Clear();

            Clear();
        }

        /// <summary>
        /// Keep all existing models and unassign the GameObject
        /// </summary>
        public static void KeepModelsAndReset()
        {
            // clean any previous position in list
            positions.Clear();
            objects.Clear();

            Clear();
        }

        /// <summary>
        /// Enable/Disable the colliders of the GameObject area so the models can ignore this when placed on the ground
        /// </summary>
        public static void EnableColliders(GameObject obj, bool value)
        {
            if (!obj) return;

            Collider[] colliders = obj.GetComponents<Collider>();
            foreach (Collider col in colliders)
                col.enabled = !value;
        }

        /// <summary>
        /// Show as red lines all next positions to be fill
        /// </summary>
        public static void DrawGizmos()
        {
            #if UNITY_EDITOR

            // check for invalid objects
            if (RemoveDestroyedObjects())
                RearrangeObjects();

            // draw a wireframe cube on each object
            UnityEditor.Handles.color = new Color(0f, 0f, 1f, 1f);
            foreach(GameObject obj in objects)
            {
                Bounds b = CalculateChildrenBounds(obj);
                UnityEditor.Handles.DrawWireCube(b.center, b.size);
            }

            if (!showPositions) return;

            UnityEditor.Handles.color = new Color(1f, 0f, 0f, 1f);
            foreach (Vector3 vec in positions)
            {
                UnityEditor.Handles.DrawLine(vec, vec + (Vector3.Cross(areaForward, areaRight).normalized * 2), 2);
            }
            #endif
        }

        /// <summary>
        /// Return true when all parameters are ready to be used
        /// </summary>
        public static bool IsReady()
        {
            return initialized;
        }

        /// <summary>
        /// Return true if we have saved previously backup parameters from the normal grid
        /// </summary>
        public static bool HasBackup()
        {
            return hasBackup;
        }

        /// <summary>
        /// Save those parameters coming from the normal grid
        /// </summary>
        public static void SetBackup(Vector3 _origin, float _cellWidth, int _cellCount)
        {
            backupGridOrigin = _origin;
            backupCellCount = _cellCount;
            backupCellWidth = _cellWidth;
            hasBackup = true;
        }

        /// <summary>
        /// Return the backup parameters tha were saved, to be restored when the grid area is disabled
        /// </summary>
        public static void GetBackup(ref Vector3 _origin, ref float _cellWidth, ref int _cellCount)
        {
            _origin = backupGridOrigin;
            _cellCount = backupCellCount;
            _cellWidth = backupCellWidth;
        }

        /// <summary>
        /// Remove from the list all objects that does not exist anymore in the scene
        /// </summary>
        private static bool RemoveDestroyedObjects()
        {
            bool result = false;

            // remove any destroyed object from the list
            int i = 0;
            while (i < objects.Count)
            {
                if (!objects[i])
                {
                    objects.RemoveAt(i);
                    result = true;
                }
                else
                    ++i;
            }

            return result;
        }
    }
}
