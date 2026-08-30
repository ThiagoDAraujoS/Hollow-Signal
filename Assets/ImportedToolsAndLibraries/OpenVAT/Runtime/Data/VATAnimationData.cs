// VATController.cs
// Author: Luke Stilson (sharpen3d)

using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "VATAnimationData", menuName = "VFX/VAT Animation Data")]
public class VATAnimationData : ScriptableObject
{
    [System.Serializable]
    public class VATAnimation
    {
        public string name;
        public int frameStart = 0;
        public int frameEnd = 30;
        public float framerate = 30f;
        public bool looping = true;
    }

    public List<VATAnimation> animations = new List<VATAnimation>();
}