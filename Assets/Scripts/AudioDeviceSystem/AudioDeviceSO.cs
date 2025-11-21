using System;
using UnityEngine;

[CreateAssetMenu(fileName = "AudioDeviceSO", menuName = "Audio Device SO")]
public class AudioDeviceSO : ScriptableObject
{
    [Range(0,1)] public float sensitivity = 0.5f;
    public Texture2D savedSignalTexture;
}
