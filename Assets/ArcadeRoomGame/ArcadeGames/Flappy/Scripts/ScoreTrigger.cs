using UnityEngine;

public class ScoreTrigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // verify player layer tagging tags before rewarding points
        if (collision.CompareTag("Player"))
        {
            FlappyGameManager.Instance.AddScore();
        }
    }
}