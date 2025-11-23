using System;
using UnityEngine;

public class BirdManager : MonoBehaviour
{
    [SerializeField] GameObject[] birds;

    public Vector4[] worldBirdPositions = new Vector4[16];
    public Vector4[] normBirdPositions = new Vector4[16];

    [SerializeField] CustomRenderTexture birdMapRenderTexture;
    [Serializable] public struct MaterialIDs
    {
        internal int birdPositionID;
    }
    [SerializeField] MaterialIDs materialIDs;

    [SerializeField] Terrain terrain;
    private void Start()
    {
        Vector2 texSize = new Vector4(birdMapRenderTexture.width, birdMapRenderTexture.height);
        for (int i = 0; i < normBirdPositions.Length; i++)
        {
            float x1 = UnityEngine.Random.Range(0f, texSize.x);
            float y1 = UnityEngine.Random.Range(0f, texSize.y);
            float x2 = UnityEngine.Random.Range(0f, texSize.x);
            float y2 = UnityEngine.Random.Range(0f, texSize.y);

            worldBirdPositions[i] = new Vector4(x1, y1, x2, y2);


            normBirdPositions[i] = new Vector4 (worldBirdPositions[i].x / texSize.x, worldBirdPositions[i].y / texSize.y, worldBirdPositions[i].z / texSize.x, worldBirdPositions[i].w / texSize.y);
        }
        materialIDs.birdPositionID = Shader.PropertyToID("_BirdPositions");
        Shader.SetGlobalVectorArray(materialIDs.birdPositionID, normBirdPositions);


        for(int i = 0; i < worldBirdPositions.Length; i++)
        {
            Vector2 pos1 = new Vector2(worldBirdPositions[i].x, worldBirdPositions[i].y);
            float terYPos1 = terrain.SampleHeight(new Vector3(pos1.x, 0, pos1.y)) + terrain.transform.position.y;
            Instantiate(birds[0], new Vector3(pos1.x, terYPos1, pos1.y), Quaternion.identity);

            Vector2 pos2 = new Vector2(worldBirdPositions[i].z, worldBirdPositions[i].w);
            float terYPos2 = terrain.SampleHeight(new Vector3(pos2.x, 0, pos2.y)) + terrain.transform.position.y;
            Instantiate(birds[0], new Vector3(pos2.x, terYPos2, pos2.y), Quaternion.identity);
        }
    }
}
