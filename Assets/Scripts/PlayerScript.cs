using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerScript : MonoBehaviour
{
	public float maxSpeed = 6f;
	public float acceleration = 30f;
	public string horizontalAxis = "Horizontal";
	public string verticalAxis = "Vertical";

	[HideInInspector]
	public Vector3 initialPosition;

	Rigidbody2D rb;

	void Awake()
	{
		rb = GetComponent<Rigidbody2D>();
		initialPosition = transform.position;
	}

	void FixedUpdate()
	{
		Vector2 input = new Vector2(Input.GetAxisRaw(horizontalAxis), Input.GetAxisRaw(verticalAxis));

		if (input.sqrMagnitude > 0.01f)
		{
			Vector2 target = input.normalized * maxSpeed;
			rb.velocity = Vector2.MoveTowards(rb.velocity, target, acceleration * Time.fixedDeltaTime);
		}
		else
		{
			// simple damping when no input
			rb.velocity = Vector2.MoveTowards(rb.velocity, Vector2.zero, acceleration * 0.5f * Time.fixedDeltaTime);
		}
	}
}

