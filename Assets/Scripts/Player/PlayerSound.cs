using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerSound : MonoBehaviour
{

    [Header("Audio Sources")]
    public AudioSource footstepAudioSource;
    public AudioSource actionAudioSource;
    public AudioSource hurtAudioSource;
    public AudioSource pickupAudioSource;

    [Header("Snow Footstep Sounds")]
    public List<AudioClip> snowFS;
    [Header("Ice Footstep Sounds")]
    public List<AudioClip> iceFS;
    [Header("Grass Footstep Sounds")]
    public List<AudioClip> grassFS;
    [Header("Ground Detection")]
    public LayerMask groundLayer;

    [Header("Action Sounds")]
    public AudioClip swordSwingSound;
    public AudioClip swordHardHitSound;
    public AudioClip swordFleshHitSound;
    public AudioClip shieldUpSound;
    public AudioClip shieldBlockSound;
    public AudioClip gruntSound;

    [Header("Hurt Sounds")]
    public AudioClip hurtSound;
    public AudioClip hitSound;
    public AudioClip deathSound;

    [Header("Pickup Sounds")]
    public AudioClip pickupSound;
    public AudioClip heartPickupSound;
    public AudioClip snowballPickupSound;


    enum SurfaceType
    {
        Snow,
        Ice,
        Grass,
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void PlaySwordSwing()
    {
        if (actionAudioSource != null && swordSwingSound != null)
        {
            actionAudioSource.PlayOneShot(swordSwingSound);
        }
    }

    public void PlayHardHit()
    {
        if (actionAudioSource != null && swordHardHitSound != null)
        {
            actionAudioSource.PlayOneShot(swordHardHitSound);
        }
    }

    public void PlayFleshHit()
    {
        if (actionAudioSource != null && swordFleshHitSound != null)
        {
            actionAudioSource.PlayOneShot(swordFleshHitSound);
        }
    }

    public void PlayShieldUp()
    {
        if (actionAudioSource != null && shieldUpSound != null)
        {
            actionAudioSource.PlayOneShot(shieldUpSound);
        }
    }

    public void PlayShieldBlock()
    {
        if (actionAudioSource != null && shieldBlockSound != null)
        {
            Debug.Log("Doing shield block");
            actionAudioSource.PlayOneShot(shieldBlockSound);
        }
    }

    public void PlayGrunt()
    {
        if (actionAudioSource != null && gruntSound != null)
        {
            actionAudioSource.PlayOneShot(gruntSound);
        }
    }


    // Footstep stuff -----------------------
    private SurfaceType GetCurrentSurface()
    {
        Ray ray = new Ray(transform.position, Vector3.down);

        if(Physics.Raycast(ray, out RaycastHit hit, 3f, groundLayer))
        {
            string tag = hit.collider.tag;

            if (tag == "Snow")
                return SurfaceType.Snow;
            else if (tag == "Ice")
                return SurfaceType.Ice;
            else if (tag == "Grass")
                return SurfaceType.Grass;
        }

        return SurfaceType.Snow; // Default to snow if no surface detected
    }

    private List<AudioClip> GetFootstepClipsForSurface(SurfaceType surface)
    {
        switch (surface)
        {
            case SurfaceType.Snow:
                return snowFS;
            case SurfaceType.Ice:
                return iceFS;
            case SurfaceType.Grass:
                return grassFS;
            default:
                return snowFS;
        }
    }

    public void PlayFootstep()
    {
        SurfaceType currentSurface = GetCurrentSurface();
        List<AudioClip> footstepClips = GetFootstepClipsForSurface(currentSurface);

        if (footstepAudioSource != null && footstepClips.Count > 0)
        {
            int index = Random.Range(0, footstepClips.Count);
            AudioClip clip = footstepClips[index];
            footstepAudioSource.clip = clip;
            footstepAudioSource.volume = Random.Range(0.8f, 1f); // Add some random volume variation
            footstepAudioSource.pitch = Random.Range(0.95f, 1.05f); // Add some random pitch variation
            footstepAudioSource.Play();
        }
    }
    
    public void PlayShieldFootstep()
    {
        SurfaceType currentSurface = GetCurrentSurface();
        List<AudioClip> footstepClips = GetFootstepClipsForSurface(currentSurface);

        if (footstepAudioSource != null && footstepClips.Count > 0)
        {
            int index = Random.Range(0, footstepClips.Count);
            AudioClip clip = footstepClips[index];
            footstepAudioSource.clip = clip;
            footstepAudioSource.volume = Random.Range(0.4f, 0.6f); // Add some random volume variation
            footstepAudioSource.pitch = Random.Range(0.95f, 1.05f); // Add some random pitch variation
            footstepAudioSource.Play();
        }
    }
    // --------------------------------------

    // Hurt sound
    public void PlayHurt()
    {
        if (hurtAudioSource != null && hurtSound != null)
        {
            hurtAudioSource.pitch = Random.Range(0.95f, 1.05f); // Add some random pitch variation
            hurtAudioSource.PlayOneShot(hurtSound);
        }
    }

    public void PlayHit()
    {
        if (actionAudioSource != null && hitSound != null)
        {
            actionAudioSource.PlayOneShot(hitSound);
        }
    }

    public void PlayDeath()
    {
        if (actionAudioSource != null && deathSound != null)
        {
            actionAudioSource.PlayOneShot(deathSound);
        }
    }

    // Pickup sounds
    public void PlayPickupEvent()
    {
        if (pickupAudioSource != null && pickupSound != null)
        {
            pickupAudioSource.PlayOneShot(pickupSound);
            Debug.Log("Played pickup sound");
        }
    }

    public void PlayHeartPickup()
    {
        if (pickupAudioSource != null && heartPickupSound != null)
        {
            pickupAudioSource.PlayOneShot(heartPickupSound);
        }
    }

    public void PlaySnowballPickup()
    {
        if (pickupAudioSource != null && snowballPickupSound != null)
        {
            pickupAudioSource.PlayOneShot(snowballPickupSound);
        }
    }
}
