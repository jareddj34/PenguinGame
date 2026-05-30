using UnityEngine;
using TMPro;
using System.Collections;

public class InteractionPrompt : MonoBehaviour
{
    [SerializeField] private GameObject promptPanel;
    [SerializeField] private TextMeshProUGUI promptText;

    public Animator animator;

    private Coroutine _hideCoroutine;

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
        // Cancel any pending hide so it doesn't deactivate the panel after we re-show
        if (_hideCoroutine != null)
        {
            StopCoroutine(_hideCoroutine);
            _hideCoroutine = null;
        }

        promptPanel.SetActive(true);
        animator.ResetTrigger("End"); // Clear any stale End trigger that may have queued
        animator.SetTrigger("Start");
        promptText.text = prompt;
    }

    private void Hide()
    {
        animator.ResetTrigger("Start"); // Clear any stale Start trigger that may have queued
        animator.SetTrigger("End");
        _hideCoroutine = StartCoroutine(HideAfterAnimation());
    }

    IEnumerator HideAfterAnimation()
    {
        yield return new WaitForSeconds(0.1f); // Match this to your close animation length
        promptPanel.SetActive(false);
        _hideCoroutine = null;
    }
}
