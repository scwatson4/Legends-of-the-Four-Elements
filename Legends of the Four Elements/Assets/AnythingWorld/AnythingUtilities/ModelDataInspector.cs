using AnythingWorld.Utilities.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AnythingWorld.Utilities
{
    [Serializable]
    public class ModelDataInspector : MonoBehaviour
    {
        [SerializeField]
        public string guid = "";
        [SerializeField]
        public string behaviour = "";
        [SerializeField]
        public string[] tags = {""};
        [SerializeField]
        public string[] habitats = {""};
        [SerializeField]
        public string author = "";
        [SerializeField]
        public string entity = "";
        [SerializeField]
        public List<LabelledFloat> scales;
        [SerializeField]
        public List<LabelledFloat> movement;
        [SerializeField]
        public float mass;
        public void Populate(ModelData data)
        {
            guid = data.json.name;
            behaviour = data.json.behaviour;
            tags = data.json.tags;
            scales = ConvertToList(data.json.scale);
            movement = ConvertToList(data.json.movement);
            entity = data.json.entity;
            author = data.json.author;
            habitats = data.json.habitats;
            mass = data.json.mass;
        }

        public List<LabelledFloat> ConvertToList(Dictionary<string, float> dict)
        {
            if (dict == null) new List<LabelledFloat>();
            List<LabelledFloat> list = new List<LabelledFloat>();
            foreach(var kvp in dict.ToArray())
            {
                list.Add(new LabelledFloat(kvp.Key, kvp.Value));
            }
            return list;
        }
    }
    
    [System.Serializable]
    public class LabelledFloat
    {
        public LabelledFloat(string _label, float _value)
        {
            label = _label;
            value = _value;
        }
        [SerializeField]
        public string label;
        [SerializeField]
        public float value;
    }

}