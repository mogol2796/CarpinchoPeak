using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    // MenuController implementation goes here
    [Header("Menu Inputs")]
    public InputActionProperty startGameAction;
    public InputActionProperty toggleControlsAction;

    [Header("Scene")]
    public string gameSceneName = "GameScene";
    public GameObject panelControls;

    private bool controlsVisible = false;

    private void OnEnable() {
        startGameAction.action?.Enable(); 
        toggleControlsAction.action?.Enable();
    }
    private void OnDisable() {
        startGameAction.action?.Disable(); 
        toggleControlsAction.action?.Disable();
    }

    void Update()
    {
        if (startGameAction.action != null && startGameAction.action.WasPressedThisFrame())
        {
            StartGame();
        }

        if (toggleControlsAction.action != null && toggleControlsAction.action.WasPressedThisFrame())
        {
            Debug.Log("Toggling controls panel");
            ToggleControls();
        }
    }

    void StartGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    void ToggleControls()
    {
        controlsVisible = !controlsVisible;
        panelControls.SetActive(controlsVisible);
    }

}