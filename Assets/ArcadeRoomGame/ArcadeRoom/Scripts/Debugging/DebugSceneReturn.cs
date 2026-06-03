using UnityEngine;
using UnityEngine.SceneManagement;

public class DebugSceneReturn : MonoBehaviour
{
    [Header("Scene Configuration")]
    [Tooltip("The exact name of your main arcade/lobby scene.")]
    public string lobbySceneName;

    [Header("Controls")]
    [Tooltip("The key you press to trigger the scene change.")]
    public KeyCode returnKey = KeyCode.R;

    private void Update()
    {
        // Detect the debug key press
        if (Input.GetKeyDown(returnKey))
        {
            if (!string.IsNullOrEmpty(lobbySceneName))
            {
                Debug.Log($"[DEBUG] Returning to '{lobbySceneName}' to verify player position/rotation persistence.");
                SceneManager.LoadScene(lobbySceneName);
            }
            else
            {
                Debug.LogError("[DEBUG] Cannot return! 'Lobby Scene Name' is empty in the Inspector.");
            }
        }
    }

    // Optional: Attach this method to a UI Button component's OnClick() event if preferred
    public void ReturnToLobbyViaButton()
    {
        if (!string.IsNullOrEmpty(lobbySceneName))
        {
            SceneManager.LoadScene(lobbySceneName);
        }
    }
}