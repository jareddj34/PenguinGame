using UnityEngine;
using TMPro;

public class ItemsHUD : MonoBehaviour
{
    public GameObject swordIcon;
    public GameObject shieldIcon;
    public GameObject snowballIcon;
    public TextMeshProUGUI snowballAmmoText;

    private PlayerThrow playerThrow;

    void Start()
    {
        swordIcon.SetActive(false);
        shieldIcon.SetActive(false);
        snowballIcon.SetActive(false);

        playerThrow = FindFirstObjectByType<PlayerThrow>();
        if (playerThrow != null)
            playerThrow.OnAmmoChanged += UpdateSnowballAmmo;
    }

    public void ShowSword()
    {
        swordIcon.SetActive(true);
    }

    public void ShowShield()
    {
        shieldIcon.SetActive(true);
    }

    public void ShowSnowball()
    {
        snowballIcon.SetActive(true);
    }

    public void UpdateSnowballAmmo(int ammo)
    {
        snowballAmmoText.text = ammo.ToString();
    }
}
