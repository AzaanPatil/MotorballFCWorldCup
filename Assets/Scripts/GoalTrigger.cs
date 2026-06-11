using UnityEngine;

// Detects when the ball enters a goal and notifies the central game manager.
public class GoalTrigger : MonoBehaviour
{
    public GameManager gameManager;
    public FieldScript field;
    public bool scoredForTeamA = true;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (gameManager == null || field == null)
            return;

        //if (gameManager.currentState != GameManager.GameState.Playing)
        //return;

        if (other.CompareTag("Ball"))
        {
            Debug.Log(gameManager.currentState);
            gameManager.GoalScored(scoredForTeamA);

            field.ResetGame();
        }
    }
}