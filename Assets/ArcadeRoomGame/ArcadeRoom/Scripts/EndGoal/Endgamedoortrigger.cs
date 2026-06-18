using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// placed on the exit door's trigger collider in the arcade room scene
// pressing e while incomplete plays a locked sound; pressing e when complete triggers the ending sequence
// the door's audiosource is routed through the sfx mixer group so the sfx slider controls its volume
[RequireComponent(typeof(Collider))]
public class EndGameDoorTrigger : MonoBehaviour
{
    // key the player must press to interact with the door
    [Header("Interaction")]
    public KeyCode interactKey = KeyCode.E;
    public string playerTag = "Player";

    // text shown in the uimanager prompt depending on whether the game is complete
    [Header("Prompts (shown via UIManager)")]
    public string readyPrompt  = "Press E to leave — the collection is complete!";
    public string lockedPrompt = "Unlock every machine and collect every trophy to finish.";

    // sounds played when the player interacts with the door in each state
    [Header("Audio")]
    [Tooltip("Played when the door is still locked (goals not met).")]
    public AudioClip lockedSound;
    [Tooltip("Played when the door unlocks (all goals met).")]
    public AudioClip unlockedSound;

    // how long to wait after playing the unlock sound before loading the menu
    [Header("Transition")]
    [Tooltip("Seconds to let the unlock sound play before fading to the menu.")]
    public float loadDelay = 1f;

    // name of the scene to load when the player finishes the game
    [Header("Scene Routing")]
    public string mainMenuSceneName = "MainMenu";

    private AudioSource _audio;
    private PlayerMovement _player;
    private PlayerCamera _playerCam;

    private bool _playerInside;

    // caches the last known completion state to avoid redundant prompt updates
    private bool _lastComplete;

    // prevents the finish routine from being triggered more than once
    private bool _finishing;

    // ensure the collider is set to trigger mode when the component is first added
    private void Reset()
    {
        Collider c = GetComponent<Collider>();
        if (c) c.isTrigger = true;
    }

    private void Awake()
    {
        // add an audiosource at runtime if none was placed in the inspector
        _audio = GetComponent<AudioSource>();
        if (_audio == null) _audio = gameObject.AddComponent<AudioSource>();
        _audio.playOnAwake = false;
    }

    private void Start()
    {
        // route the door's audio through the sfx mixer group
        SettingsManager.Instance?.Route(_audio, SettingsManager.AudioCategory.SFX);
    }

    private void Update()
    {
        if (!_playerInside || _finishing) return;

        bool complete = GameCompletionManager.Instance != null && GameCompletionManager.Instance.IsComplete();

        // update the prompt whenever completion state changes
        if (complete != _lastComplete)
        {
            _lastComplete = complete;
            ShowPrompt(complete);
        }

        if (Input.GetKeyDown(interactKey))
        {
            if (complete) StartCoroutine(FinishRoutine());
            else PlayLocked();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        _playerInside = true;
        _player = other.GetComponent<PlayerMovement>();

        // get the camera from a child of the player if movement is found
        _playerCam = _player != null ? _player.GetComponentInChildren<PlayerCamera>() : null;

        _lastComplete = GameCompletionManager.Instance != null && GameCompletionManager.Instance.IsComplete();
        ShowPrompt(_lastComplete);
    }

    private void OnTriggerExit(Collider other)
    {
        // don't clear anything mid-finishing sequence
        if (_finishing) return;
        if (!other.CompareTag(playerTag)) return;
        _playerInside = false;
        if (UIManager.Instance) UIManager.Instance.HidePrompt();
    }

    // displays the appropriate prompt through uimanager based on current completion state
    private void ShowPrompt(bool complete)
    {
        if (UIManager.Instance == null) return;
        UIManager.Instance.ShowPrompt(complete ? readyPrompt : lockedPrompt);
    }

    // plays the locked sound when the player tries to leave before completing everything
    private void PlayLocked()
    {
        if (_audio != null && lockedSound != null) _audio.PlayOneShot(lockedSound);
    }

    // freezes the player, marks the slot as completed, then transitions to the main menu
    private IEnumerator FinishRoutine()
    {
        _finishing = true;

        // freeze input the same way as boarding an arcade machine
        if (EscapeMenu.Instance != null) EscapeMenu.Instance.ForceCloseAndLock();
        if (_player != null) _player.isFrozenByArcade = true;
        if (_playerCam != null) _playerCam.isFrozenByArcade = true;

        if (UIManager.Instance) UIManager.Instance.HidePrompt();

        // write the completion flag to the save slot before leaving
        GameCompletionManager.Instance?.MarkCurrentSlotCompleted();

        if (_audio != null && unlockedSound != null) _audio.PlayOneShot(unlockedSound);

        // wait for the unlock sound to finish before loading the menu
        yield return new WaitForSecondsRealtime(loadDelay);

        if (SceneFader.Instance != null) SceneFader.Instance.LoadScene(mainMenuSceneName);
        else SceneManager.LoadScene(mainMenuSceneName);
    }
}