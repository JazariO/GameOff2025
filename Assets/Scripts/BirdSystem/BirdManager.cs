using System;
using UnityEngine;

public class BirdManager : MonoBehaviour
{
    [SerializeField] BirdManagerSettingsSOs settings;
    [SerializeField] BirdManagerStatsSOs stats;
    [SerializeField] GameObject[] birds;
    [SerializeField] TreeSO[] trees;
    [SerializeField] Terrain terrain;

    [Serializable] public struct PositionData
    {
        internal Vector4[] birdTargetPositions;
        internal Vector4[] birdWorldPositions;
    }
    [SerializeField] PositionData positionData;
    [Serializable] public struct MaterialIDs
    {
        internal int birdPositionID;
    }
    [SerializeField] MaterialIDs materialIDs;

    private void Awake()
    {
        positionData.birdTargetPositions = new Vector4[32];
        positionData.birdWorldPositions = new Vector4[32];

        materialIDs.birdPositionID = Shader.PropertyToID("_BirdPositions");
    }
    private void Start()
    {
        for (int i = 0; i < positionData.birdTargetPositions.Length; i++)
        {
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

            positionData.birdTargetPositions[i].x = worldPerchPos.x;
            positionData.birdTargetPositions[i].y = worldPerchPos.z;

            Vector2 pos = new Vector2(positionData.birdTargetPositions[i].x, positionData.birdTargetPositions[i].y);
                
            Vector2 halfTerrainSize = new Vector2(terrain.terrainData.size.x, terrain.terrainData.size.z) * 0.5f;
            Vector2 randCirclePos = UnityEngine.Random.insideUnitCircle * halfTerrainSize;
            float randHeight = UnityEngine.Random.Range(settings.spawnHeightRange.x, settings.spawnHeightRange.y);

            BirdBrain bird = Instantiate(birds[0], new Vector3(randCirclePos.x, randHeight, randCirclePos.y), Quaternion.identity).GetComponent<BirdBrain>();
            stats.birds[i] = bird;
            bird.statData.targetPosition = new Vector3(pos.x, worldPerchPos.y, pos.y);

        }
    }

    private void Update()
    {
        for(int i = 0; i < positionData.birdWorldPositions.Length; i++)
        {
            positionData.birdWorldPositions[i].x = stats.birds[i].transform.position.x;
            positionData.birdWorldPositions[i].y = stats.birds[i].transform.position.z;
        }
        
        Shader.SetGlobalVectorArray(materialIDs.birdPositionID, positionData.birdWorldPositions);
    }
}
