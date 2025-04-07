using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AnythingWorld.Utilities.Data
{
    [System.Serializable]
    public abstract class ParameterVariable
    {
        public bool set = false;
        public bool IsSet => set;
    }
    [System.Serializable]
    public class Vector3Param : ParameterVariable
    {
        public Vector3 value;
        public Vector3Param() { }
        public Vector3Param(Vector3 val)
        {
            value = val;
            set = true;
        }
    }

    public class BoolParam : ParameterVariable
    {
        public bool value;
        public BoolParam(bool val = default)
        {
            value = val;
        }
    }
}
