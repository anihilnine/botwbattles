using System;
using UnityEngine;

[Serializable]
public class AnimLayerData
{
    public string key;
    public bool normalizeWeights;
    public bool allowNonCrossOverFades;
    
    [Header("=== Debug")] 
    public float sumWeights;
}