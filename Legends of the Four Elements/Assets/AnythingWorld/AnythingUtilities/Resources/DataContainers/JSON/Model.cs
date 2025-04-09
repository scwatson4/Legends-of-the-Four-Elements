using System;
using System.Collections.Generic;

namespace AnythingWorld.Utilities.Data
{
    [Serializable]
    public class Model
    {
        public Rig rig;
        //eg. Format url pair for static model, GLTF: "database.com/staticmodel.gltf"
        public Dictionary<string, string> formats;
        //Part name/Url pair for obj parts of the model.
        //Eg. body: "database.com/head.obj"
        public Dictionary<string, string> parts;
        public Dictionary<string, string> parts_fbx;
        public Other other;
    }     
}
