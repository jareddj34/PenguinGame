using UnityEngine;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    
    
    public void OnStartGamePressed()
    {
        Debug.Log("Starting game...");
        SceneManager.LoadScene("SampleScene");
    }

    public void OnQuitPressed()
    {
        Debug.Log("Quitting game...");
        Application.Quit();
    }
}
