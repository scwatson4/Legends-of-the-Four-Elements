using System;
using System.Collections.Generic;
using UnityEngine;

namespace AnythingWorld.Utilities.Data
{
    [Serializable]
    public class ModelJson
    {
        [SerializeField]
        public string[] themeCategories;
        [SerializeField]
        public string name;
        [SerializeField]
        public string author = "";
        [SerializeField]
        public string _id;
        [SerializeField]
        public string behaviour;
        [SerializeField]
        public string type;
        [SerializeField]
        public string entity;
        [SerializeField]
        public string original_source;
        [SerializeField]
        public Model model;
        [SerializeField]
        public Dictionary<string, float> scale = new Dictionary<string, float>();
        [SerializeField]
        public Dictionary<string, float> movement = new Dictionary<string, float>();
        [SerializeField]
        public string detail;
        [SerializeField]
        public string[] tags;
        [SerializeField]
        public string[] habitats;
        [SerializeField]
        public bool preserveOriginalPosition = false;
        [SerializeField]
        public bool preserveOriginalRotation = false;
        [SerializeField]
        public bool preserveOriginalScale = false;
        [SerializeField]
        public Dictionary<string, string> thumbnails;
        [SerializeField]
        public string aw_thumbnail = "";
        [SerializeField]
        public int popularity = 0;
        [SerializeField]
        public int voteScore = 0;
        [SerializeField]
        public string userVote = "none";
        [SerializeField]
        public float mass = 1f;
        [SerializeField]
        public Dictionary<string, int> vert_count = new Dictionary<string, int>();
        [SerializeField]
        public Dictionary<string, int> poly_count = new Dictionary<string, int>();
        [SerializeField]
        public bool animated = false;

    }
}
