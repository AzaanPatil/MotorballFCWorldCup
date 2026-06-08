using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BallScript : MonoBehaviour
{
	public float maxSpeed = 15f;
	public float drag = 0.5f;

	Rigidbody2D rb;

	void Awake()
	{
		rb = GetComponent<Rigidbody2D>();
	}

	void FixedUpdate()
	{
		if (rb == null)
			return;

		// apply simple drag
		rb.velocity = Vector2.Lerp(rb.velocity, rb.velocity * (1f - drag), Time.fixedDeltaTime);

		// clamp speed
		if (rb.velocity.sqrMagnitude > maxSpeed * maxSpeed)
		{
			rb.velocity = rb.velocity.normalized * maxSpeed;
		}
	}

	public void ResetBall(Transform kickoffPoint = null)
	{
		if (kickoffPoint != null)
			transform.position = kickoffPoint.position;
		else
			transform.position = Vector3.zero;

		if (rb != null)
			rb.velocity = Vector2.zero;
	}
}
