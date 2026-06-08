using UnityEngine;

public class GoalTrigger : MonoBehaviour
{
    public GameManager gameManager;
    public bool scoredForTeamA = true;
    public string ballTag = "Ball";

    void OnTriggerEnter2D(Collider2D other)
    {
        if (gameManager == null || other == null)
            return;

        if (string.IsNullOrEmpty(ballTag) || other.CompareTag(ballTag))
        {
            gameManager.GoalScored(scoredForTeamA);
        }
    }
}

