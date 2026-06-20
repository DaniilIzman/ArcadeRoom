using UnityEngine;
using UnityEngine.UI;

// shows a one-time welcome / instructions screen the first time a new game enters the arcade room
public class ArcadeIntroController : MonoBehaviour
{
    // the welcome panel (text + continue button) shown on first entry
    [Header("UI References")]
    public GameObject introPanel;
    public Button     continueButton;

    // freezes the game and frees the cursor while the welcome screen is open
    [Header("Behaviour")]
    [SerializeField] private bool freezeWhileShown = true;
    // relocks and hides the cursor when the screen is closed (typical for a first-person room)
    [SerializeField] private bool lockCursorOnClose = true;

    // the save slot currently being played, used to read/clear the intro flag
    private int _activeSlot;

    private void Awake()
    {
        // hook the continue button up to close the screen
        if (continueButton) continueButton.onClick.AddListener(CloseIntro);
    }

    private void Start()
    {
        _activeSlot = PlayerPrefs.GetInt("Global_LastPlayedSlot", 1);

        // show the welcome screen only if this slot was just started as a new game
        bool showIntro = PlayerPrefs.GetInt($"Slot_{_activeSlot}_ShowArcadeIntro", 0) == 1;

        if (showIntro) ShowIntro();
        else if (introPanel) introPanel.SetActive(false);
    }

    // displays the welcome screen and optionally pauses the game and frees the cursor
    private void ShowIntro()
    {
        if (introPanel) introPanel.SetActive(true);

        if (freezeWhileShown)
        {
            Time.timeScale   = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
        }
    }

    // closes the welcome screen, clears the flag so it never auto-shows again, and resumes play
    public void CloseIntro()
    {
        if (introPanel) introPanel.SetActive(false);

        // clear the flag so the intro does not appear again for this slot
        PlayerPrefs.DeleteKey($"Slot_{_activeSlot}_ShowArcadeIntro");
        PlayerPrefs.Save();

        if (freezeWhileShown) Time.timeScale = 1f;

        if (lockCursorOnClose)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }
    }
}