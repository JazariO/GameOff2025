using System;
using UnityEngine;

public class BirdManager : MonoBehaviour
{
    [SerializeField] GameObject[] birds;
    [SerializeField] TreeSO mountainAsh;

    [Serializable] public struct PositionData
    {

    }
    public Vector4[] worldBirdPositions = new Vector4[16];
    public float[] worldBirdHeight = new float[32];
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
            for (int j = 0; j < 2;  j++)
            {
                bool isEven = j % 2 == 0;

                int randTreeIndex = UnityEngine.Random.Range(0, terrain.terrainData.treeInstanceCount);
                TreeInstance tree = terrain.terrainData.treeInstances[randTreeIndex];

                Vector3 treePos = new Vector3(tree.position.x * terrain.terrainData.size.x, tree.position.y * terrain.terrainData.size.y, tree.position.z * terrain.terrainData.size.z) + terrain.transform.position;
                
                int randPerchIndex = UnityEngine.Random.Range(0, mountainAsh.perchesPositions.Length);

                Vector3 localPerchPos = mountainAsh.perchesPositions[randPerchIndex];
                Vector3 scaledPerchPos = new Vector3(localPerchPos.x * tree.widthScale, localPerchPos.y * tree.heightScale, localPerchPos.z * tree.widthScale);

                Quaternion treeRot = Quaternion.Euler(0, tree.rotation * Mathf.Rad2Deg, 0);
                Vector3 worldPerchPos = treeRot * scaledPerchPos + treePos;

                if (isEven)
                {
                    worldBirdPositions[i].x = worldPerchPos.x;
                    worldBirdPositions[i].y = worldPerchPos.z;

                    Vector2 pos = new Vector2(worldBirdPositions[i].x, worldBirdPositions[i].y);
                    float terYPos = terrain.SampleHeight(new Vector3(pos.x, worldPerchPos.y, pos.y)) + terrain.transform.position.y + worldPerchPos.y;
                    Instantiate(birds[0], new Vector3(pos.x, terYPos, pos.y), Quaternion.identity);
                }
                else
                {
                    worldBirdPositions[i].z = worldPerchPos.x;
                    worldBirdPositions[i].w = worldPerchPos.z;

                    Vector2 pos = new Vector2(worldBirdPositions[i].z, worldBirdPositions[i].w);
                    float terYPos = terrain.SampleHeight(new Vector3(pos.x, 0, pos.y)) + terrain.transform.position.y + worldPerchPos.y;
                    Instantiate(birds[0], new Vector3(pos.x, terYPos, pos.y), Quaternion.identity);
                }

            }

            normBirdPositions[i] = new Vector4 (worldBirdPositions[i].x / texSize.x, worldBirdPositions[i].y / texSize.y, worldBirdPositions[i].z / texSize.x, worldBirdPositions[i].w / texSize.y);
        }
        materialIDs.birdPositionID = Shader.PropertyToID("_BirdPositions");
        Shader.SetGlobalVectorArray(materialIDs.birdPositionID, normBirdPositions);
    }
}
