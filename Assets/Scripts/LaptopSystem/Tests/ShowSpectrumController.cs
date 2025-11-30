using UnityEngine;
using UnityEngine.UI;

public class ShowSpectrumController : MonoBehaviour
{
    [SerializeField] PlayerSaveDataSO playerSaveDataSO;
    [SerializeField] Image spectrumTexture;

    public void ShowSpectrumTexture()
    {
        Texture2D texture = new Texture2D(1024, 256, TextureFormat.RGB24, false);
        texture.LoadRawTextureData(playerSaveDataSO.spectrumTextureData);
        texture.Apply();

        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 1024, 256), new Vector2(0.5f, 0.5f));

        spectrumTexture.sprite = sprite;
    }
}
