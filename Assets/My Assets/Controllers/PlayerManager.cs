using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerManager : MonoBehaviour
{
    [Header("Base")]
    public float baseMaxEnergy = 100f;

    [Header("Stats")]
    [Range(0, 100)] public float health = 100f;
    [Range(0, 100)] public float hunger = 100f;
    [Range(0, 100)] public float cold = 0f;

    [Header("Energy")]
    public float energy = 100f;
    public float maxEnergy;

    [Header("Hunger Drain")]
    public bool drainHunger = true;
    public float hungerDrainPerMinute = 6f;
    public float minHunger = 0f;

    private const float MAX = 100f;
    public float EnergyPercent01 => baseMaxEnergy <= 0 ? 0 : maxEnergy / baseMaxEnergy;
    public bool outOfStamina = false;

    [Header("Death / Game Over")]
    [Tooltip("If enabled, reaching a death condition will trigger Game Over.")]
    public bool enableDeath = true;
    public GameObject gameOverPanel;
    [Header("Game Over Input")]
    [Tooltip("Press this input (eg. A button) while dead to return to main menu.")]
    public InputActionProperty returnToMenuAction;
    [Tooltip("Disable these scripts when dead (locomotion, climbing, etc).")]
    public MonoBehaviour[] disableWhileDead;
    public string mainMenuSceneName = "MainMenu";
    public bool pauseTimeOnDeath = false;

    public bool IsDead => _isDead;
    private bool _isDead;
    private bool _returnToMenuArmed;

    private void Awake()
    {
        if (gameOverPanel)
            gameOverPanel.SetActive(false);

        // Keep disabled until death to avoid conflicts with gameplay inputs.
        returnToMenuAction.action?.Disable();
    }

    private void OnDisable()
    {
        // InputActionReferences are assets; make sure we don't leave this enabled across scene loads.
        returnToMenuAction.action?.Disable();
    }

    private void Update()
    {
        if (_isDead)
        {
            // Require a release-then-press to avoid immediately returning if the button was held during death.
            InputAction action = returnToMenuAction.action;
            if (action != null)
            {
                if (!_returnToMenuArmed)
                {
                    if (!action.IsPressed())
                        _returnToMenuArmed = true;
                }
                else if (action.WasPressedThisFrame())
                {
                    ReturnToMainMenu();
                }
            }

            return;
        }

        UpdateHunger(Time.deltaTime);
        RecalculateEnergy();
    }

    private void UpdateHunger(float dt)
    {
        if (!drainHunger) return;
        Debug.Log("Draining hunger");
        float drainPerSecond = hungerDrainPerMinute / 60f;
        hunger = Mathf.Max(minHunger, hunger - drainPerSecond * dt);
    }

    private void RecalculateEnergy()
    {
        if (_isDead)
        {
            maxEnergy = 0f;
            energy = 0f;
            outOfStamina = true;
            return;
        }

        float h01 = Mathf.Clamp01(health / MAX);
        float hu01 = Mathf.Clamp01(hunger / MAX);
        float c01 = Mathf.Clamp01(cold / MAX);
        if (h01 <= 0f || hu01 <= 0f || c01 >= 1f)
        {
            maxEnergy = 0f;
            energy = 0f;
            outOfStamina = true;

            if (enableDeath)
                Die();
            return;
        }

        float healthPenalty = h01 >= 0.7f ? 1f : Mathf.Lerp(0.5f, 1f, h01 / 0.7f);
        float hungerPenalty = hu01 >= 0.7f ? 1f : Mathf.Lerp(0.5f, 1f, hu01 / 0.7f);

        float tCold = (c01 - 0.3f) / 0.7f;
        float coldPenalty = c01 <= 0.3f ? 1f : Mathf.Lerp(1f, 0.5f, Mathf.Clamp01(tCold));

        maxEnergy = baseMaxEnergy * healthPenalty * hungerPenalty * coldPenalty;
        energy = Mathf.Min(energy, maxEnergy);
    }

    private void Die()
    {
        if (_isDead) return;
        _isDead = true;
        _returnToMenuArmed = false;

        // Safety: block stamina-based actions (climbing uses this).
        outOfStamina = true;
        maxEnergy = 0f;
        energy = 0f;

        if (pauseTimeOnDeath)
            Time.timeScale = 0f;

        if (gameOverPanel)
            gameOverPanel.SetActive(true);
        else
            Debug.LogWarning("PlayerManager: gameOverPanel is not assigned. Game Over will not be shown.");

        // Enable only now so it doesn't conflict with gameplay inputs.
        returnToMenuAction.action?.Enable();

        if (disableWhileDead != null && disableWhileDead.Length > 0)
        {
            for (int i = 0; i < disableWhileDead.Length; i++)
            {
                MonoBehaviour b = disableWhileDead[i];
                if (!b) continue;
                if (b == this) continue;
                b.enabled = false;
            }
        }
        else
        {
            // Minimal fallback so "dead = can't move" works even if you forget to assign the list.
            DisableMovementFallback();
        }
    }

    private void DisableMovementFallback()
    {
        MonoBehaviour[] all = transform.root.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < all.Length; i++)
        {
            MonoBehaviour b = all[i];
            if (!b) continue;
            if (b == this) continue;

            string fullName = b.GetType().FullName;
            if (fullName == "Oculus.Interaction.Locomotion.FirstPersonLocomotor"
                || fullName == "ClimbManager"
                || fullName == "ClimbHand")
            {
                b.enabled = false;
            }
        }
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        returnToMenuAction.action?.Disable();

        if (string.IsNullOrWhiteSpace(mainMenuSceneName))
        {
            Debug.LogError("PlayerManager: mainMenuSceneName is empty.");
            return;
        }

        SceneManager.LoadScene(mainMenuSceneName, LoadSceneMode.Single);
    }

    public void TakeDamage(float dmg)
    {
        if (_isDead) return;

        health = Mathf.Clamp(health - dmg, 0f, 100f);
        if (enableDeath && health <= 0f)
            Die();
    }

    public void Heal(float amount)
    {
        if (_isDead) return;
        health = Mathf.Clamp(health + amount, 0f, 100f);
    }

    public void Eat(float amount)
    {
        if (_isDead) return;
        hunger = Mathf.Clamp(hunger + amount, 0f, 100f);
    }

    public void Freeze(float amount)
    {
        if (_isDead) return;
        cold = Mathf.Clamp(cold + amount, 0f, 100f);
    }
}
