using Proselyte.Sigils;
using UnityEngine;
using UnityEngine.UI;

public class LaptopController : MonoBehaviour
{
    [SerializeField] PlayerSaveDataSO playerSaveDataSO;
    [SerializeField] GameObject media_explorer_prefab_file_image;
    [SerializeField] Transform media_explorer_panel_content_transform;

    [SerializeField] GameEvent OnPhotoTaken;
    [SerializeField] GameEvent OnInspectDisengageBegin;

    [SerializeField] RenderTexture photographic_camera_viewport_rendertexture;

    private int photos_stored_count;
    private bool laptop_in_use;

    private void OnEnable()
    {
        OnPhotoTaken.RegisterListener(HandlePhotoTaken);
        OnInspectDisengageBegin.RegisterListener(HandleInspectDisengage);
    }
    private void OnDisable()
    {
        OnPhotoTaken.UnregisterListener(HandlePhotoTaken);
        OnInspectDisengageBegin.UnregisterListener(HandleInspectDisengage);
    }

    private void Update()
    {
        if(laptop_in_use)
        {

        }
    }

    public void HandlePhotoTaken()
    {
        // TODO(Jazz): add upload sound emanating from laptop when receiving photo data from photographic camera


        // create new image prefab in the media explorer program
        GameObject file_image_gameObject = Instantiate(media_explorer_prefab_file_image, media_explorer_panel_content_transform);

        Image[] images = file_image_gameObject.GetComponentsInChildren<Image>();
        if(images[1] == null) Debug.LogError("prefab missing image component for photo.");

        // convert byte data to photos for the images
        int width = photographic_camera_viewport_rendertexture.width;
        int height = photographic_camera_viewport_rendertexture.height;
        Texture2D texture = new Texture2D(width, height, TextureFormat.ARGB32, false);
        texture.LoadImage(playerSaveDataSO.photo_taken_bytes);

        // assign sprite to image component
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
        images[1].sprite = sprite;
    }

    public void HandleInspectEngage()
    {
        laptop_in_use = true;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void HandleInspectDisengage()
    {
        laptop_in_use = false;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
}
