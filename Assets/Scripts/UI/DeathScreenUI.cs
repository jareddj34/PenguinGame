using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DeathScreenUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeInDuration = 1.5f;

    public GameObject content;

    private void Start()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        GameEvents.Instance.OnPlayerDied += StartFadeIn;

        content.SetActive(false);
    }

    private void OnDestroy()
    {
        GameEvents.Instance.OnPlayerDied -= StartFadeIn;
    }

    private void StartFadeIn()
    {
        content.SetActive(true);
        StartCoroutine(FadeIn());
    }

    private IEnumerator FadeIn()
    {
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
            yield return null;
        }

        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    // Wire these to your buttons in the Inspector
    public void OnRestartPressed()
    {
        Debug.Log("Restarting level...");

        // Destroy the persistent player so the starting scene gets a clean one
        PlayerPersistence player = FindFirstObjectByType<PlayerPersistence>();
        if (player != null) {

            PlayerSaveData.SaveFrom(
                player.GetComponent<PlayerAttack>(),
                player.GetComponent<PlayerShield>(),
                player.GetComponent<PlayerThrow>(),
                player.GetComponent<PlayerHealth>()
            );

            Destroy(player.gameObject);
        }

        AudioManager.Instance.StopMusic();

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnMainMenuPressed()
    {
        SceneManager.LoadScene("MainMenu"); // match your scene name
    }
}