using Proselyte.Sigils;
using System;
using UnityEngine;

public class PhotographicCameraController : MonoBehaviour
{
    [SerializeField] PlayerInputDataSO playerInputDataSO;

    [SerializeField] GameEvent OnInspectPhotographicCameraEngage;
    [SerializeField] GameEvent OnInspectPhotographicCameraDisengage;

    [SerializeField] Transform photographicCameraPivotTransform;

    [Serializable] struct CameraControls
    {
        public float rotation_limit_pitch_min;
        public float rotation_limit_pitch_max;
        public float rotation_limit_yaw_min;
        public float rotation_limit_yaw_max;
        public float zoom_fov_limit_min;
        public float zoom_fov_limit_max;
        public float zoom_fov_speed;
    } [SerializeField] CameraControls cameraControls;

    private bool engaged;
    private bool is_zooming;
    
    private float pitch;
    private float yaw;
    private float zoom;

    private void OnEnable()
    {
        OnInspectPhotographicCameraEngage.RegisterListener(HandleInspectEngage);
        OnInspectPhotographicCameraDisengage.RegisterListener(HandleInspectDisengage);
    }

    private void OnDisable()
    {
        OnInspectPhotographicCameraEngage.UnregisterListener(HandleInspectEngage);
        OnInspectPhotographicCameraDisengage.UnregisterListener(HandleInspectDisengage);
    }

    private void Update()
    {
        if(engaged)
        {
            // Rotate photographic camera using player move input
            // Accumulate player input
            pitch -= playerInputDataSO.input_move.y;
            yaw += playerInputDataSO.input_move.x;

            pitch = Mathf.Clamp(pitch, cameraControls.rotation_limit_pitch_min, cameraControls.rotation_limit_pitch_max);
            yaw = Mathf.Clamp(yaw, cameraControls.rotation_limit_yaw_min, cameraControls.rotation_limit_yaw_max);

            photographicCameraPivotTransform.localRotation = Quaternion.Euler(pitch, yaw, 0);
        }
    }

    public void HandleInspectEngage() { engaged = true; }
    public void HandleInspectDisengage() { engaged = false; }
}
