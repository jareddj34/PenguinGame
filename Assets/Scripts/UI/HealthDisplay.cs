using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthDisplay : MonoBehaviour
{
    [Header("Heart Sprites")]
    public Sprite emptyHeart;
    public Sprite halfHeart;
    public Sprite fullHeart;

    [Header("Heart UI")]
    [Tooltip("A UI Image prefab used as the template for each heart slot.")]
    public Image heartPrefab;
    public Transform heartContainer;

    private PlayerHealth playerHealth;
    private readonly List<Image> heartImages = new List<Image>();

    void Start()
    {
        Rewire();
    }

    public void Rewire()
    {
        if(playerHealth != null)
        {
            playerHealth.OnHealthChanged -= OnHealthChanged;
        }
        
        playerHealth = FindFirstObjectByType<PlayerHealth>();
        if(playerHealth == null) return;

        playerHealth.OnHealthChanged += OnHealthChanged;
        BuildHearts();
        UpdateHearts();
    }

    void OnDestroy()
    {
        if (playerHealth != null)
            playerHealth.OnHealthChanged -= OnHealthChanged;
    }

    // Called whenever currentHealth or maxHealth changes.
    void OnHealthChanged(float current, float max)
    {
        // Rebuild the row if the number of heart containers changed (e.g. after AddHeartContainer).
        if (heartImages.Count != playerHealth.maxHeartContainers)
            BuildHearts();

        UpdateHearts();
    }

    // Instantiates one Image per heart container under heartContainer.
    void BuildHearts()
    {
        foreach (var img in heartImages)
            if (img != null) Destroy(img.gameObject);
        heartImages.Clear();

        if (heartPrefab == null || heartContainer == null)
        {
            Debug.LogError("HealthDisplay: heartPrefab or heartContainer is not assigned.");
            return;
        }

        for (int i = 0; i < playerHealth.maxHeartContainers; i++)
        {
            Image heart = Instantiate(heartPrefab, heartContainer);
            heart.gameObject.SetActive(true);
            heartImages.Add(heart);
        }
    }

    // Sets each heart's sprite based on current HP.
    void UpdateHearts()
    {
        float hp = playerHealth.currentHealth;
        float hpc = PlayerHealth.HealthPerContainer;

        for (int i = 0; i < heartImages.Count; i++)
        {
            float slotMin = i * hpc;
            float slotMax = (i + 1) * hpc;

            if (hp >= slotMax)
                heartImages[i].sprite = fullHeart;
            else if (hp > slotMin)
                heartImages[i].sprite = halfHeart;
            else
                heartImages[i].sprite = emptyHeart;
        }
    }
}
