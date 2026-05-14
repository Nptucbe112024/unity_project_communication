using UnityEngine;

public class FlashlightController1 : MonoBehaviour
{
    public Light flashlight;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            flashlight.enabled = !flashlight.enabled;
        }
    }
}