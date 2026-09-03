using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Core.Config{
    
    [CreateAssetMenu(fileName = "SceneDependencyConfig", menuName = "CRPG/Config/Scene Dependencies")]
    public class SceneDependencyDatabase : ScriptableObject{
        [SerializeField] private List<SceneDependencies> dependencies = new();

        public List<string> GetSceneDependencies(string sceneName){
            foreach (SceneDependencies dep in dependencies.Where(dep => string.Equals(dep.sceneName, sceneName, StringComparison.OrdinalIgnoreCase)))
                return dep.requiredFiles;
            return new List<string>();
        }
    }
    [Serializable]
    public struct SceneDependencies{
        [Tooltip("The exact name of the Unity Scene file.")]
        public string sceneName;

        [Tooltip("The partition files that must be loaded into RAM before this scene is opened.")]
        public List<string> requiredFiles;
    }
}