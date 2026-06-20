using UnityEngine;

// toggles between the pause menu panel and the help manual panel inside the arcade room
public class PauseHelpToggle : MonoBehaviour
{
    // the main pause menu panel and the help guide panel it swaps to
    [Header("Panels")]
    public GameObject pausePanel;
    public GameObject helpPanel;

    private void Start()
    {
        // make sure the help panel starts hidden
        if (helpPanel) helpPanel.SetActive(false);
    }

    // opens the help guide and hides the pause menu (called by the pause menu's help button)
    public void OpenHelp()
    {
        if (pausePanel) pausePanel.SetActive(false);
        if (helpPanel)  helpPanel.SetActive(true);
    }

    // closes the help guide and returns to the pause menu (called by the help panel's back button)
    public void CloseHelp()
    {
        if (helpPanel)  helpPanel.SetActive(false);
        if (pausePanel) pausePanel.SetActive(true);
    }
}