using UnityEngine;
using TMPro;

public class InteractionPrompt : MonoBehaviour
{
    [SerializeField] private GameObject promptPanel;
    [SerializeField] private TextMeshProUGUI promptText;

    private void Start()
    {
        GameEvents.Instance.OnPromptShow += Show;
        GameEvents.Instance.OnPromptHide += Hide;
    }

    private void OnDisable()
    {
        GameEvents.Instance.OnPromptShow -= Show;
        GameEvents.Instance.OnPromptHide -= Hide;
    }

    private void Show(string prompt)
    {
        promptPanel.SetActive(true);
        promptText.text = prompt;
    }

    private void Hide()
    {
        promptPanel.SetActive(false);
    }
}
