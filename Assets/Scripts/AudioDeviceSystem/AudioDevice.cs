using System;
using UnityEngine;

public class AudioDevice : MonoBehaviour
{
    [SerializeField] AudioDeviceSettingsSO settings;

    [SerializeField] Renderer polarCoordRenderer;
    [SerializeField] Renderer distanceCoordRenderer;

    private MaterialPropertyBlock polarCoordMPB;
    private MaterialPropertyBlock distanceCoordMPB;

    [SerializeField] Transform polarCoordKnob;
    [SerializeField] Transform distanceCoordKnob;
    [Serializable] struct MaterialIDs
    {
        internal int signalAngleID;
        internal int signalDistanceID;
    }
    MaterialIDs materialIDs;

    [Serializable] public struct Stats
    {
        internal float signalAngle;
        internal float signalDistance;
    }
    public Stats stats;

    private void Awake()
    {
        materialIDs.signalAngleID = Shader.PropertyToID("_SignalAngle");
        materialIDs.signalDistanceID = Shader.PropertyToID("_SignalDistance");

        polarCoordMPB = new MaterialPropertyBlock();
        distanceCoordMPB = new MaterialPropertyBlock();
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
    }
}
