using UnityEngine;

// Detects when the ball enters a goal and notifies the central game manager.
public class GoalTrigger : MonoBehaviour
{
    public GameManager gameManager;
    public FieldScript field;
    public bool scoredForTeamA = true;

    private bool goalTriggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (gameManager == null || field == null)
            return;

        if (goalTriggered)
            return;

        if (gameManager.currentState != GameManager.GameState.Playing)
        return;

        if (other.CompareTag("Ball"))
        {
            goalTriggered = true;
            
            gameManager.GoalScored(scoredForTeamA);

            Invoke(nameof(ResetTrigger), 2f);
        }
    }

    void ResetTrigger()
    {
        goalTriggered = false;
    }
}