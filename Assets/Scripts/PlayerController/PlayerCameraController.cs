using Proselyte.Sigils;
using UnityEngine;

public class PlayerCameraController : MonoBehaviour
{
    [SerializeField] private PlayerInputDataSO playerInputData;
    [SerializeField] private UserSettingsDataSO userSettingsData;
    [SerializeField] private Transform cameraPivot;   // Controls yaw (horizontal)
    [SerializeField] private Transform cameraTransform; // Controls pitch (vertical)

    private float pitch = 0f;

    private void Update()
    {
        Vector2 lookInput = playerInputData.input_look;
        float sensitivity = userSettingsData.lookSensitivity;

        // Horizontal rotation (yaw) on the pivot
        cameraPivot.Rotate(Vector3.up * lookInput.x * sensitivity);

        // Vertical rotation (pitch) on the camera
        pitch -= lookInput.y * sensitivity;
        pitch = Mathf.Clamp(pitch, -80f, 80f);
        cameraTransform.localEulerAngles = new Vector3(pitch, 0f, 0f);
    }
}