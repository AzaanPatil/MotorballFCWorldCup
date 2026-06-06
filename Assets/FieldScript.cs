using UnityEngine;

public class FieldScript : MonoBehaviour

    public float leftBound;
    public float rightBound;
    public float topBound;
    public float bottomBound;

    public Transform ball;
    public Rigidbody2D ballRb;
{
    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ballRb = ball.GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        if (ball.position.x < leftBound || ball.position.x > rightBound || ball.position.y > topBound || ball.position.y < bottomBound)
        {
            ResetBall();
        }
    }

    void ResetBall()
    {
        ball.position = Vector3.zero;
        ballRb.velocity = Vector2.zero;
    }

    void GoalScored()
    {
        
        ResetBall();
    }
}
