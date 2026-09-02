using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Partition = System.Collections.Generic.Dictionary<string, object>;

namespace Core.Managers{
    [DisallowMultipleComponent]
    public class BlackBoard : MonoBehaviour{
        public Dictionary<string, Partition> Partitions{ get; } = new(StringComparer.OrdinalIgnoreCase);

        public bool TryGetPartition(string key, out Partition data) => Partitions.TryGetValue(key, out data);

        public Partition GetOrCreatePartition(string key){
            if (Partitions.TryGetValue(key, out Partition dict)) return dict;
            dict = new Partition(StringComparer.OrdinalIgnoreCase);

            Partitions[key] = dict;
            return dict;
        }

        public void SetPartition(string key, Partition data = null){
            Partitions[key] = data == null
                ? new Partition(StringComparer.OrdinalIgnoreCase)
                : new Partition(data, StringComparer.OrdinalIgnoreCase);
        }

        public void RemovePartition(string key) => Partitions.Remove(key);

        public void Clear() => Partitions.Clear();

        public bool Contains(string key) => Partitions.ContainsKey(key);

        public bool SerializeBoard(string path, Action<string> onFailure = null){
            try{
                string json      = JsonConvert.SerializeObject(Partitions, Formatting.Indented);
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
                File.WriteAllText(path, json);
                return true;
            }
            catch (Exception e){
                onFailure?.Invoke($"Failed writing on the file: {e.Message}");
                return false;
            }
        }
        
        public bool DeserializeAllBoards(string path, Action<string> onFailure = null){
            if (!File.Exists(path)){
                onFailure?.Invoke($"Save file not found at: {Path.GetFileName(path)}");
                return false;
            }

            try{
                string json = File.ReadAllText(path);
                Dictionary<string, Partition> diskData =
                    JsonConvert.DeserializeObject<Dictionary<string, Partition>>(json, new SafeNumericConverter());
                Clear();
                if (diskData != null){
                    foreach (KeyValuePair<string, Partition> kvp in diskData)
                        Partitions[kvp.Key] = new Partition(kvp.Value, StringComparer.OrdinalIgnoreCase);
                    return true;
                }
                onFailure?.Invoke("Save file is empty or invalid.");
                return false;
            }
            catch (Exception e){
                onFailure?.Invoke($"Save file is corrupted: {e.Message}");
                return false;
            }
        }
    }
    public class SafeNumericConverter : JsonConverter{
        public override bool CanConvert(Type objectType) => objectType == typeof(object);
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer){
            JToken token = JToken.Load(reader);
            return ReadToken(token);
        }
        private object ReadToken(JToken token) {
            return token.Type switch {
                JTokenType.Object => ConvertObject((JObject)token),
                JTokenType.Array  => ConvertArray((JArray)token),
                JTokenType.Null   => null,
                _                 => ParsePrimitiveValue() 
            };
            
            object ParsePrimitiveValue() {
                object value = token.ToObject<object>();
                if (value == null) return null;

                return token.Type switch {
                    JTokenType.Integer => Convert.ToInt32(value),
                    JTokenType.Float   => Convert.ToSingle(value),
                    JTokenType.Boolean => (bool)value,
                    JTokenType.String  => (string)value,
                    _                  => value
                };
            }
        }

        private object ConvertObject(JObject obj){
            Partition dict = new(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, JToken> prop in obj)
                dict[prop.Key] = ReadToken(prop.Value);
            return dict;
        }

        private object ConvertArray(JArray arr) => arr.Select(ReadToken).ToList();

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) => throw new NotImplementedException();
        public override bool CanWrite => false;
    }
}
