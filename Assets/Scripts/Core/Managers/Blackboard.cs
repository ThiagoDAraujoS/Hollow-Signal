using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Partition = System.Collections.Generic.Dictionary<string, object>;
using FileContainer = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, object>>;

namespace Core.Managers{
    [DisallowMultipleComponent]
    public class Blackboard : MonoBehaviour{
        public Dictionary<string, FileContainer> Files{ get; } = new(StringComparer.OrdinalIgnoreCase);

        //public bool TryGetFile(string fileName, out FileContainer data) => Files.TryGetValue(fileName, out data);

        public FileContainer GetOrCreateFile(string fileName){
            if (Files.TryGetValue(fileName, out FileContainer dict)) return dict;
            dict = new FileContainer(StringComparer.OrdinalIgnoreCase);

            Files[fileName] = dict;
            return dict;
        }

        public void Clear() => Files.Clear();
        
        public void ReleaseFile(string fileName) => Files.Remove(fileName);
        
        public bool Contains(string fileName) => Files.ContainsKey(fileName);

        private static string GetSaveFilePath(string fileName) => Path.Combine(SaveSystem.CurrentSaveSlotDirectory, $"{fileName}.json");
        private static string GetTempFilePath(string fileName) => Path.Combine(SaveSystem.TempDirectory,            $"{fileName}.json");

        public Partition GetPartition(string fileName, string partitionName){
            if (!Files[fileName].ContainsKey(partitionName))
                Files[fileName].Add(partitionName, new Partition(StringComparer.OrdinalIgnoreCase));
            return Files[fileName][partitionName];
        }

        /// Serializes all active file partitions in parallel to the temporary directory.
        public async Task SerializeBoard(Action<string> onFailure = null) {
            List<Task> tasks = Files.Select(board => Task.Run(() => {
                (string fileName, FileContainer data) = board;
                string filePath = GetTempFilePath(fileName);
                
                try {
                    string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                    File.WriteAllText(filePath, json);
                }
                catch (Exception e) {
                    onFailure?.Invoke($"Failed writing save file {fileName} asynchronously: {e.Message}");
                }
            })).ToList();

            await Task.WhenAll(tasks);
        }
        
        /// Deserializes multiple save files in parallel on background worker threads,
        public async Task DeserializeFiles(IEnumerable<string> fileNames, Action<string> onFailure = null) {
            List<Task<(string Name, FileContainer Data)>> tasks = fileNames.Select(fileName => Task.Run(() => {
                string filePath = GetSaveFilePath(fileName);
                
                if (!File.Exists(filePath)) 
                    return (fileName, new FileContainer(StringComparer.OrdinalIgnoreCase));
                
                try {
                    string        json     = File.ReadAllText(filePath);
                    FileContainer diskData = JsonConvert.DeserializeObject<FileContainer>(json, new SafeNumericConverter());
                    return (fileName, diskData ?? new FileContainer(StringComparer.OrdinalIgnoreCase));
                }
                catch (Exception e) {
                    onFailure?.Invoke($"Save file {fileName} is corrupted: {e.Message}");
                    return (fileName, null);
                }
            })).ToList();

            (string Name, FileContainer Data)[] results = await Task.WhenAll(tasks);
  
            foreach ((string fileName, FileContainer data) in results) 
                if (data != null)
                    Files[fileName] = data;
        }

        public class SafeNumericConverter : JsonConverter{
            public override bool CanConvert(Type objectType) => objectType == typeof(object);

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer){
                JToken token = JToken.Load(reader);
                return ReadToken(token);
            }

            private object ReadToken(JToken token){
                return token.Type switch{
                    JTokenType.Object => ConvertObject((JObject)token),
                    JTokenType.Array  => ConvertArray((JArray)token),
                    JTokenType.Null   => null,
                    _                 => ParsePrimitiveValue()
                };

                object ParsePrimitiveValue(){
                    object value = token.ToObject<object>();
                    if (value == null) return null;

                    return token.Type switch{
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
}
