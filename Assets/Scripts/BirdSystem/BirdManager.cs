using System;
using UnityEngine;

public class BirdManager : MonoBehaviour
{
    [SerializeField] GameObject[] birds;
    [SerializeField] TreeSO[] trees;
    [SerializeField] Terrain terrain;
    [SerializeField] CustomRenderTexture birdMapRenderTexture;

    [Serializable] public struct PositionData
    {
        internal Vector4[] worldBirdPositions;
        internal Vector4[] normBirdPositions;
    }
    [SerializeField] PositionData positionData;
    [Serializable] public struct MaterialIDs
    {
        internal int birdPositionID;
    }
    [SerializeField] MaterialIDs materialIDs;

    private void Awake()
    {
        positionData.worldBirdPositions = new Vector4[16];
        positionData.normBirdPositions = new Vector4[16];
    }
    private void Start()
    {
        Vector2 texSize = new Vector4(birdMapRenderTexture.width, birdMapRenderTexture.height);
        for (int i = 0; i < positionData.worldBirdPositions.Length; i++)
        {
            for (int j = 0; j < 2;  j++)
            {
                bool isEven = j % 2 == 0;

                int randTreeIndex = UnityEngine.Random.Range(0, terrain.terrainData.treeInstanceCount);
                TreeInstance tree = terrain.terrainData.treeInstances[randTreeIndex];

                Vector3 treePos = new Vector3(tree.position.x * terrain.terrainData.size.x, tree.position.y * terrain.terrainData.size.y, tree.position.z * terrain.terrainData.size.z) + terrain.transform.position;

                TreeSO curTree = null;
                for (int k = 0; k < trees.Length; k++)
                {
                    if (terrain.terrainData.treePrototypes[tree.prototypeIndex].prefab == trees[k].tree)
                    {
                        curTree = trees[k]; 
                        break;
                    }
                }

                if (curTree == null) { Debug.LogError("Did not find tree type"); return; }
                int randPerchIndex = UnityEngine.Random.Range(0, curTree.perchesPositions.Length);
                Vector3 localPerchPos = curTree.perchesPositions[randPerchIndex];
                Vector3 scaledPerchPos = new Vector3(localPerchPos.x * tree.widthScale, localPerchPos.y * tree.heightScale, localPerchPos.z * tree.widthScale);

                Quaternion treeRot = Quaternion.Euler(0, tree.rotation * Mathf.Rad2Deg, 0);
                Vector3 worldPerchPos = treeRot * scaledPerchPos + treePos;

                float terYPos = worldPerchPos.y;
                if (isEven)
                {
                    positionData.worldBirdPositions[i].x = worldPerchPos.x;
                    positionData.worldBirdPositions[i].y = worldPerchPos.z;

                    Vector2 pos = new Vector2(positionData.worldBirdPositions[i].x, positionData.worldBirdPositions[i].y);
                    Instantiate(birds[0], new Vector3(pos.x, terYPos, pos.y), Quaternion.identity);
                }
                else
                {
                    positionData.worldBirdPositions[i].z = worldPerchPos.x;
                    positionData.worldBirdPositions[i].w = worldPerchPos.z;

                    Vector2 pos = new Vector2(positionData.worldBirdPositions[i].z, positionData.worldBirdPositions[i].w);
                    Instantiate(birds[0], new Vector3(pos.x, terYPos, pos.y), Quaternion.identity);
                }
            }

            positionData.normBirdPositions[i] = new Vector4 (positionData.worldBirdPositions[i].x / texSize.x, positionData.worldBirdPositions[i].y / texSize.y, positionData.worldBirdPositions[i].z / texSize.x, positionData.worldBirdPositions[i].w / texSize.y);
        }
        materialIDs.birdPositionID = Shader.PropertyToID("_BirdPositions");
        Shader.SetGlobalVectorArray(materialIDs.birdPositionID, positionData.normBirdPositions);
    }
}
