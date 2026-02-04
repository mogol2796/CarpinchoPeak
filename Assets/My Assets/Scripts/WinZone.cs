using UnityEngine;
using UnityEngine.SceneManagement;

public class WinZone : MonoBehaviour
{
    [Header("Win Settings")]
    public GameObject winUI;
    public bool restartOnWin = false;
    public float delayBeforeRestart = 5f;

    [Header("Feedback Settings")]
    public AudioClip winSound;
    public ParticleSystem fireworksParticles; // <--- NEW: Reference for your fireworks
    public bool slowMotionOnWin = true;
    [Range(0f, 1f)] public float slowMoAmount = 0.2f;

    private bool hasWon = false;

    private void Awake()
    {
        winUI.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !hasWon)
        {
            WinSequence(other.gameObject);
        }
    }

    void WinSequence(GameObject player)
    {
        hasWon = true;

        // 1. Visual Feedback: Show UI and Cursor
        if (winUI != null)
        {
            winUI.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // 2. NEW: Trigger Fireworks
        if (fireworksParticles != null)
        {
            fireworksParticles.Play();
        }

        // 3. Audio Feedback
        AudioSource audio = GetComponent<AudioSource>();
        if (audio != null && winSound != null)
        {
            audio.PlayOneShot(winSound);
        }

        // 4. Environment: Stop the Fog
        FogRise fog = FindFirstObjectByType<FogRise>();
        if (fog != null) fog.enabled = false;

        // 5. Dramatic Slow Motion
        if (slowMotionOnWin)
        {
            Time.timeScale = slowMoAmount;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
        }

        if (restartOnWin) Invoke("RestartGame", delayBeforeRestart * Time.timeScale);
    }

    void RestartGame()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}