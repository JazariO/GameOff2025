using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerSaveDataSO", menuName = "Save-Load System/Player Save Data SO")]
public class PlayerSaveDataSO : ScriptableObject
{
    public Vector3 position;
    public Vector3 cameraPosition;
    public Quaternion cameraRotation;
    public bool isInspecting;

    public byte[] photo_taken_bytes;

    public Vector2 laptop_canvas_mouse_position;

    // Audio clip storage (mono, 16-bit PCM)
    public int audioBitDepth;
    public byte[] audioClipData;       // 16-bit PCM audio samples as bytes
    public int audioSampleRate;        // Sample rate (e.g., 48000)
    public float audioClipDuration;    // Duration in seconds (should be 3.0f)

    public byte[] spectrumTextureData; // RGB24 format: 1024 * 256 * 3 bytes
}
