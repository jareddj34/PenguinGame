using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class AudioManager : MonoBehaviour
{

    public static AudioManager Instance { get; private set; }

    [Header("Music")]
    public AudioSource musicSource;
    public AudioClip mainTheme;
    public AudioClip interior;
    public AudioClip gameOverTheme;
    private float originalMusicVolume;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += PlayTheme;

        originalMusicVolume = musicSource.volume;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= PlayTheme;
    }

    // Can use scene data to play certain music
    public void PlayTheme(Scene scene, LoadSceneMode mode)
    {
        switch(scene.name)
        {
            case "SampleScene":
                musicSource.clip = mainTheme;
                musicSource.Play();
                break;
            
            case "Interior":
                musicSource.clip = interior;
                musicSource.Play();
                break;

            default:
                musicSource.clip = mainTheme;
                musicSource.Play();
                break;

        }


    }


    public void PlayGameOverMusic()
    {
        if (musicSource != null && gameOverTheme != null)
        {
            musicSource.clip = gameOverTheme;
            musicSource.Play();
        }
    }

    public void StopMusic(){
        musicSource.Stop();
    }

    public void ReduceMusicVolume()
    {
        StartCoroutine(ChangeMusicVolumeCoroutine(.2f, .2f));
    }

    public void RestoreMusicVolume()
    {
        StartCoroutine(ChangeMusicVolumeCoroutine(.2f, originalMusicVolume));
    }

    IEnumerator ChangeMusicVolumeCoroutine(float duration, float targetVolume)
    {
        float currentTime = 0;
        float startVolume = musicSource.volume;

        while(currentTime < duration)
        {
            currentTime += Time.deltaTime;

            musicSource.volume = Mathf.Lerp(startVolume, targetVolume, currentTime / duration);
            yield return null;
        }

        musicSource.volume = targetVolume;
    }
}
