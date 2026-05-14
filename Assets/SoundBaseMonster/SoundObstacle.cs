using UnityEngine;

// 掛在牆壁、箱子等上面
public class SoundObstacle : MonoBehaviour
{
    [Tooltip("對聲音的衰減值（磚牆 ~6，木箱 ~2，玻璃 ~1）")]
    public float dampening = 5f;
}