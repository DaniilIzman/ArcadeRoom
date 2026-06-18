using UnityEngine;

// singleton that tracks whether the current save slot has reached 100% completion
// completion requires every listed arcade machine to be unlocked and every shop trophy to be bought
// lives only in the arcade room scene; not dontdestroyonload
// machine unlock state is read from the same playerprefs key used by arcademachine.cs
// game names in arcadeGames must exactly match each arcademachine's gameName field
// machines with requiresUnlock = false are never written to the unlock string, so don't list them here
public class GameCompletionManager : MonoBehaviour
{
    public static GameCompletionManager Instance { get; private set; }

    // names of all unlockable arcade machines; must match arcademachine.gameName exactly
    [Tooltip("Must match the machine names used by your unlock keys / MainMenuController.knownArcadeGames")]
    public string[] arcadeGames = { "Space Invaders", "Flappy Bird", "Endless Runner" };

    // reads the active slot number from playerprefs each time it is accessed
    public int CurrentSlot => PlayerPrefs.GetInt("Global_LastPlayedSlot", 1);

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // returns true if the given game name is present in the active slot's unlock string
    public bool IsMachineUnlocked(string game)
    {
        string data = PlayerPrefs.GetString($"ArcadeUnlocks_Slot{CurrentSlot}", "");
        return data.Contains($"[{game}]");
    }

    // counts how many of the listed arcade games have been unlocked in the active slot
    public int UnlockedMachineCount()
    {
        int count = 0;
        foreach (string g in arcadeGames)
            if (IsMachineUnlocked(g)) count++;
        return count;
    }

    // total number of machines that can be unlocked, derived from the arcadeGames array length
    public int TotalMachines => arcadeGames != null ? arcadeGames.Length : 0;

    // returns true only when every listed machine has been unlocked
    public bool AllMachinesUnlocked() => TotalMachines > 0 && UnlockedMachineCount() == TotalMachines;

    // returns true only when every shop item has been purchased
    public bool AllTrophiesBought() => ShopManager.Instance != null && ShopManager.Instance.AllItemsBought();

    // returns true when both all machines are unlocked and all trophies are bought
    public bool IsComplete() => AllMachinesUnlocked() && AllTrophiesBought();

    // writes the completion flag for the current slot to playerprefs
    public void MarkCurrentSlotCompleted()
    {
        PlayerPrefs.SetInt($"Slot_{CurrentSlot}_Completed", 1);
        PlayerPrefs.Save();
    }
}