using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerScript : MonoBehaviour
{
	// Max player speed and how quickly velocity moves toward target.
	public float maxSpeed = 6f;
	public float acceleration = 30f;
	public string horizontalAxis = "Horizontal";
	public string verticalAxis = "Vertical";

	[HideInInspector]
	public Vector3 initialPosition;

	Rigidbody2D rb;

	void Awake()
	{
		// Cache the rigidbody and record the starting position for resets.
		rb = GetComponent<Rigidbody2D>();
		initialPosition = transform.position;
	}

	void FixedUpdate()
	{
		Vector2 input = new Vector2(Input.GetAxisRaw(horizontalAxis), Input.GetAxisRaw(verticalAxis));

		if (input.sqrMagnitude > 0.01f)
		{
			Vector2 target = input.normalized * maxSpeed;
			rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, target, acceleration * Time.fixedDeltaTime);
		}
		else
		{
			// Fade velocity smoothly when no input is provided.
			rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, Vector2.zero, acceleration * 0.5f * Time.fixedDeltaTime);
		}
	}
}

