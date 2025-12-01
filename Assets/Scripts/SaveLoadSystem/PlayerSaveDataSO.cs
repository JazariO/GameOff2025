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
}
