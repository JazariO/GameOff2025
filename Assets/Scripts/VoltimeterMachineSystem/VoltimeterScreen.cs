using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Proselyte.Sigils;
using System;
using UnityEngine;

public class VoltimeterScreen : MonoBehaviour
{
    [SerializeField] CustomRenderTexture voltimeterRenderTexture;
    [Serializable] public struct MaterialVariableIDs
    {
        internal int powerAmountID;
        internal int releaseID;
    }
    MaterialVariableIDs matVarIDs;

    [Serializable] public struct RuntimeVariables
    {
        public float powerChangeTime;
        internal float elapsedTime;
        internal float newPowerAmount;
    }
    [SerializeField] RuntimeVariables runtimeVariables;

    [Serializable] public struct GameEventData
    {
        public GameEvent onReset;
    }
    [SerializeField] GameEventData gameEventData;
    private void Awake()
    {
        matVarIDs.powerAmountID = Shader.PropertyToID("_PowerAmount");
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.V) && runtimeVariables.elapsedTime == 0.0f)
        {
            runtimeVariables.newPowerAmount = UnityEngine.Random.Range(0.0f, 1.0f);
            ChangingVoltimeterReading(runtimeVariables.newPowerAmount).Forget();
        }
    }

    private async UniTaskVoid ChangingVoltimeterReading(float changeAmount)
    {
        float startPowerAmount = voltimeterRenderTexture.material.GetFloat(matVarIDs.powerAmountID);

        while (runtimeVariables.elapsedTime < runtimeVariables.powerChangeTime)
        {
            runtimeVariables.elapsedTime += Time.deltaTime;
            float t = runtimeVariables.elapsedTime / runtimeVariables.powerChangeTime;
            float curPowerAmount = Mathf.Lerp(startPowerAmount, changeAmount, t);
            voltimeterRenderTexture.material.SetFloat(matVarIDs.powerAmountID, curPowerAmount);
            await UniTask.Yield();
        }
        runtimeVariables.elapsedTime = 0.0f;
    }
}
