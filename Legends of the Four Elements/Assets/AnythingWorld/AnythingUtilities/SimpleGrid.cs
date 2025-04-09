using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

namespace AnythingWorld.Utilities
{
    [Serializable]
    public static class SimpleGrid
    {
        [SerializeField]
        public static Vector3 origin = Vector3.zero;
        [SerializeField]
        public static int cellCount = 10;
        [SerializeField]
        public static float cellWidth = 1f;
        [SerializeField]
        public static int rows = 0;
        [SerializeField]
        public static int objectsInClosestRow = 0;
        private static void AddRow()
        {
            rows = rows + 1;
            objectsInClosestRow = 0;
        }
        public static Vector3 AddCell()
        {
            if (objectsInClosestRow == cellCount)
            {
                AddRow();
            }

            objectsInClosestRow = objectsInClosestRow + 1;
            var outputPosition = new Vector2(objectsInClosestRow * cellWidth, rows * cellWidth);
            var adjustedPosition = new Vector3(outputPosition.x, 0, outputPosition.y) + origin;
            return adjustedPosition;
        }
        public static void Reset()
        {
            rows = 0;
            objectsInClosestRow = 0;
        }


    }
}
