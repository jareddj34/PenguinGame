using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class WelcomePanel : MonoBehaviour
{

    public GameObject[] objectsToDisable;

    public UnityEvent OnBeginDemo;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    IEnumerator Start()
    {
        yield return new WaitForSeconds(0.1f); // Wait a moment to ensure all objects are initialized
        FreezePlayer();
    }

    void FreezePlayer()
    {
        GameEvents.Instance?.FreezePlayer();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void BeginDemo()
    {
        GameEvents.Instance?.UnfreezePlayer();
        OnBeginDemo?.Invoke();
        foreach (GameObject obj in objectsToDisable)
        {
            obj.SetActive(false);
        }
    }
}
