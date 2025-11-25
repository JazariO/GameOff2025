using Proselyte.Sigils;
using UnityEngine;
using UnityEngine.UI;

public class LaptopController : MonoBehaviour
{
    [SerializeField] Image photoImage;
    [SerializeField] PlayerSaveDataSO playerSaveDataSO;
    [SerializeField] GameObject media_explorer_prefab_file_image;

    [SerializeField] GameEvent OnPhotoTaken;
    [SerializeField] GameEvent OnInspectDisengageBegin;

    [SerializeField] RenderTexture photographic_camera_viewport_rendertexture;

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
        
    }

    public void HandlePhotoTaken()
    {
        // TODO(Jazz): add upload sound emanating from laptop when receiving photo data from photographic camera



        // create new image in the media explorer program
        //Instantiate(media_explorer_prefab_file_image);

        // convert byte data to photos for the images
        int width = photographic_camera_viewport_rendertexture.width;
        int height = photographic_camera_viewport_rendertexture.height;
        Texture2D texture = new Texture2D(width, height, TextureFormat.ARGB32, false);
        texture.LoadImage(playerSaveDataSO.photos_taken_bytes[0]);
        

        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));

        photoImage.sprite = sprite;

        // update all images in the media explorer view
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
