using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class HUDManager : MonoBehaviour
{
    private HealthDisplay healthDisplay;
    private ItemsHUD itemsHUD;

    void Awake() {
        if (FindObjectsOfType<HUDManager>().Length > 1) {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);

        healthDisplay = GetComponentInChildren<HealthDisplay>();
        itemsHUD = GetComponentInChildren<ItemsHUD>();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        Rewire();
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(RewireNextFrame());
    }

    IEnumerator RewireNextFrame()
    {
        yield return null;
        Rewire();
    }

    void Rewire()
    {
        healthDisplay.Rewire();
        itemsHUD.Rewire();
    }
}
