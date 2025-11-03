using UnityEngine;
using UnityEngine.Events;
using CMF;

public class TriggerInteraction : MonoBehaviour
{
    public UnityEvent triggerEvent;
    [SerializeField] private bool turnOffAfterTrigger = true;

    private bool deactivated = false;

    private void Start()
    {
        //used for exposing checkbox
    }

    private void OnTriggerStay(Collider other)
    {
        AdvancedWalkerController playerWalker = other.GetComponent<AdvancedWalkerController>();
        if (!deactivated && other.CompareTag("Player") && playerWalker != null)
        {
            if (turnOffAfterTrigger)
            {
                deactivated = true;
            }
            triggerEvent?.Invoke();
        }
    }

}