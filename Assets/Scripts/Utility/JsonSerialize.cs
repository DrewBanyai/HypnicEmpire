using Newtonsoft.Json;

namespace HypnicEmpire
{
    public static class JsonSerialization
    {
        public static string Serialize(object obj) => JsonConvert.SerializeObject(obj);
        public static Type Deserialize<Type>(string json) => JsonConvert.DeserializeObject<Type>(json);
    }
}

