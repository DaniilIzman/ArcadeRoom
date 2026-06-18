using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// singleton that handles fade-to-black transitions between scenes
// builds its own fullscreen canvas at runtime so no scene setup is needed
// uses unscaled time so fades work correctly even when timescale is zero
[DisallowMultipleComponent]
public class SceneFader : MonoBehaviour
{
    public static SceneFader Instance { get; private set; }

    // how long each fade in or out takes in seconds
    [SerializeField] private float fadeDuration = 0.5f;

    // the color the screen fades to during transitions
    [SerializeField] private Color fadeColor = Color.black;

    // the canvas group used to control the opacity of the fullscreen overlay
    private CanvasGroup _group;

    // prevents overlapping transition calls from running simultaneously
    private bool _busy;

    // automatically creates the scenefader before any scene loads if one doesn't exist
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;
        var go = new GameObject("SceneFader");
        go.AddComponent<SceneFader>();
    }

    private void Awake()
    {
        // destroy duplicates to keep a single instance alive across all scenes
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildOverlay();

        // start the first scene with a fade-in from black
        _group.alpha = 1f;
        StartCoroutine(Fade(1f, 0f));
    }

    // constructs the fullscreen black overlay canvas entirely in code
    private void BuildOverlay()
    {
        var canvasGO = new GameObject("FadeCanvas");
        canvasGO.transform.SetParent(transform, false);

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;

        // high sorting order ensures the overlay draws on top of all other ui
        canvas.sortingOrder = 32760;
        canvasGO.AddComponent<CanvasScaler>();

        // the image that fills the entire screen with the fade color
        var imgGO = new GameObject("FadeImage");
        imgGO.transform.SetParent(canvasGO.transform, false);
        var img = imgGO.AddComponent<Image>();
        img.color = fadeColor;

        // disable raycasting so the overlay never blocks ui interaction when invisible
        img.raycastTarget = false;

        // stretch the image to fill the entire canvas
        var rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // canvas group controls the alpha and raycast blocking of the whole overlay
        _group = canvasGO.AddComponent<CanvasGroup>();
        _group.alpha          = 0f;
        _group.blocksRaycasts = false;
        _group.interactable   = false;
    }

    // starts a fade-out, loads the named scene, then fades back in
    public void LoadScene(string sceneName)
    {
        if (_busy || string.IsNullOrEmpty(sceneName)) return;
        StartCoroutine(FadeAndLoad(sceneName, -1));
    }

    // overload that accepts a build index instead of a scene name
    public void LoadScene(int buildIndex)
    {
        if (_busy) return;
        StartCoroutine(FadeAndLoad(null, buildIndex));
    }

    // coroutine that sequences the fade-out, scene load, and fade-in
    private IEnumerator FadeAndLoad(string sceneName, int buildIndex)
    {
        _busy = true;

        // block clicks during the transition so the player can't interact mid-fade
        _group.blocksRaycasts = true;

        yield return Fade(_group.alpha, 1f);

        // reset timescale so the new scene never inherits a paused state
        Time.timeScale = 1f;

        // load by name or index depending on which was provided
        AsyncOperation op = sceneName != null
            ? SceneManager.LoadSceneAsync(sceneName)
            : SceneManager.LoadSceneAsync(buildIndex);
        while (op != null && !op.isDone) yield return null;

        yield return Fade(1f, 0f);

        _group.blocksRaycasts = false;
        _busy = false;
    }

    // lerps the overlay alpha between two values over the configured fade duration using unscaled time
    private IEnumerator Fade(float from, float to)
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            _group.alpha = Mathf.Lerp(from, to, t / fadeDuration);
            yield return null;
        }

        // snap to the target value to avoid floating point drift
        _group.alpha = to;
    }
}