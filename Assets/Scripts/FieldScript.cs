using UnityEngine;

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
        if (ball != null && ballRb == null)
            ballRb = ball.GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (ball == null)
            return;

        if (ball.position.x < leftBound || ball.position.x > rightBound || ball.position.y > topBound || ball.position.y < bottomBound)
        {
            ResetBall();
        }
    }

    void ResetBall()
    {
        if (ball != null)
            ball.position = Vector3.zero;

        if (ballRb != null)
            ballRb.linearVelocity = Vector2.zero;
    }

    public void ResetPlayers()
    {
        if (players == null || players.Length == 0)
            return;

        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] == null)
                continue;

            if (playerKickoffPoints != null && i < playerKickoffPoints.Length && playerKickoffPoints[i] != null)
            {
                players[i].position = playerKickoffPoints[i].position;
            }
            else
            {
                var ps = players[i].GetComponent<PlayerScript>();
                if (ps != null)
                    players[i].position = ps.initialPosition;
            }

            var rb = players[i].GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.linearVelocity = Vector2.zero;
        }
    }

    public void GoalScored(bool teamA)
    {
        if (gameManager != null)
            gameManager.GoalScored(teamA);

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

