using UnityEngine;
using UnityEngine.UI;

// shows a one-time welcome / instructions screen the first time a new game enters the arcade room
public class ArcadeIntroController : MonoBehaviour
{
    // true while the welcome screen is open; other systems may check this if needed
    public static bool IsActive { get; private set; }

    // the welcome panel (text + continue button) shown on first entry
    [Header("UI References")]
    public GameObject introPanel;
    public Button     continueButton;

    // relocks and hides the cursor when the screen is closed (typical for a first-person room)
    [Header("Behaviour")]
    [SerializeField] private bool lockCursorOnClose = true;

    // the save slot currently being played, used to read/clear the intro flag
    private int _activeSlot;

    // cached player components so their input can be frozen while the intro is open
    private PlayerCamera   _camera;
    private PlayerMovement _movement;

    private void Awake()
    {
        // hook the continue button up to close the screen
        if (continueButton) continueButton.onClick.AddListener(CloseIntro);
    }

    private void Start()
    {
        _activeSlot = PlayerPrefs.GetInt("Global_LastPlayedSlot", 1);

        // cache the player controllers so we can freeze and unfreeze their input
        _camera   = Object.FindFirstObjectByType<PlayerCamera>();
        _movement = Object.FindFirstObjectByType<PlayerMovement>();

        // show the welcome screen only if this slot was just started as a new game
        bool showIntro = PlayerPrefs.GetInt($"Slot_{_activeSlot}_ShowArcadeIntro", 0) == 1;

        if (showIntro) ShowIntro();
        else if (introPanel) introPanel.SetActive(false);
    }

    private void LateUpdate()
    {
        // keep the cursor free every frame while the intro is up so nothing can re-lock it
        if (IsActive)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
        }
    }

    // displays the welcome screen, freezes the player, and blocks the escape menu
    private void ShowIntro()
    {
        IsActive = true;
        if (introPanel) introPanel.SetActive(true);

        // freeze movement and camera look using the existing arcade freeze flag
        if (_camera)   _camera.isFrozenByArcade   = true;
        if (_movement) _movement.isFrozenByArcade = true;

        // stop the escape key from opening the pause menu while the intro is shown
        if (EscapeMenu.Instance) EscapeMenu.Instance.ForceCloseAndLock();

        Time.timeScale   = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    // closes the welcome screen, clears the flag so it never auto-shows again, and resumes play
    public void CloseIntro()
    {
        // ignore stray calls once the intro is already closed
        if (!IsActive) return;
        IsActive = false;

        if (introPanel) introPanel.SetActive(false);

        // clear the flag so the intro does not appear again for this slot
        PlayerPrefs.DeleteKey($"Slot_{_activeSlot}_ShowArcadeIntro");
        PlayerPrefs.Save();

        // unfreeze the player and allow the escape menu to open again
        if (_camera)   _camera.isFrozenByArcade   = false;
        if (_movement) _movement.isFrozenByArcade = false;
        if (EscapeMenu.Instance) EscapeMenu.Instance.UnlockMenu();

        Time.timeScale = 1f;

        if (lockCursorOnClose)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }
    }
}