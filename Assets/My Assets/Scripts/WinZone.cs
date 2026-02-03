using UnityEngine;
using UnityEngine.SceneManagement; // Useful if you want to restart or change scenes

public class WinZone : MonoBehaviour
{
    [Header("Win Settings")]
    public GameObject winUI; // Assign a "You Win!" text or panel here
    public bool restartOnWin = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Peak Reached! You Win!");

            // 1. Show Win UI
            if (winUI != null) winUI.SetActive(true);

            // 2. Stop the Fog (Optional)
            FogRise fog = FindFirstObjectByType<FogRise>();
            if (fog != null) fog.enabled = false;

            // 3. Disable Player movement (Optional)
            // other.GetComponent<PlayerController>().enabled = false;

            if (restartOnWin) Invoke("RestartGame", 3f);
        }
    }

    void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}