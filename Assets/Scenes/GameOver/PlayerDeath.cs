using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerDeath : MonoBehaviour
{
    private bool isDead = false;

    public void Die()
    {
        if (isDead)
            return;

        isDead = true;

        SceneManager.LoadScene("GameOver");
    }
}