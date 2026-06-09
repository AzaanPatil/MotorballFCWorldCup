using UnityEngine;
using TMPro;

public class Scorebug : MonoBehaviour
{
    public GameManager gameManager;

    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timerText;

    void Update()
    {
        if (gameManager == null) return;

        // Score display
        scoreText.text = gameManager.teamAScore + " - " + gameManager.teamBScore;

        // Timer display
        float timeLeft = gameManager.gameDuration - gameManager.gameTime;
        int minutes = Mathf.FloorToInt(timeLeft / 60);
        int seconds = Mathf.FloorToInt(timeLeft % 60);

        timerText.text = minutes.ToString("00") + ":" + seconds.ToString("00");
    }
}