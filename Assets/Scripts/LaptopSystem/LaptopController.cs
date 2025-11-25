using Proselyte.Sigils;
using UnityEngine;
using UnityEngine.UI;

public class LaptopController : MonoBehaviour
{
    [SerializeField] Image photoImage;
    [SerializeField] PlayerSaveDataSO playerSaveDataSO;

    [SerializeField] GameEvent OnPhotoTaken;

    [SerializeField] RenderTexture photographic_camera_viewport_rendertexture;

    private void OnEnable() { OnPhotoTaken.RegisterListener(HandlePhotoTaken); }
    private void OnDisable() { OnPhotoTaken.UnregisterListener(HandlePhotoTaken); }

    public void HandlePhotoTaken()
    {
        // convert byte data to photos for the images

        int width = photographic_camera_viewport_rendertexture.width;
        int height = photographic_camera_viewport_rendertexture.height;

        Texture2D texture = new Texture2D(width, height, TextureFormat.ARGB32, false);
        texture.LoadImage(playerSaveDataSO.photos_taken_bytes[0]);
        

        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
        sprite.name = "_MainTexdafdafsd";

        photoImage.sprite = sprite;

        // update all images in the media explorer view
    }
}
