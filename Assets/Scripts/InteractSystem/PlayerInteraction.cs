using UnityEngine;
using Proselyte.Sigils;

public class PlayerInteraction : MonoBehaviour
{
    public float playerReach = 2f;
    Interactable currentInteractable;
    [HideInInspector] public bool hit = false;
    [HideInInspector] public GameObject currentObject;
    [SerializeField] LayerMask interactionLayerMask;

    [Header("Incoming References")]
    [SerializeField] BoolReference interactInput;

    [Header("Outgoing Variables")]
    [SerializeField] StringVariable interactText;
    [SerializeField] BoolVariable cursorFilled;

    [Header("Game Events SEND")]
    [SerializeField] GameEvent OnHoverEnterInteractable;
    [SerializeField] GameEvent OnHoverExitInteractable;

    private Renderer currentRenderer;
    private uint currentRenderingLayer;
    private bool init;
    private Interactable previousInteractable;

    private void Awake()
    {
        interactText.value = string.Empty;
    }

    void Update()
    {
        CheckInteraction();
        if(interactInput.Value && currentInteractable != null)
        {
            Debug.Log("Attempting Interaction");
            currentInteractable.Interact();
        }
    }

    void CheckInteraction()
    {
        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);

        // Use the interactionLayerMask to only hit objects in the specified layers
        if(Physics.Raycast(ray, out RaycastHit hit, playerReach, interactionLayerMask))
        {
            Interactable newInteractable = hit.collider.GetComponent<Interactable>();

            if(newInteractable == null || !newInteractable.enabled)
            {
                DisableCurrentInteractable();
                return;
            }

            currentObject = hit.transform.gameObject;

            if(newInteractable != currentInteractable)
            {
                DisableCurrentInteractable(); // Exit old one
                SetNewCurrentInteractable(newInteractable); // Enter new one
            }
            else
            {
                // Still hovering the same object, do nothing
            }
        }
        else
        {
            DisableCurrentInteractable();
        }
    }

    void SetNewCurrentInteractable(Interactable newInteractable)
    {
        hit = true;
        currentInteractable = newInteractable;
        previousInteractable = newInteractable;

        interactText.value = currentInteractable.displayMessage;
        cursorFilled.value = true;
        OnHoverEnterInteractable.Raise();
        
        currentRenderer = currentInteractable.GetComponent<Renderer>();
        if(currentRenderer == null) return;
        if(!currentInteractable.showOutline) return; // Exit early if not rendering outline
        
        if(!init) // Cache default rendering layer mask when setting interactable on rollover;
        {
            currentRenderingLayer = currentRenderer.renderingLayerMask;
            init = true;
        }

        currentRenderer.renderingLayerMask |= RenderingLayerMask.GetMask("Outline_1");
    }

    void DisableCurrentInteractable()
    {
        if(currentInteractable == null) return;

        interactText.value = string.Empty;
        cursorFilled.value = false;
        OnHoverExitInteractable.Raise();

        if(currentRenderer != null)
        {
            if(currentInteractable.showOutline)
                currentRenderer.renderingLayerMask = currentRenderingLayer; // reset renderer layermask
            
            currentRenderer = null;
        }

        hit = false;
        init = false;
        currentInteractable = null;
        previousInteractable = null;
    }
}
