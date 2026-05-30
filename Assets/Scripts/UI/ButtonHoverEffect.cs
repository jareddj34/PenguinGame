using UnityEngine;
using System.Collections;

public class ButtonHoverEffect : MonoBehaviour
{
    [Header("Scale Settings")]
    [SerializeField] private float hoveredScale = 1.1f;
    [SerializeField] private float scaleDuration = 0.15f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip hoverSound;

    private Vector3 originalScale;
    private Coroutine scaleCoroutine;

    private void Start()
    {
        originalScale = transform.localScale;
    }

    public void OnHoverEnter()
    {
        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
        scaleCoroutine = StartCoroutine(ScaleTo(originalScale * hoveredScale));

        // if (audioSource != null && hoverSound != null)
        //     audioSource.PlayOneShot(hoverSound);
    }

    public void OnHoverExit()
    {
        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
        scaleCoroutine = StartCoroutine(ScaleTo(originalScale));
    }

    private IEnumerator ScaleTo(Vector3 targetScale)
    {
        Vector3 startScale = transform.localScale;
        float elapsed = 0f;

        while (elapsed < scaleDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            transform.localScale = Vector3.Lerp(startScale, targetScale, elapsed / scaleDuration);
            yield return null;
        }

        transform.localScale = targetScale;
    }
}