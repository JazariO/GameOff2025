using Cysharp.Threading.Tasks;
using System;
using System.IO;
using UnityEngine;

public class AudioDevice : MonoBehaviour
{
    [SerializeField] AudioDeviceSO settings;

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
    }
    public Stats stats;

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

    private void Update()
    {
        if (Input.GetKey(KeyCode.A))
        {
            stats.signalAngle = (stats.signalAngle + (Time.deltaTime * settings.sensitivity)) % 1;
            polarCoordMPB.SetFloat(materialIDs.signalAngleID, stats.signalAngle);
            polarCoordRenderer.SetPropertyBlock(polarCoordMPB);

            float zEulerAngle = (1 - stats.signalAngle) * 360;
            polarCoordKnob.eulerAngles = new Vector3(polarCoordKnob.eulerAngles.x, polarCoordKnob.eulerAngles.y, zEulerAngle);
        }

        if (Input.GetKey(KeyCode.D))
        {
            stats.signalAngle = (stats.signalAngle - (Time.deltaTime * settings.sensitivity)) % 1;
            polarCoordMPB.SetFloat(materialIDs.signalAngleID, stats.signalAngle);
            polarCoordRenderer.SetPropertyBlock(polarCoordMPB);


            float zEulerAngle = (1 - stats.signalAngle) * 360;
            polarCoordKnob.eulerAngles = new Vector3(polarCoordKnob.eulerAngles.x, polarCoordKnob.eulerAngles.y, zEulerAngle);
        }

        if (Input.GetKey(KeyCode.W))
        {
            stats.signalDistance = Mathf.Clamp01(stats.signalDistance + (Time.deltaTime * settings.sensitivity * 2));
            polarCoordMPB.SetFloat(materialIDs.signalDistanceID, stats.signalDistance);
            distanceCoordMPB.SetFloat(materialIDs.signalDistanceID, stats.signalDistance);
            polarCoordRenderer.SetPropertyBlock(polarCoordMPB);
            distanceCoordRenderer.SetPropertyBlock(distanceCoordMPB);


            float zEulerAngle = stats.signalDistance * 360;
            distanceCoordKnob.eulerAngles = new Vector3(distanceCoordKnob.eulerAngles.x, distanceCoordKnob.eulerAngles.y, zEulerAngle);
        }

        if (Input.GetKey(KeyCode.S))
        {
            stats.signalDistance = Mathf.Clamp01(stats.signalDistance - (Time.deltaTime * settings.sensitivity * 2));
            polarCoordMPB.SetFloat(materialIDs.signalDistanceID, stats.signalDistance);
            distanceCoordMPB.SetFloat(materialIDs.signalDistanceID, stats.signalDistance);
            polarCoordRenderer.SetPropertyBlock(polarCoordMPB);
            distanceCoordRenderer.SetPropertyBlock(distanceCoordMPB);


            float zEulerAngle = stats.signalDistance * 360;
            distanceCoordKnob.eulerAngles = new Vector3(distanceCoordKnob.eulerAngles.x, distanceCoordKnob.eulerAngles.y, zEulerAngle);
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Record().Forget();
        }
    }

    private async UniTaskVoid Record()
    {
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

        SaveSignalTexture(signalRenderTexture);
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
}

