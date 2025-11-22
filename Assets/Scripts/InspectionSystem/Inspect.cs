using DelaysExpected.RuntimeUtilities;
using Proselyte.Sigils;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEditor;
#if UNITY_EDITOR
using UnityEditor.Events;
#endif

[RequireComponent(typeof(Interactable))]
public class Inspect : MonoBehaviour
{
    [SerializeField] Transform inspectionTransform;
    [SerializeField] float transitionTime = 1f;

    [Space]
    [SerializeField] GameEvent OnInspectEngage;
    [SerializeField] GameEvent OnInspect;
    [SerializeField] GameEvent OnInspectDisengage;
    [SerializeField] PlayerInputDataSO playerInputDataSO;
    [SerializeField] PlayerSaveDataSO playerSaveDataSO;

    private Vector3 _cameraStandPos;
    private Quaternion _cameraStandRot;

    private bool _transitioning;

    public void EngageInspection()
    {
        Interactable interactable = GetComponent<Interactable>();
        interactable.ClearDisplayMessage();
        OnInspectEngage.Raise();

        if(!_transitioning)
        {
            _transitioning = true;

            // Cache player camera position & rotation before inspecting.
            _cameraStandPos = Camera.main.transform.position;
            _cameraStandRot = Camera.main.transform.rotation;

            StartCoroutine(SmoothInspect());
        }
    }

    public void DisengageInspection()
    {
        if(!_transitioning)
        {
            _transitioning = true;
            StartCoroutine(SmoothStand());
        }
    }

    private IEnumerator SmoothInspect()
    {
        playerSaveDataSO.isInspecting = true;

        float ctr = 0;

        while(ctr < transitionTime)
        {
            ctr += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, ctr / transitionTime);

            Camera.main.transform.SetPositionAndRotation(
                Vector3.Lerp(_cameraStandPos, inspectionTransform.position, t), 
                Quaternion.Lerp(_cameraStandRot, inspectionTransform.rotation, t));

            yield return null;
        }

        SetInspect();
    }

    private void SetInspect()
    {
        // Snap camera to position and rotation
        Camera.main.transform.SetPositionAndRotation(inspectionTransform.position, inspectionTransform.rotation);

        _transitioning = false;
        OnInspect.Raise();
    }

    private IEnumerator SmoothStand()
    {
        float ctr = 0;
        while(ctr < transitionTime)
        {
            ctr += Time.deltaTime;

            float t = Mathf.SmoothStep(0, 1, ctr / transitionTime);

            Camera.main.transform.SetPositionAndRotation(
                Vector3.Lerp(inspectionTransform.position, _cameraStandPos, t), 
                Quaternion.Lerp(inspectionTransform.rotation, _cameraStandRot, t));

            yield return null;
        }

        SetStand();
    }

    private void SetStand()
    {
        Interactable interactable = GetComponent<Interactable>();
        interactable.ClearDisplayMessage();

        // Ensure camera is exactly at the standing position and rotation
        Camera.main.transform.SetPositionAndRotation(_cameraStandPos, _cameraStandRot);
        playerSaveDataSO.isInspecting = false;

        _transitioning = false;
        OnInspectDisengage.Raise();
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if(inspectionTransform != null)
        {
            // Draw inspection pivot axes
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(
                inspectionTransform.position,
                inspectionTransform.position + (inspectionTransform.forward * 0.2f));

            Gizmos.color = Color.green;
            Gizmos.DrawLine(
                inspectionTransform.position,
                inspectionTransform.position + (inspectionTransform.up * 0.2f));
        }
    }
#endif
}
