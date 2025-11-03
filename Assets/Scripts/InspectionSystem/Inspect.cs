using CMF;
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
    [SerializeField] GameEvent OnInspectionStandEvent;
    [SerializeField] BoolReference interactInput;

    private GameObject _player;
    private Camera _playerCamera;
    private AdvancedWalkerController _playerController;
    private CameraController _cameraController;
    private Rigidbody _playerRigidbody;

    private Vector3 _cameraStandPos;
    private Quaternion _cameraStandRot;

    private bool _transitioning;
    private bool _playerSittingState;

    private static Inspect _currentInspectScript;

    private bool _editorInitialized;

#if UNITY_EDITOR
    private void Reset()
    {
        if(!_editorInitialized)
        {
            // Set default distance away from inspectable
            float yOffset = .35f;

            // Instantiate an empty GameObject named "InspectionPivot"
            GameObject inspectPoint = new GameObject("InspectionPivot");

            // Register the creation of the GameObject for undo
            Undo.RegisterCreatedObjectUndo(inspectPoint, "Create InspectionPivot");

            // Set the inspection point as a child of the current GameObject and register this change
            Undo.SetTransformParent(inspectPoint.transform, transform, "Set InspectionPivot Parent");

            // Set the inspection point's default local position and rotation;
            inspectPoint.transform.SetLocalPositionAndRotation(
                new Vector3(0, yOffset, 0),
                Quaternion.Euler(90, 0, 0));

            // Assign the inspection point as the inspectionTransform
            inspectionTransform = inspectPoint.transform;

            // Initialize the Interactable component
            Interactable interactable = GetComponent<Interactable>();
            interactable.ChangeDisplayMessage("Read");

            // Check/Add the gameObject's collider which is required to interact with it.
            Collider collider = GetComponent<Collider>();
            if(collider == null)
                collider = gameObject.AddComponent<BoxCollider>();

            // gameobject.layer == 'Default'
            if(gameObject.layer == 0)
            {
                // GameObject has 'Default' layer, set the layer to 'Interactable'
                gameObject.layer = LayerMask.NameToLayer("Interactable");
            }

            // Add blue diamond (dot9) view icon to GameObject
            EventTools.AddSceneViewIcon(inspectPoint, "sv_icon_dot9_sml");

            // Add inspection pivot to the inspection point
            inspectPoint.AddComponent<InspectionPivot>();

            // Add the EngageInspection method as a persistent listener if not already added
            UnityAction engageInspectionAction = EngageInspection;
            if(!EventTools.IsMethodAlreadySubscribed(interactable.onInteraction, engageInspectionAction))
            {
                // Subscribe the EngageInspection method
                UnityEventTools.AddPersistentListener(interactable.onInteraction, engageInspectionAction);
            }

            // Define action for changing the display message on the interactable component
            UnityAction clearDisplayMessageAction = interactable.ClearDisplayMessage;

            // Check if ChangeDisplayMessage is already subscribed
            if(!EventTools.IsMethodAlreadySubscribed(interactable.onInteraction,
                    clearDisplayMessageAction))
            {
                // Clear Display Message on inspection
                UnityEventTools.AddPersistentListener(interactable.onInteraction,
                    clearDisplayMessageAction);
            }

            // Mark the editor as initialized to prevent this from running multiple times
            _editorInitialized = true;
        }
    }
#endif

    private void Start()
    {
        FindPlayer();
    }

    private void Update()
    {
        // NOTE(Jazz): Check for inspection disengage
        if(_playerController.IsInspecting &&
            _currentInspectScript == this &&
            interactInput.Value)
        {
            if(!_transitioning && FindPlayer())
            {
                _transitioning = true;
                StartCoroutine(SmoothStand());
            }
        }
    }

    private bool FindPlayer()
    {
        _player = GameObject.FindGameObjectWithTag("Player");
        if(_player == null)
        {
            Debug.LogWarning("Player GameObject with tag 'Player' not found.");
            return false;
        }

        if(!_player.TryGetComponent(out _playerController))
        {
            Debug.LogWarning("Player GameObject with tag 'Player' missing " +
                "JustinController Component.");
            return false;
        }

        _playerCamera = _player.GetComponentInChildren<Camera>();
        if(_playerCamera == null)
        {
            Debug.LogWarning("Player GameObject with tag 'Player' missing Camera " +
                "Component in children.");
            return false;
        }

        _cameraController = _player.GetComponentInChildren<CameraController>();
        if(_playerCamera == null)
        {
            Debug.LogWarning("Player GameObject with tag 'Player' missing CameraMouseInput " +
                "Component in children.");
            return false;
        }

        if(!_player.TryGetComponent(out _playerRigidbody))
        {
            Debug.LogWarning("Player GameObject with tag 'Player' missing " +
                "Rigidbody Component.");
            return false;
        }
        else
            return true;
    }

    public void EngageInspection()
    {
        Interactable interactable = GetComponent<Interactable>();
        interactable.ClearDisplayMessage();
        OnInspectionStartEvent.Raise();

        if(!_transitioning && FindPlayer() && !_playerController.IsInspecting)
        {
            // Hide cursor reticle
            //playerController.reticleImage.enabled = false; // HACK, no reticle yet

            // Clear player stand prompt
            if(_playerController.IsSitting)
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
        _cameraController.enabled = false;
        _playerController.canMove = false;

        // Set player controller sitting & inspecting state
        _playerSittingState = _playerController.IsSitting;
        _playerController.SetInspectionState(true);

        // Disable player controller rigidbody physics
        _playerRigidbody.isKinematic = true;

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

        _playerController.SetInspectionState(true);
        _transitioning = false;
        OnInspectEvent.Raise();
    }

    private IEnumerator SmoothStand()
    {
        // Disable player input controls.
        _cameraController.enabled = false;
        _playerController.canMove = false;

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
        _cameraController.enabled = true;
        _playerController.canMove = true;

        // Return player controller sitting state.
        _playerController.SetSittingState(_playerSittingState);

        // Set player controller inspection off.
        _playerController.SetInspectionState(false);

        // Return player controller rigidbody physics if they weren't sitting
        // before inspecting
        _playerRigidbody.isKinematic = _playerSittingState;

        _transitioning = false;
        _currentInspectScript = null;
        OnInspectionStandEvent.Raise();

        // Show cursor reticle
        //playerController.reticleImage.enabled = true;

        if(_playerController.IsSitting)
        {
            // Player was sitting before starting the inspection, return the prompt to stand
            //playerController.standPromptTMP.text = playerController.standPrompt;
            _playerController.SetSittingState(true);
        }
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
