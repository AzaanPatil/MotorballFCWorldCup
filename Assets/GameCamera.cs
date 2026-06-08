using UnityEngine;

public class GameCamera : MonoBehaviour
{
    public Transform target; // The target the camera will follow
    public float smoothSpeed = 0.125f; // Smoothing speed for camera movement
    public Vector3 offset; // Offset from the target position
    public Vector2 minBounds;
    public Vector2 maxBounds;

    void LateUpdate()
    {
        if (target != null)
        {
            Vector3 desiredPosition = target.position + offset;
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);

            // Clamp for 2D usage
            smoothedPosition.x = Mathf.Clamp(smoothedPosition.x, minBounds.x, maxBounds.x);
            smoothedPosition.y = Mathf.Clamp(smoothedPosition.y, minBounds.y, maxBounds.y);

            transform.position = smoothedPosition;
        }
    }
}