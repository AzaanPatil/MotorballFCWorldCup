using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BallScript : MonoBehaviour
{
	public float maxSpeed = 15f;
	public float drag = 0.5f;
	public GameAudio gameAudio;

	Rigidbody2D rb;

	void Awake()
	{
		// Cache the rigidbody reference on startup.
		rb = GetComponent<Rigidbody2D>();
	}

	void FixedUpdate()
	{
		if (rb == null)
			return;

		// Apply basic drag so the ball slows over time.
		rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, rb.linearVelocity * (1f - drag), Time.fixedDeltaTime);

		// Prevent the ball from exceeding the configured top speed.
		if (rb.linearVelocity.sqrMagnitude > maxSpeed * maxSpeed)
		{
			rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
		}
	}

	void OnCollisionEnter2D(Collision2D col)
	{
		if (gameAudio != null) gameAudio.PlayBallHit();
	}

	public void ResetBall(Transform kickoffPoint = null)
	{
		rb.angularVelocity = 0f;
		
		// Reset the ball position and stop all movement.
		if (kickoffPoint != null)
			transform.position = kickoffPoint.position;
		else
			transform.position = Vector3.zero;

		if (rb != null)
			rb.linearVelocity = Vector2.zero;
	}
}
