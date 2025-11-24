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
    [SerializeField] GameEvent OnInspectEngageBegin;
    [SerializeField] GameEvent OnInspectEngageEnd;
    [SerializeField] GameEvent OnInspectDisengageBegin;
    [SerializeField] GameEvent OnInspectDisengageEnd;
    [SerializeField] PlayerInputDataSO playerInputDataSO;
    [SerializeField] PlayerSaveDataSO playerSaveDataSO;

    private Vector3 _cameraStandPos;
    private Quaternion _cameraStandRot;
    private Transform _cameraParentPivot;

    private bool _transitioning;

    public void EngageInspection()
    {
        // Cache camera parent transform before detaching
        _cameraParentPivot = Camera.main.transform.parent;

        Interactable interactable = GetComponent<Interactable>();
        interactable.ClearDisplayMessage();
        if(OnInspectEngageBegin != null) OnInspectEngageBegin.Raise();

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
            if(OnInspectDisengageBegin != null) OnInspectDisengageBegin.Raise();
            // Parent camera to its cached parent transform (likely the player controller, again)
            Camera.main.transform.SetParent(_cameraParentPivot, true);

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

        // Parent camera to inspection pivot (for moving the camera relative to the inspection pivot)
        Camera.main.transform.SetParent(inspectionTransform, true);

        _transitioning = false;
        if(OnInspectEngageEnd != null) OnInspectEngageEnd.Raise();
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
        if(OnInspectDisengageEnd != null) OnInspectDisengageEnd.Raise();
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
