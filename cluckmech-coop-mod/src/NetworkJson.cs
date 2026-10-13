using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CluckmechCoop
{
    internal static class NetworkJson
    {
        internal const int MaxLength = 65536;
        internal static bool TryRead<T>(string json, out T message) where T : class
        {
            message = null;
            if (string.IsNullOrWhiteSpace(json) || json.Length > MaxLength) return false;
            try
            {
                using (var reader = new JsonTextReader(new StringReader(json)) { MaxDepth = 32, DateParseHandling = DateParseHandling.None })
                {
                    var value = JObject.Load(reader);
                    if (reader.Read() || value["Type"]?.Type != JTokenType.String || value["Version"]?.Type != JTokenType.String) return false;
                    message = value.ToObject<T>();
                    return message != null;
                }
            }
            catch (JsonException) { Plugin.Log("[Network] Ignored invalid JSON message"); return false; }
        }
    }
}
