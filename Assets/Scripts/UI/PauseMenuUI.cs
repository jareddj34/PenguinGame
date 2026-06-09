using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseMenuUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeDuration = 0.15f;

    private Coroutine fadeCoroutine;

    private void OnEnable()
    {
        GameStateManager.OnStateChanged += OnStateChanged;
    }

    private void OnDisable()
    {
        GameStateManager.OnStateChanged -= OnStateChanged;
    }

    private void Start()
    {
        // Start hidden
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void Update()
    {
        // Only allow pausing/unpausing from Normal or Paused states
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
            GameStateManager.Instance?.TogglePause();
    }

    private void OnStateChanged(GameState state)
    {
        if (state == GameState.Paused)
            Show();
        else
            Hide();
    }

    private void Show()
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(Fade(1f));
    }

    private void Hide()
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(Fade(0f));
    }

    private IEnumerator Fade(float target)
    {
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        float start = canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime; // unscaled — timeScale is 0 while paused
            canvasGroup.alpha = Mathf.Lerp(start, target, elapsed / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = target;

        if (target >= 1f)
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
    }



    public void OnResumePressed()
    {
        GameStateManager.Instance?.TogglePause();
    }

    public void OnMainMenuPressed()
    {
        Time.timeScale = 1f; // restore before scene load
        SceneManager.LoadScene("MainMenu");
    }

    public void OnQuitPressed()
    {
        Time.timeScale = 1f;
        Application.Quit();
    }
}
