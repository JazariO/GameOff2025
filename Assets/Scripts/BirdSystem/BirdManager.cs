using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

public class BirdManager : MonoBehaviour
{
    public Vector4[] birdPositions = new Vector4[16];

    [SerializeField] CustomRenderTexture birdMapRenderTexture;
    [Serializable] public struct MaterialIDs
    {
        public int birdPositionID;
    }
    [SerializeField] MaterialIDs materialIDs;
    private void Start()
    {
        Vector2 texSize = new Vector4(birdMapRenderTexture.width, birdMapRenderTexture.height);
        for (int i = 0; i < birdPositions.Length; i++)
        {
            float x1 = UnityEngine.Random.Range(0f, texSize.x);
            float y1 = UnityEngine.Random.Range(0f, texSize.y);
            float x2 = UnityEngine.Random.Range(0f, texSize.x);
            float y2 = UnityEngine.Random.Range(0f, texSize.y);

            birdPositions[i] = new Vector4(x1, y1, x2, y2);

            birdPositions[i] = new Vector4 (birdPositions[i].x / texSize.x, birdPositions[i].y / texSize.y, birdPositions[i].z / texSize.x, birdPositions[i].w / texSize.y);
        }
        materialIDs.birdPositionID = Shader.PropertyToID("_BirdPositions");
        Shader.SetGlobalVectorArray(materialIDs.birdPositionID, birdPositions);
    }
}
