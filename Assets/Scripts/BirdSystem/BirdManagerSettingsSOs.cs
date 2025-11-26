using UnityEngine;

[CreateAssetMenu(fileName = "BirdManagerSettingsSOs", menuName = "Bird Manager Settings SOs") ]
public class BirdManagerSettingsSOs : ScriptableObject
{
    public Vector2 spawnHeightRange = new Vector2(10f, 80f);
    public CustomRenderTexture birdMapRenderTexture;
}
