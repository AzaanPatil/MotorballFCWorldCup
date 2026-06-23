using UnityEngine;

public class PlayerHighlight : MonoBehaviour
{
    public GameManager gameManager;
    public Vector3 offset = new Vector3(0f, -0.1f, 1f);

    void Start()
    {
        if (gameManager == null)
            gameManager = FindAnyObjectByType<GameManager>();
    }

    void LateUpdate()
    {
        if (gameManager == null || gameManager.activePlayer == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        transform.position = gameManager.activePlayer.transform.position + offset;
    }
}
