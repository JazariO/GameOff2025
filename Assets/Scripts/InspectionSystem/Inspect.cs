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
    [SerializeField] GameEvent OnInspectionStartEvent;
    [SerializeField] GameEvent OnInspectEvent;
    [SerializeField] GameEvent OnInspectionEndEvent;
    [SerializeField] BoolReference interactInput;

    private Camera _playerCamera;

    private Vector3 _cameraStandPos;
    private Quaternion _cameraStandRot;

    private bool _transitioning;
    private bool _playerSittingState;

    private static Inspect _currentInspectScript;

    private void Update()
    {
        // NOTE(Jazz): Check for inspection disengage
        if(//_playerController.IsInspecting &&
            _currentInspectScript == this &&
            interactInput.Value)
        {
            if(!_transitioning)
            {
                _transitioning = true;
                StartCoroutine(SmoothStand());
            }
        }
    }
    public void EngageInspection()
    {
        Interactable interactable = GetComponent<Interactable>();
        interactable.ClearDisplayMessage();
        OnInspectionStartEvent.Raise();

        //if(!_transitioning && FindPlayer() && !_playerController.IsInspecting)
        {
            // Hide cursor reticle
            //playerController.reticleImage.enabled = false; // HACK, no reticle yet

            // Clear player stand prompt
            //if(_playerController.IsSitting)
            {
                //playerController.standPromptTMP.text = string.Empty; // HACK, no stand prompt yet
            }

            _transitioning = true;
            _currentInspectScript = this;

            // Cache player camera position & rotation before inspecting.
            _cameraStandPos = _playerCamera.transform.position;
            _cameraStandRot = _playerCamera.transform.rotation;

            StartCoroutine(SmoothInspect());
        }
    }

    private IEnumerator SmoothInspect()
    {

        // Disable player input controls.
        //_cameraController.enabled = false;
        //_playerController.canMove = false;

        //// Set player controller sitting & inspecting state
        //_playerSittingState = _playerController.IsSitting;
        //_playerController.SetInspectionState(true);

        float ctr = 0;

        while(ctr < transitionTime)
        {
            ctr += Time.deltaTime;

            float t = Mathf.SmoothStep(0, 1, ctr / transitionTime);

            _playerCamera.transform.position = Vector3.Lerp(
                _cameraStandPos,
                inspectionTransform.
                position,
                t);

            _playerCamera.transform.rotation = Quaternion.Lerp(
                _cameraStandRot,
                inspectionTransform.rotation,
                t);

            yield return null;
        }

        SetInspect();
    }

    private void SetInspect()
    {
        // Ensure camera is exactly at the inspection position and rotation
        _playerCamera.transform.position = inspectionTransform.position;
        _playerCamera.transform.rotation = inspectionTransform.rotation;

        //_playerController.SetInspectionState(true);
        _transitioning = false;
        OnInspectEvent.Raise();
    }

    private IEnumerator SmoothStand()
    {
        // Disable player input controls.
        //_cameraController.enabled = false;
        //_playerController.canMove = false;

        float ctr = 0;
        while(ctr < transitionTime)
        {
            ctr += Time.deltaTime;

            float t = Mathf.SmoothStep(0, 1, ctr / transitionTime);

            _playerCamera.transform.position = Vector3.Lerp(
                inspectionTransform.position,
                _cameraStandPos,
                t);
            _playerCamera.transform.rotation = Quaternion.Lerp(
                inspectionTransform.rotation,
                _cameraStandRot,
                t);

            yield return null;
        }

        SetStand();
    }

    private void SetStand()
    {
        Interactable interactable = GetComponent<Interactable>();
        interactable.ClearDisplayMessage();

        // Ensure camera is exactly at the standing position and rotation
        _playerCamera.transform.position = _cameraStandPos;
        _playerCamera.transform.rotation = _cameraStandRot;

        // Return player controller input control.
        //_cameraController.enabled = true;
        //_playerController.canMove = true;

        //// Return player controller sitting state.
        //_playerController.SetSittingState(_playerSittingState);

        //// Set player controller inspection off.
        //_playerController.SetInspectionState(false);

        _transitioning = false;
        _currentInspectScript = null;
        OnInspectionEndEvent.Raise();

        // Show cursor reticle
        //playerController.reticleImage.enabled = true;

        //if(_playerController.IsSitting)
        //{
        //    // Player was sitting before starting the inspection, return the prompt to stand
        //    //playerController.standPromptTMP.text = playerController.standPrompt;
        //    _playerController.SetSittingState(true);
        //}
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if(inspectionTransform != null)
        {
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
