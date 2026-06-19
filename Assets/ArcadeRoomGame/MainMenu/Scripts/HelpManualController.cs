using UnityEngine;
using UnityEngine.UI;
using TMPro;

// drives the paged help manual: shows one section at a time with prev/next navigation
public class HelpManualController : MonoBehaviour
{
    // a single help page: an optional icon, a heading, and the body explanation
    [System.Serializable]
    public class HelpPage
    {
        public string title;
        [TextArea(3, 10)] public string body;
        public Sprite icon;
    }

    // all the pages shown in order, filled in the inspector
    [Header("Pages")]
    public HelpPage[] pages;

    // ui elements that display the current page
    [Header("UI References")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI bodyText;
    public Image           iconImage;
    public TextMeshProUGUI pageIndicator;

    // the left and right navigation buttons
    [Header("Navigation Buttons")]
    public Button prevButton;
    public Button nextButton;

    // when true the pages loop around instead of stopping at the first/last
    [Header("Behaviour")]
    public bool wrapAround = false;

    // index of the page currently being shown
    private int _current;

    private void Awake()
    {
        // hook the navigation buttons up to their handlers
        if (prevButton) prevButton.onClick.AddListener(Prev);
        if (nextButton) nextButton.onClick.AddListener(Next);
    }

    // reset to the first page every time the help panel is opened
    private void OnEnable()
    {
        _current = 0;
        ShowPage(_current);
    }

    // displays the page at the given index and updates the buttons and indicator
    private void ShowPage(int index)
    {
        if (pages == null || pages.Length == 0) return;

        // keep the index inside the valid range
        _current = Mathf.Clamp(index, 0, pages.Length - 1);
        HelpPage page = pages[_current];

        if (titleText) titleText.text = page.title;
        if (bodyText)  bodyText.text  = page.body;

        // show the icon only if this page has one assigned
        if (iconImage)
        {
            iconImage.sprite  = page.icon;
            iconImage.enabled = page.icon != null;
        }

        // update the "1 / 5" style page counter
        if (pageIndicator) pageIndicator.text = $"{_current + 1} / {pages.Length}";

        UpdateNavButtons();
    }

    // greys out prev on the first page and next on the last page, unless wrapping is on
    private void UpdateNavButtons()
    {
        if (wrapAround) return;
        if (prevButton) prevButton.interactable = _current > 0;
        if (nextButton) nextButton.interactable = _current < pages.Length - 1;
    }

    // moves to the next page, looping to the first if wrap is enabled
    public void Next()
    {
        if (pages == null || pages.Length == 0) return;
        int next = _current + 1;
        if (next >= pages.Length) next = wrapAround ? 0 : pages.Length - 1;
        ShowPage(next);
    }

    // moves to the previous page, looping to the last if wrap is enabled
    public void Prev()
    {
        if (pages == null || pages.Length == 0) return;
        int prev = _current - 1;
        if (prev < 0) prev = wrapAround ? pages.Length - 1 : 0;
        ShowPage(prev);
    }
}