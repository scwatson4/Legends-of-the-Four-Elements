using AnythingWorld.Utilities.Data;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System.Collections.Generic;
using System.Linq;

namespace AnythingWorld.Utilities
{
    public class JsonDeserializer 
    {
        public static List<ModelJson> DeserializeModelJsonList(string json)
        {
            List<ModelJson> processedModelJsonList = new List<ModelJson>();
            JArray modelJsonArray = JArray.Parse(json);

            foreach(var token in modelJsonArray)
            {
                processedModelJsonList.Add(DeserializeModelJson(token.ToString()));
            }

            return processedModelJsonList;
        }

        public static ModelJson DeserializeModelJson(string json)
        {
            ModelJson processedModelJson = new ModelJson();
            JObject modelJsonObject = JObject.Parse(json);
            processedModelJson._id = modelJsonObject.Value<string>("_id");
            processedModelJson.name = modelJsonObject.Value<string>("name");
            processedModelJson.author = modelJsonObject.Value<string>("author");
            processedModelJson.original_source = modelJsonObject.Value<string>("original_source");
            processedModelJson.animated = modelJsonObject.Value<bool>("animated");
            
            processedModelJson.type = modelJsonObject.Value<string>("type");
            processedModelJson.entity = modelJsonObject.Value<string>("entity");
            processedModelJson.behaviour = modelJsonObject.Value<string>("behaviour");
            if (modelJsonObject.TryGetValue("themeCategories", out JToken themeCategories)) processedModelJson.themeCategories = JsonConvert.DeserializeObject<string[]>(themeCategories.ToString());
            if (modelJsonObject.TryGetValue("tags", out JToken tags)) processedModelJson.tags = JsonConvert.DeserializeObject<string[]>(tags.ToString());
            if (modelJsonObject.TryGetValue("habitats", out JToken habitats)) processedModelJson.habitats = JsonConvert.DeserializeObject<string[]>(habitats.ToString());

            if (modelJsonObject.TryGetValue("scale", out JToken scale)) processedModelJson.scale = JsonConvert.DeserializeObject<Dictionary<string, float>>(scale.ToString());
            if (modelJsonObject.TryGetValue("movement", out JToken movement)) processedModelJson.movement = JsonConvert.DeserializeObject<Dictionary<string, float>>(movement.ToString());
            processedModelJson.detail = modelJsonObject.Value<string>("detail");
            processedModelJson.mass = modelJsonObject.Value<float>("mass");
            if (modelJsonObject.TryGetValue("vert_count", out JToken vert_count)) processedModelJson.vert_count = JsonConvert.DeserializeObject<Dictionary<string, int>>(vert_count.ToString());
            if (modelJsonObject.TryGetValue("poly_count", out JToken poly_count)) processedModelJson.poly_count = JsonConvert.DeserializeObject<Dictionary<string, int>>(poly_count.ToString());
            
            processedModelJson.preserveOriginalPosition = modelJsonObject.Value<bool>("preserveOriginalPosition");
            processedModelJson.preserveOriginalRotation = modelJsonObject.Value<bool>("preserveOriginalRotation");
            processedModelJson.preserveOriginalScale = modelJsonObject.Value<bool>("preserveOriginalScale");

            if (modelJsonObject.TryGetValue("thumbnails", out JToken thumbnails)) processedModelJson.thumbnails = JsonConvert.DeserializeObject<Dictionary<string, string>>(thumbnails.ToString());
            processedModelJson.aw_thumbnail = modelJsonObject.Value<string>("aw_thumbnail");
            
            processedModelJson.popularity = modelJsonObject.Value<int>("popularity");
            processedModelJson.voteScore = modelJsonObject.Value<int>("voteScore");
            processedModelJson.userVote = modelJsonObject.Value<string>("userVote");

            if (modelJsonObject.ContainsKey("model"))
            {
                processedModelJson.model = ParseModel((JObject)modelJsonObject["model"]);
                if (processedModelJson.model.other.texture == null && modelJsonObject.ContainsKey("textures"))
                {
                    if (((JObject)modelJsonObject["textures"]).TryGetValue("original", out JToken textures)) processedModelJson.model.other.texture = JsonConvert.DeserializeObject<string[]>(textures.ToString()).Distinct().ToArray();
                }
            }

            return processedModelJson;
        }

        private static Model ParseModel(JObject modelDirective)
        {
            Model model = new Model();

            if (modelDirective.TryGetValue("formats", out JToken formats)) model.formats = JsonConvert.DeserializeObject<Dictionary<string, string>>(formats.ToString());
            if (modelDirective.ContainsKey("parts"))
            {
                JObject partsList = (JObject)modelDirective["parts"];
                model.parts = ParseVehicleParts(partsList);
            }
            if (modelDirective.ContainsKey("parts_fbx"))
            {
                JObject partsList = (JObject)modelDirective["parts_fbx"];
                model.parts_fbx = ParseVehicleParts(partsList);
            }

            if (modelDirective.ContainsKey("rig"))
            {
                Rig rig = new Rig();
                JObject rigJObject = (JObject)modelDirective["rig"];
                rig.animations = new Dictionary<string, AnimationFormats>();
                if (rigJObject.ContainsKey("animations"))
                {
                    foreach (KeyValuePair<string, JToken> animationToken in (JObject)rigJObject.GetValue("animations"))
                    {
                        rig.animations.Add(animationToken.Key, new AnimationFormats()
                        {
                            GLB = animationToken.Value["GLB"]?.ToString(),
                            FBX = animationToken.Value["FBX"]?.ToString()
                        });
                    }
                }
                model.rig = rig;
            }

            Other other = new Other();
            JObject otherJObject = (JObject)modelDirective["other"];
            other.material = otherJObject.Value<string>("material");
            if(otherJObject.TryGetValue("texture", out JToken texture)) other.texture = JsonConvert.DeserializeObject<string[]>(texture.ToString());
            other.model = otherJObject.Value<string>("model");
            model.other = other;

            return model;
        }

        public static Dictionary<string, string> ParseVehicleParts(JObject partsDirective)
        {
            Dictionary<string, string> finalPartsDictionary = new Dictionary<string, string>();
            foreach (KeyValuePair<string, JToken> token in partsDirective)
            {
                if (token.Value.Type != JTokenType.Object && token.Value.Type != JTokenType.Property)
                {
                    finalPartsDictionary.Add(token.Key, token.Value.ToString());
                }
                else
                {
                    finalPartsDictionary = finalPartsDictionary.Concat(ParseVehicleParts((JObject)token.Value)
                                                                   .Where(kvp => !finalPartsDictionary
                                                                       .ContainsKey(kvp.Key)))
                                                               .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                }
            }

            return finalPartsDictionary;
        }
    }
}
