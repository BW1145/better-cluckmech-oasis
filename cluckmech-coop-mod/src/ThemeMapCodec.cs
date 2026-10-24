using System;
using Newtonsoft.Json;
using UnityEngine;

namespace CluckmechCoop
{
    // Map fields retain the native layout; database assets cross saves by their native IDs.
    internal static class ThemeMapCodec
    {
        private static JsonSerializerSettings Settings() => new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.None,
            Converters = { new MapAssetIds(), new MapGridValues() }
        };

        internal static string Encode(地图管理器.地图存档类 map) => JsonConvert.SerializeObject(map, Settings());
        internal static 地图管理器.地图存档类 Decode(string json) =>
            JsonConvert.DeserializeObject<地图管理器.地图存档类>(json, Settings());
        internal static 地图管理器.地图存档类 Clone(地图管理器.地图存档类 map) => Decode(Encode(map));

        private sealed class MapAssetIds : JsonConverter
        {
            public override bool CanConvert(Type type) => type == typeof(关卡挑战模版) || type == typeof(关卡奖励模版);
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                if (value == null) { writer.WriteNull(); return; }
                writer.WriteValue(value is 关卡挑战模版 challenge ? challenge.id : ((关卡奖励模版)value).id);
            }
            public override object ReadJson(JsonReader reader, Type type, object value, JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.Null) return null;
                int id = Convert.ToInt32(reader.Value, System.Globalization.CultureInfo.InvariantCulture);
                if (type == typeof(关卡挑战模版))
                {
                    var asset = UnityEngine.Object.FindObjectOfType<数据库_关卡挑战>()?.查询关卡挑战(id);
                    if (asset != null) return asset;
                    foreach (var loaded in Resources.FindObjectsOfTypeAll<关卡挑战模版>()) if (loaded.id == id) return loaded;
                }
                else
                {
                    var asset = UnityEngine.Object.FindObjectOfType<数据库_关卡奖励>()?.查询关卡奖励(id);
                    if (asset != null) return asset;
                    foreach (var loaded in Resources.FindObjectsOfTypeAll<关卡奖励模版>()) if (loaded.id == id) return loaded;
                }
                throw new JsonSerializationException("Map database asset missing: " + type.Name + "/" + id);
            }
        }

        private sealed class MapGridValues : JsonConverter
        {
            public override bool CanConvert(Type type) => type == typeof(Vector2Int) || type == typeof(Vector3Int);
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                writer.WriteStartArray();
                if (value is Vector2Int two) { writer.WriteValue(two.x); writer.WriteValue(two.y); }
                else { var three = (Vector3Int)value; writer.WriteValue(three.x); writer.WriteValue(three.y); writer.WriteValue(three.z); }
                writer.WriteEndArray();
            }
            public override object ReadJson(JsonReader reader, Type type, object value, JsonSerializer serializer)
            {
                var coordinates = serializer.Deserialize<int[]>(reader);
                if (coordinates == null || coordinates.Length != (type == typeof(Vector2Int) ? 2 : 3))
                    throw new JsonSerializationException("Invalid map grid value");
                if (type == typeof(Vector2Int)) return new Vector2Int(coordinates[0], coordinates[1]);
                return new Vector3Int(coordinates[0], coordinates[1], coordinates[2]);
            }
        }
    }
}
