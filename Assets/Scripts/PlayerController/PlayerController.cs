using Proselyte.Sigils;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] PlayerInputDataSO playerInputData;
    [SerializeField] UserSettingsDataSO userSettingsData;
    [SerializeField] Transform cameraPivot;   // Controls yaw (horizontal)
    [SerializeField] Transform cameraTransform; // Controls pitch (vertical)

    [SerializeField] GameEvent OnInspectEngage;
    [SerializeField] GameEvent OnInspectDisengage;

    private Vector3 startPos;
    private float pitch = 0f;
    private bool canLook;

    private void OnEnable()
    {
        OnInspectEngage.RegisterListener(SetCanLookFalse);
        OnInspectDisengage.RegisterListener(SetCanLookTrue);
    }

    private void OnDisable()
    {
        OnInspectEngage.UnregisterListener(SetCanLookFalse);
        OnInspectDisengage.UnregisterListener(SetCanLookTrue);
    }

    public void SetCanLookTrue() { canLook = true; }
    public void SetCanLookFalse() { canLook = false; }

    private void Start()
    {
        startPos = transform.position;
        canLook = true;
    }

    private void Update()
    {
        if(!canLook) return;

        Vector2 lookInput = playerInputData.input_look;
        float sensitivity = userSettingsData.lookSensitivity;

        // Horizontal rotation (yaw) on the pivot
        cameraPivot.Rotate(lookInput.x * sensitivity * Vector3.up);

        // Vertical rotation (pitch) on the camera
        pitch -= lookInput.y * sensitivity;
        pitch = Mathf.Clamp(pitch, -80f, 80f);
        cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }
}