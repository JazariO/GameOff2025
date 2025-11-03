using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

public class VoltimeterScreen : MonoBehaviour
{
    Renderer objRenderer;
    MaterialPropertyBlock mpb;

    float dropTime;
    float totalTime;
    float elapsedTime;
    bool increaseReading;
    public struct MaterialVariableIDs
    {
        internal int deltaTimeID;
        internal int dropTimeID;
        internal int speedID;
        internal int increaseReadingID;
    }
    MaterialVariableIDs matVarIDs;


    private void Awake()
    {
        matVarIDs.dropTimeID = Shader.PropertyToID("_DropTime");
        matVarIDs.speedID = Shader.PropertyToID("_Speed");
        matVarIDs.deltaTimeID = Shader.PropertyToID("_DeltaTime");
        matVarIDs.increaseReadingID = Shader.PropertyToID("_IncreaseReading");
        mpb = new MaterialPropertyBlock();
        objRenderer = GetComponent<Renderer>();
    }

    private void Start()
    {
        mpb.SetFloat(matVarIDs.dropTimeID, 0.0f);
        objRenderer.SetPropertyBlock(mpb);

        float speed = objRenderer.sharedMaterial.GetFloat("_Speed");
        dropTime = 16.0f * speed;
    }

    private void Update()
    {
        totalTime += Time.deltaTime;
        mpb.SetFloat(matVarIDs.deltaTimeID, totalTime);
        objRenderer.SetPropertyBlock(mpb);

        if (Input.GetKeyDown(KeyCode.V) && elapsedTime == 0.0f)
        {
            increaseReading = increaseReading ? false : true;
            mpb.SetFloat(matVarIDs.increaseReadingID, increaseReading ? 1.0f : 0.0f);
            ChangingVoltimeterReading(increaseReading).Forget();
        }
    }

    private async UniTaskVoid ChangingVoltimeterReading(bool increase)
    {
        while (elapsedTime < dropTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / dropTime;
            if (increase) t = 1.0f - t;
            mpb.SetFloat(matVarIDs.dropTimeID, t);
            objRenderer.SetPropertyBlock(mpb);
            await UniTask.Yield();
        }

        elapsedTime = 0.0f;
    }
}
