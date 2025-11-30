using Cysharp.Threading.Tasks;
using Proselyte.Sigils;
using System;
using System.IO;
using UnityEngine;

public class AudioDevice : MonoBehaviour
{
    [SerializeField] AudioDeviceSO settings;
    [SerializeField] PlayerInputDataSO playerInputData;

    [SerializeField] GameEvent OnInspectDisengageBegin;
    [SerializeField] Renderer polarCoordRenderer;
    [SerializeField] Renderer distanceCoordRenderer;
    [SerializeField] Renderer signalRenderer;

    MaterialPropertyBlock polarCoordMPB;
    MaterialPropertyBlock distanceCoordMPB;
    MaterialPropertyBlock signalMPB;

    [SerializeField] CustomRenderTexture signalRenderTexture;
    Material signalRenTexMat;
    RenderTexture recordedSignalRenTex;
    [SerializeField] Material recordedSignalMaterial;
    [SerializeField] Transform polarCoordKnob;
    [SerializeField] Transform distanceCoordKnob;
    [SerializeField] Transform micPivotYawTransform;
    [SerializeField] Transform micPivotPitchTransform;

    [Serializable] struct MaterialIDs
    {
        internal int signalAngleID;
        internal int signalDistanceID;
        internal int recordLengthID;
        internal int signalHorizontalSpeedID;
        internal int recordedSignalTextureID;
    }
    MaterialIDs materialIDs;

    [Serializable] public struct Stats
    {
        internal float signalAngle;
        internal float signalDistance;
        internal float recordLength;
        internal float recordLengthTime;
        internal bool recording;
        internal bool engaged;
    }
    public Stats stats;

    private float cachedMicYaw;
    private float cachedMicPitch;

    private void Awake()
    {
        materialIDs.signalAngleID = Shader.PropertyToID("_SignalAngle");
        materialIDs.signalDistanceID = Shader.PropertyToID("_SignalDistance");
        materialIDs.recordLengthID = Shader.PropertyToID("_RecordLength");
        materialIDs.signalHorizontalSpeedID = Shader.PropertyToID("_HorizontalSpeed");
        materialIDs.recordedSignalTextureID = Shader.PropertyToID("_RecordedSignalTexture");

        polarCoordMPB = new MaterialPropertyBlock();
        distanceCoordMPB = new MaterialPropertyBlock();
        signalMPB = new MaterialPropertyBlock();

        signalRenTexMat = signalRenderTexture.material;

        recordedSignalRenTex = new RenderTexture(signalRenderTexture.width, signalRenderTexture.height, 0, signalRenderTexture.format);
        recordedSignalRenTex.Create();
    }

    private void Start()
    {
        // NOTE(Jazz): initialize the signal angle and distance from here rather than reading from the material.
        stats.signalAngle = 0;
        stats.signalDistance = 1;

        polarCoordMPB.SetFloat(materialIDs.signalAngleID, stats.signalAngle);
        distanceCoordMPB.SetFloat(materialIDs.signalAngleID, stats.signalAngle);
        polarCoordMPB.SetFloat(materialIDs.signalDistanceID, stats.signalDistance);
        distanceCoordMPB.SetFloat(materialIDs.signalDistanceID, stats.signalDistance);

        polarCoordRenderer.SetPropertyBlock(polarCoordMPB);
        distanceCoordRenderer.SetPropertyBlock(distanceCoordMPB);

        cachedMicYaw = micPivotYawTransform.localEulerAngles.y;
        cachedMicPitch = micPivotPitchTransform.localEulerAngles.x;
    }
    private void OnEnable()
    {
        OnInspectDisengageBegin.RegisterListener(HandleInspectDisengage);
    }

    private void OnDisable()
    {
        OnInspectDisengageBegin.UnregisterListener(HandleInspectDisengage);
        
    }
    private void Update()
    {

        if (stats.engaged && !stats.recording)
        {

            if (playerInputData.input_move.x != 0)
            {
                stats.signalAngle = (stats.signalAngle - (Time.deltaTime * settings.sensitivity * playerInputData.input_move.x)) % 1;

                polarCoordMPB.SetFloat(materialIDs.signalAngleID, stats.signalAngle);
                polarCoordRenderer.SetPropertyBlock(polarCoordMPB);

                signalRenTexMat.SetFloat(materialIDs.signalAngleID, stats.signalAngle);

                float zEulerAngle = (1 - stats.signalAngle) * 360;
                polarCoordKnob.localEulerAngles = new Vector3(polarCoordKnob.localEulerAngles.x, polarCoordKnob.localEulerAngles.y, zEulerAngle);

                micPivotYawTransform.localEulerAngles = new Vector3(micPivotYawTransform.localEulerAngles.x, zEulerAngle + cachedMicYaw, micPivotYawTransform.localEulerAngles.z);
            }


            if (playerInputData.input_move.y != 0)
            {
                stats.signalDistance = Mathf.Clamp01(stats.signalDistance + (Time.deltaTime * settings.sensitivity * 2 * playerInputData.input_move.y));

                polarCoordMPB.SetFloat(materialIDs.signalDistanceID, stats.signalDistance);
                distanceCoordMPB.SetFloat(materialIDs.signalDistanceID, stats.signalDistance);

                polarCoordRenderer.SetPropertyBlock(polarCoordMPB);
                distanceCoordRenderer.SetPropertyBlock(distanceCoordMPB);

                signalRenTexMat.SetFloat(materialIDs.signalDistanceID, stats.signalDistance);


                float zEulerAngle = stats.signalDistance * 90;
                distanceCoordKnob.eulerAngles = new Vector3(distanceCoordKnob.eulerAngles.x, distanceCoordKnob.eulerAngles.y, zEulerAngle);

                float pitchEulerAngle = (1 - Mathf.Clamp01(stats.signalDistance - (Time.deltaTime * settings.sensitivity * 2 * -playerInputData.input_move.y))) * 90;
                micPivotPitchTransform.localEulerAngles = new Vector3(pitchEulerAngle + cachedMicPitch, micPivotPitchTransform.localEulerAngles.y, micPivotPitchTransform.localEulerAngles.z);
            }

            if (playerInputData.input_interact)
            {
                Record().Forget();
            }
        }
    }

    private async UniTaskVoid Record()
    {
        stats.recording = true;
        stats.recordLength = 0.0f;
        stats.recordLengthTime = signalRenTexMat.GetFloat(materialIDs.signalHorizontalSpeedID);
        while(stats.recordLength < 1)
        {
            stats.recordLength += (Time.deltaTime * stats.recordLengthTime);
            signalMPB.SetFloat(materialIDs.recordLengthID, stats.recordLength);
            signalRenderer.SetPropertyBlock(signalMPB);
            await UniTask.Yield();
        }

        Graphics.CopyTexture(signalRenderTexture, recordedSignalRenTex);
        recordedSignalMaterial.SetTexture(materialIDs.recordedSignalTextureID, recordedSignalRenTex);
        stats.recording = false;
        //SaveSignalTexture(signalRenderTexture);
    }

    private void SaveSignalTexture(RenderTexture rt)
    {
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D raw = new Texture2D(rt.width, rt.height, TextureFormat.RGFloat, false);
        raw.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        raw.Apply();
        RenderTexture.active = prev;

        Texture2D gTex = new Texture2D(rt.width, rt.height, TextureFormat.R8, false);

        Color[] pixels = raw.GetPixels();
        Color[] gPixels = new Color[pixels.Length];

        for (int i = 0; i < pixels.Length; i++)
        {
            float g = pixels[i].g;
            gPixels[i] = new Color(g, g, g);
        }

        gTex.SetPixels(gPixels);
        gTex.Apply();

        byte[] bytes = gTex.EncodeToPNG();

        string folderPath = "Assets/Textures/SignalTextures";
        if (!Directory.Exists(folderPath)) { Directory.CreateDirectory(folderPath); }


        string fileName = "SignalTexture.png";
        string fullPath = Path.Combine(folderPath, fileName);

        File.WriteAllBytes(fullPath, bytes);
        Debug.Log(Application.persistentDataPath);
        //UnityEditor.AssetDatabase.ImportAsset(fullPath);
    }

    public void HandleInspectEngage() { stats.engaged = true; }
    public void HandleInspectDisengage() { stats.engaged = false; }
}

