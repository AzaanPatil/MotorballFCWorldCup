using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Scorebug : MonoBehaviour
{
    public GameManager gameManager;

    [Header("TeamName UI")]
    public TextMeshProUGUI teamANameText;
    public TextMeshProUGUI teamBNameText;
    public Image teamAFlagImage;
    public Image teamBFlagImage;

    [Header("Score UI")]
    public TextMeshProUGUI teamAScoreText;
    public TextMeshProUGUI teamBScoreText;
    public Image teamAScoreFlash;
    public Image teamBScoreFlash;

    [Header("Timer UI")]
    public TextMeshProUGUI timerText;

    
    void Start()
    {
        if (gameManager == null)
        {
            // Only accept a GameManager that lives in the same scene as this ScoreBug.
            // FindAnyObjectByType searches all loaded scenes, so without this check the
            // Main Menu ScoreBug would find the GameScene's GameManager when both are open.
            foreach (var gm in FindObjectsByType<GameManager>(FindObjectsInactive.Exclude))
            {
                if (gm.gameObject.scene == gameObject.scene)
                {
                    gameManager = gm;
                    break;
                }
            }
        }

        if (gameManager == null)
            gameObject.SetActive(false);
    }

    string GetAbbreviation(string name)
    {
        if (string.IsNullOrEmpty(name))
            return "";

        name = name.ToUpper();

        return name.Length <= 3 ? name : name.Substring(0, 3);
    }


    void Update()
    {
        if (gameManager == null) return;

        teamANameText.text = GetAbbreviation(gameManager.teamA.teamName);
        teamBNameText.text = GetAbbreviation(gameManager.teamB.teamName);

        if (teamAFlagImage != null && gameManager.teamA.teamFlag != null)
            teamAFlagImage.sprite = gameManager.teamA.teamFlag;

        if (teamBFlagImage != null && gameManager.teamB.teamFlag != null)
            teamBFlagImage.sprite = gameManager.teamB.teamFlag;

        if (gameManager.currentState == GameManager.GameState.GameOver)
        {
            timerText.text = ""; // hide timer
            return;
        }

        // Update scores separately
        teamAScoreText.text = gameManager.teamAScore.ToString();
        teamBScoreText.text = gameManager.teamBScore.ToString();

        // Timer (elapsed time)
        float timeElapsed = gameManager.gameTime;

        int minutes = Mathf.FloorToInt(timeElapsed / 60);
        int seconds = Mathf.FloorToInt(timeElapsed % 60);

        timerText.text = minutes.ToString("00") + ":" + seconds.ToString("00");
    }
}