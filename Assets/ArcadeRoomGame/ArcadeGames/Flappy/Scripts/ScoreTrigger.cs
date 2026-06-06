using UnityEngine;

public class ScoreTrigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // check if it's actually the bird crossing the line
        if (collision.CompareTag("Player"))
        {
            FlappyGameManager.Instance.AddScore();
        }
    }
}