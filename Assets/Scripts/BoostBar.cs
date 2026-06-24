using UnityEngine;
using UnityEngine.UI;

public class BoostBar : MonoBehaviour
{
    public GameManager gameManager;
    public Slider slider;
    public Image fillImage;

    [Header("Colors")]
    public Color fullColor  = new Color(0f, 0.8f, 1f);
    public Color emptyColor = new Color(1f, 0.2f, 0f);

    void Awake()
    {
        if (gameManager == null)
            gameManager = FindAnyObjectByType<GameManager>();
    }

    void Update()
    {
        bool active = gameManager != null && gameManager.IsGameActive && gameManager.activePlayer != null;

        if (slider != null && slider.gameObject.activeSelf != active)
            slider.gameObject.SetActive(active);

        if (!active) return;

        float ratio = gameManager.activePlayer.BoostRatio;

        if (slider    != null) slider.value    = ratio;
        if (fillImage != null) fillImage.color = Color.Lerp(emptyColor, fullColor, ratio);
    }
}
