using Proselyte.Sigils;
using System;
using UnityEngine;

public class PhotographicCameraController : MonoBehaviour
{
    [SerializeField] PlayerInputDataSO playerInputDataSO;
    [SerializeField] UserSettingsDataSO userSettingsDataSO;
    [SerializeField] PlayerSaveDataSO playerSaveDataSO;

    [SerializeField] GameEvent OnInspectDisengageBegin;
    [SerializeField] GameEvent OnPhotoTaken;

    [SerializeField] Transform photographicCameraPivotTransform;
    [SerializeField] Camera photographicCameraComponent;

    [SerializeField] RenderTexture photographic_camera_viewport_rendertexture;

    [Serializable] struct CameraControls
    {
        [Range(-90,90)] public float rotation_limit_pitch_min;
        [Range(-90,90)] public float rotation_limit_pitch_max;
        [Range(-90,90)] public float rotation_limit_yaw_min;
        [Range(-90,90)] public float rotation_limit_yaw_max;
        [Range(0.01f,180)] public float zoom_fov_limit_min;
        [Range(0.01f,180)] public float zoom_fov_limit_max;
        [Range(0,1)]public float zoom_fov_speed;
        [Range(0,1)] public float zoom_fov_speed_scale_min;
        [Range(0,1)] public float zoom_fov_speed_scale_max;
    } [SerializeField] CameraControls cameraControls;

    private bool engaged;
    private bool is_zooming;
    
    private float pitch;
    private float yaw;
    private float zoom;
    private float zoom_percent;

    private void OnEnable()
    {
        OnInspectDisengageBegin.RegisterListener(HandleInspectDisengage);
    }

    private void OnDisable()
    {
        OnInspectDisengageBegin.UnregisterListener(HandleInspectDisengage);
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        zoom = photographicCameraComponent.fieldOfView;
        zoom = Mathf.Clamp(zoom, cameraControls.zoom_fov_limit_min, cameraControls.zoom_fov_limit_max);
        photographicCameraComponent.fieldOfView = zoom;
    }

    private void Update()
    {
        //if(playerInputDataSO.input_change_view && engaged)
        //{
        //    HandleInspectDisengage();
        //    return;
        //}

        if(engaged)
        {
            // Photographic Zoom
            {
                zoom -= playerInputDataSO.input_move.y * cameraControls.zoom_fov_speed;
                zoom = Mathf.Clamp(zoom, cameraControls.zoom_fov_limit_min, cameraControls.zoom_fov_limit_max);

                // Normalize zoom percentage between 0-1
                zoom_percent = Mathf.InverseLerp(zoom, cameraControls.zoom_fov_limit_min, cameraControls.zoom_fov_limit_max);

                // Apply sqrt remap
                zoom_percent = Mathf.Sqrt(zoom_percent);

                // Clamp to custom speed scale range
                zoom_percent = Mathf.Clamp(zoom_percent, cameraControls.zoom_fov_speed_scale_min, cameraControls.zoom_fov_speed_scale_max);

                photographicCameraComponent.fieldOfView = zoom;
            }


            // Rotate photographic camera with input
            {
                // Accumulate player input
                pitch -= playerInputDataSO.input_look.y * userSettingsDataSO.lookSensitivity * zoom_percent;
                yaw += playerInputDataSO.input_look.x * userSettingsDataSO.lookSensitivity * zoom_percent;

                pitch = Mathf.Clamp(pitch, cameraControls.rotation_limit_pitch_min, cameraControls.rotation_limit_pitch_max);
                yaw = Mathf.Clamp(yaw, cameraControls.rotation_limit_yaw_min, cameraControls.rotation_limit_yaw_max);

                photographicCameraPivotTransform.localRotation = Quaternion.Euler(pitch, yaw, 0);
            }

            // Take photo with photographic camera
            if(playerInputDataSO.input_interact)
            {
                // Set the active RenderTexture
                RenderTexture.active = photographic_camera_viewport_rendertexture;

                // Generate texture2D to hold the rendertexture data
                Texture2D texture2D = new Texture2D
                (
                    photographic_camera_viewport_rendertexture.width,
                    photographic_camera_viewport_rendertexture.height,
                    TextureFormat.ARGB32,
                    false
                );

                texture2D.ReadPixels(new Rect(0, 0, photographic_camera_viewport_rendertexture.width, photographic_camera_viewport_rendertexture.height), 0, 0);
                texture2D.Apply();

                // Reset active rendertexture to avoid breaking rendering
                RenderTexture.active = null;

                byte[] photoBytes = texture2D.EncodeToPNG();
                playerSaveDataSO.photos_taken_bytes.Add(photoBytes);
                OnPhotoTaken.Raise();
            }
        }
    }

    public void HandleInspectEngage() { engaged = true; }
    public void HandleInspectDisengage() { engaged = false; }
}
