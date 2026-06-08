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
		rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, rb.linearVelocity * (1f - drag), Time.fixedDeltaTime);

		// clamp speed
		if (rb.linearVelocity.sqrMagnitude > maxSpeed * maxSpeed)
		{
			rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
		}
	}

	public void ResetBall(Transform kickoffPoint = null)
	{
		if (kickoffPoint != null)
			transform.position = kickoffPoint.position;
		else
			transform.position = Vector3.zero;

		if (rb != null)
			rb.linearVelocity = Vector2.zero;
	}
}
