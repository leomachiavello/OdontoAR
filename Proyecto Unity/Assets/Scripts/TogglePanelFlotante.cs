using UnityEngine;

public class TogglePanelFlotante : MonoBehaviour
{
    [SerializeField] private CanvasGroup panelFlotante;

    private bool visible = true;

    public void AlternarPanel()
    {
        visible = !visible;
        panelFlotante.alpha = visible ? 1f : 0f;
        panelFlotante.interactable = visible;
        panelFlotante.blocksRaycasts = visible;
    }
}
