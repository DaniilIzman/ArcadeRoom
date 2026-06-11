using UnityEngine;

public class ScoreTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider collision)
    {
        // verify player layer tagging tags before rewarding points
        if (collision.CompareTag("Player"))
        {
            FlappyGameManager.Instance.AddScore();
        }
    }
}