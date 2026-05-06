using UnityEngine;

public class FlashlightController : MonoBehaviour
{
    [Header("Flashlight Settings")]
    public Light flashlight;
    public KeyCode toggleKey = KeyCode.F;
    public bool startOn = false;

    void Start()
    {
        if (flashlight != null)
        {
            flashlight.enabled = startOn;
        }
    }

    void Update()
    {
        if (flashlight == null) return;

        if (Input.GetKeyDown(toggleKey))
        {
            flashlight.enabled = !flashlight.enabled;
        }
    }

    public bool IsOn()
    {
        return flashlight != null && flashlight.enabled;
    }
}