using UnityEngine;

public class AudioDeviceBirdDetector : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.layer == LayerMask.NameToLayer("Bird"))
        {
            Debug.Log("bird trigger enter");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if(other.gameObject.layer == LayerMask.NameToLayer("Bird"))
        {
            Debug.Log("bird trigger exit");
        }
    }
}
