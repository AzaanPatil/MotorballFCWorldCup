using UnityEngine;

public class GameCamera : MonoBehaviour
{
    public Transform target; // Assign the target Transform here
    public float smoothSpeed = 5f;

    private GameManager gameManager;

    void Start()
    {
        gameManager = FindAnyObjectByType<GameManager>();
    }

    void LateUpdate()
    {
        if (gameManager == null || target == null) return;
        if (gameManager.activePlayer == null) return;

        Vector3 midpoint = (target.position + gameManager.activePlayer.transform.position) / 2f;
        Vector3 desired = new(midpoint.x, midpoint.y, -10f);
        transform.position = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);
    }
}
