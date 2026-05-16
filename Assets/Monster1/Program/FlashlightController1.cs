using UnityEngine;

public class FlashlightController1 : MonoBehaviour
{
    [Header("Flashlight")]
    public Light flashlight;

    [Header("Sound")]
    public AudioSource audioSource;
    public AudioClip turnOnSound;
    public AudioClip turnOffSound;

    void Start()
    {
        if (flashlight != null)
        {
            flashlight.enabled = false;
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            ToggleFlashlight();
        }
    }

    void ToggleFlashlight()
    {
        flashlight.enabled = !flashlight.enabled;

        if (audioSource != null)
        {
            if (flashlight.enabled)
            {
                audioSource.PlayOneShot(turnOnSound);
            }
            else
            {
                audioSource.PlayOneShot(turnOffSound);
            }
        }
    }
}