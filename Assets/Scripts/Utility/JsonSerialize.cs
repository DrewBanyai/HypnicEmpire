using Newtonsoft.Json;

namespace Sleepwalking
{
    public static class JsonSerialization
    {
        public static string Serialize(object obj) => JsonConvert.SerializeObject(obj);
        public static Type Deserialize<Type>(string json) => JsonConvert.DeserializeObject<Type>(json);
    }
}

