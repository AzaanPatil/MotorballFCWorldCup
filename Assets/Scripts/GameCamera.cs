using UnityEngine;

// Smooth 2D camera follow with bounding clamps.
public class GameCamera : MonoBehaviour
{
    public Transform target; // The target the camera will follow
    public float smoothSpeed = 5f; // Smoothing speed for camera movement
    public Vector3 offset; // Offset from the target position
    public Vector2 minBounds;
    public Vector2 maxBounds;

    void LateUpdate()
    {
        if (target == null) return;

        Transform player = FindObjectOfType<VehicleController>().transform;

        Vector3 midpoint = (target.position + player.position) / 2f;

        Vector3 desiredPosition = new Vector3(
            midpoint.x,
            midpoint.y,
            -10f
        );

        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            5f * Time.deltaTime
        );
    }
}