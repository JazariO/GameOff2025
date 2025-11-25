using Proselyte.Sigils;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] GameEvent OnInspectionEngageBegin;
    [SerializeField] GameEvent OnInspectionDisengageEnd;

    [SerializeField] Image first_person_cursor_image;

    private void OnEnable()
    {
        OnInspectionEngageBegin.RegisterListener(HandleInspectionEngage);
        OnInspectionDisengageEnd.RegisterListener(HandleInspectionDisengage);
    }

    private void OnDisable()
    {
        OnInspectionEngageBegin.UnregisterListener(HandleInspectionEngage);
        OnInspectionDisengageEnd.UnregisterListener(HandleInspectionDisengage);
    }

    public void HandleInspectionEngage()
    {
        first_person_cursor_image.enabled = false;
    }

    public void HandleInspectionDisengage()
    {
        first_person_cursor_image.enabled = true;
    }
}
