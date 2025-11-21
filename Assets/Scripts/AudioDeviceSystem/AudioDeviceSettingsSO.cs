using System;
using UnityEngine;

[CreateAssetMenu(fileName = "AudioDeviceSettingsSO", menuName = "Audio Device Settings SO")]
public class AudioDeviceSettingsSO : ScriptableObject
{
   [Range(0,1)] public float sensitivity = 0.5f;
}
