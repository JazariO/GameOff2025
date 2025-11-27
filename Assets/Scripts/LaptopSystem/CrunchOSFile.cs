using Proselyte.Sigils;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CrunchOSFile : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Serializable]
    public struct CrunchFileData
    {
        public bool isImage;
        public Image fileSelectionImage;
        public Image fileDisplayImage;
        public string timeStamp;
    } 
    public CrunchFileData crunchFileData;

    [SerializeField] GameEvent OnMediaExplorerFileSelect;
    [SerializeField] GameEvent OnMediaExplorerFileDeselectAll;

    [SerializeField] MediaExplorerDetailsPanelDataSO mediaExplorerDetailsPanelDataSO;
    private bool selected;

    private void OnEnable()
    {
        OnMediaExplorerFileDeselectAll.RegisterListener(DeselectFile);
    }

    private void OnDisable()
    {
        OnMediaExplorerFileDeselectAll.UnregisterListener(DeselectFile);
    }

    private void DeselectFile()
    {
        selected = false;
        crunchFileData.fileSelectionImage.color = new Color(0f, 0.5f, 1f, 0f);
    }

    void IPointerEnterHandler.OnPointerEnter(PointerEventData _)
    {
        if(!selected) crunchFileData.fileSelectionImage.color = new Color(0f, 0.5f, 1f, 1f);
        Debug.Log("Pointer Enter");
    }

    void IPointerExitHandler.OnPointerExit(PointerEventData _)
    {
        if(!selected) crunchFileData.fileSelectionImage.color = new Color(0f, 0.5f, 1f, 0f);
        Debug.Log("Pointer Exit");
    }

    void IPointerClickHandler.OnPointerClick(PointerEventData pointerEventData)
    {
        Debug.Log("Pointer Click");
        OnMediaExplorerFileDeselectAll.Raise();

        mediaExplorerDetailsPanelDataSO.crunch_OS_file_data = this.crunchFileData;
        // TODO(Jazz): add audio file data here for passing to media explorer

        crunchFileData.fileSelectionImage.color = Color.blue;
        selected = true;

        OnMediaExplorerFileSelect.Raise();
    }
}
