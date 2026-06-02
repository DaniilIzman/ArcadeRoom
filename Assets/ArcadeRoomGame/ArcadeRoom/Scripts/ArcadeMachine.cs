using UnityEngine;

public class ArcadeMachine : Interactable
{
    [Header("Arcade Cabinets Settings")]
    public string machineName = "Pac-Man";
    public int rewardCredits = 15;

    private void Start()
    {
        // this dynamically formats the string to include the game name
        hoverText = "Press E to play " + machineName;
    }

    public override void Interact()
    {
        Debug.Log("Now playing: " + machineName + "!");
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddCredits(rewardCredits);
        }
    }
}