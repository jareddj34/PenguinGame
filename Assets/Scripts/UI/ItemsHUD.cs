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
        Rewire();
    }

    void OnDestroy()
    {
        if(playerThrow != null)
        {
            playerThrow.OnAmmoChanged -= UpdateSnowballAmmo;
        }
    }

    public void Rewire()
    {
        if(playerThrow != null)
        {
            playerThrow.OnAmmoChanged -= UpdateSnowballAmmo;
        }

        playerThrow = FindFirstObjectByType<PlayerThrow>();
        
        if(playerThrow != null)
        {
            playerThrow.OnAmmoChanged += UpdateSnowballAmmo;
        }
        bool showSnowballs = playerThrow != null && playerThrow.gotSnowballs;
        snowballIcon.SetActive(showSnowballs);
        if(showSnowballs)
        {
            UpdateSnowballAmmo(playerThrow.snowballCount);
        }

        var attack = FindFirstObjectByType<PlayerAttack>();
        var shield = FindFirstObjectByType<PlayerShield>();
        swordIcon.SetActive(attack != null && attack.hasSword);
        shieldIcon.SetActive(shield != null && shield.hasShield);


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
