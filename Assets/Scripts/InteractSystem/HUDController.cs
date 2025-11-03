using UnityEngine;
using TMPro;

public class HUDController : MonoBehaviour
{
    public static HUDController instance;

    private void Awake()
    {
        instance = this;
    }

    //gets the text from the interactable
    [HideInInspector] public TMP_Text interactionText;

    public void EnableInteractionText(string text)
    {
        interactionText.text = text;
        interactionText.gameObject.SetActive(true);
    }

    public void DisableInteractionText()
    {
        interactionText.gameObject.SetActive(false);
    }
}
