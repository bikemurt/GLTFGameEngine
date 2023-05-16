using glTFLoader.Schema;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLTFGameEngine
{
    
    public class KHRLightsPunctualExt
    {
        [Newtonsoft.Json.JsonProperty("light")]
        public int? Light;
    }
    public class KHRLightsPunctual
    {
        [Newtonsoft.Json.JsonProperty("lights")]
        public KHRLightsPunctualLight[] Lights;
    }
    public class KHRLightsPunctualLight
    {
        [Newtonsoft.Json.JsonProperty("color")]
        public float[]? Color;

        [Newtonsoft.Json.JsonProperty("intensity")]
        public float? Intensity;

        [Newtonsoft.Json.JsonProperty("type")]
        public string? Type;

        [Newtonsoft.Json.JsonProperty("name")]
        public string? Name;
    }



    internal class Light
    {
        public Vector3 Color = new Vector3(1, 1, 1);
        public Vector3 Position = new Vector3(1, 3, 0);
        public float Intensity = 100f;
        public string LightType = "point";
    }
}
