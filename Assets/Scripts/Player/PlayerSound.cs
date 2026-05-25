using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerSound : MonoBehaviour
{

    [Header("Audio Sources")]
    public AudioSource footstepAudioSource;
    public AudioSource actionAudioSource;
    public AudioSource hurtAudioSource;

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

    [Header("Hurt Sounds")]
    public AudioClip hurtSound;
    public AudioClip hitSound;


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


    // Footstep stuff -----------------------
    private SurfaceType GetCurrentSurface()
    {
        Ray ray = new Ray(transform.position, Vector3.down);

        if(Physics.Raycast(ray, out RaycastHit hit, 3f, groundLayer))
        {
            string tag = hit.collider.tag;
            Debug.Log("Surface detected: " + tag);

            if (tag == "Snow")
                return SurfaceType.Snow;
            else if (tag == "Ice")
                return SurfaceType.Ice;
            else if (tag == "Grass")
                Debug.Log("Grass detected");
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
}
