using UnityEngine;

// Controls the playing field boundaries, ball reset behavior, and player kickoff positions.
public class FieldScript : MonoBehaviour
{
    public float leftBound;
    public float rightBound;
    public float topBound;
    public float bottomBound;

    public Transform ball;
    public Rigidbody2D ballRb;
    public GameManager gameManager;
    public Transform[] players;
    public Transform[] playerKickoffPoints;
    public Transform kickoffPoint;

    void Start()
    {
        // Cache the ball Rigidbody2D if it was not already assigned.
        if (ball != null && ballRb == null)
            ballRb = ball.GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (ball == null)
            return;

        // If the ball leaves the field bounds, reset it to the center.
        if (ball.position.x < leftBound || ball.position.x > rightBound || ball.position.y > topBound || ball.position.y < bottomBound)
        {
            ResetBall();
        }
    }

    void ResetBall()
    {
        if (ball != null)
        {
            if (kickoffPoint != null)
                ball.position = kickoffPoint.position;
            else
                ball.position = Vector3.zero;
            
        }

        if (ballRb != null)
        {
            ballRb.linearVelocity = Vector2.zero;
            ballRb.angularVelocity = 0f;
        }
    }

    public void ResetGame()
    {
        ResetBall();
        ResetPlayers();
    }

    public void ResetPlayers()
    {
        if (players == null || players.Length == 0)
            return;

        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] == null)
                continue;

            // Place each player at a kickoff point if available.
            if (playerKickoffPoints != null && i < playerKickoffPoints.Length && playerKickoffPoints[i] != null)
            {
                players[i].position = playerKickoffPoints[i].position;
            }

            // Stop all movement
            var rb = players[i].GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.linearVelocity = Vector2.zero;
        }
    }

    public void GoalScored(bool teamA)
    {
        if (gameManager != null)
            gameManager.GoalScored(teamA);

        // Reset the ball and players after a goal event.
        ResetBall();
        ResetPlayers();
    }

    public void SetupKickoff()
    {
        ResetBall();
        ResetPlayers();
        if (kickoffPoint != null && ball != null)
            ball.position = kickoffPoint.position;
    }
}

