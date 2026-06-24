using UnityEngine;

public class PlayerHighlight : MonoBehaviour
{
    public GameManager gameManager;
    public Vector3 offset = new Vector3(0f, 0f, 0f);

    private SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (gameManager == null)
            gameManager = FindAnyObjectByType<GameManager>();
    }

    void LateUpdate()
    {
        bool hasTarget = gameManager != null && gameManager.activePlayer != null;

        if (sr != null) sr.enabled = hasTarget;

        if (hasTarget)
            transform.position = gameManager.activePlayer.transform.position + offset;
    }
}
