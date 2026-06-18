using UnityEngine;

public class ScoreTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider collision)
    {
        // only award a point when the player passes through this trigger zone
        if (collision.CompareTag("Player"))
        {
            FlappyGameManager.Instance.AddScore();
        }
    }
}