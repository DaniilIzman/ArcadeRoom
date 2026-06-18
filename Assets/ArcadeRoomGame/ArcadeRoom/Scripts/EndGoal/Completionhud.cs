using UnityEngine;
using UnityEngine.UI;

// hud that shows progress toward 100% completion via icon arrays for machines and trophies
// icons fill left-to-right by count so unlock order doesn't matter
// only the alpha of each icon is changed; color and sprite are left as set in the inspector
public class CompletionHUD : MonoBehaviour
{
    // one image per arcade machine; lit count equals the number of unlocked machines
    [Header("Machine Icons (order matches GameCompletionManager.arcadeGames)")]
    public Image[] machineIcons;

    // one image per trophy; lit count equals the number of trophies purchased in the shop
    [Header("Trophy Icons (order matches ShopManager.shopItems)")]
    public Image[] trophyIcons;

    // alpha values applied to icons depending on their locked or unlocked state
    [Header("Alpha")]
    [Range(0f, 1f)] public float lockedAlpha   = 0.5f;
    [Range(0f, 1f)] public float unlockedAlpha = 1f;

    // optional gameobject shown only when every machine and trophy has been completed
    [Header("All-Complete Indicator (optional)")]
    public GameObject readyIndicator;

    // refresh whenever the hud becomes visible
    private void OnEnable() => Refresh();

    // poll every frame to keep icons in sync with live save data
    private void Update() => Refresh();

    // reads current completion data and updates all icon alphas and the ready indicator
    public void Refresh()
    {
        GameCompletionManager gc = GameCompletionManager.Instance;
        if (gc == null) return;

        // light up machine icons from left to right based on how many have been unlocked
        if (machineIcons != null)
        {
            int unlocked = gc.UnlockedMachineCount();
            for (int i = 0; i < machineIcons.Length; i++)
                SetAlpha(machineIcons[i], i < unlocked);
        }

        // light up trophy icons from left to right based on how many have been bought
        if (trophyIcons != null)
        {
            int bought = ShopManager.Instance != null ? ShopManager.Instance.BoughtCount : 0;
            for (int i = 0; i < trophyIcons.Length; i++)
                SetAlpha(trophyIcons[i], i < bought);
        }

        // show the all-complete indicator only when every requirement is met
        if (readyIndicator != null) readyIndicator.SetActive(gc.IsComplete());
    }

    // sets the alpha of a single icon image to either the locked or unlocked value
    private void SetAlpha(Image img, bool on)
    {
        if (img == null) return;
        Color c = img.color;
        c.a = on ? unlockedAlpha : lockedAlpha;
        img.color = c;
    }
}