using UnityEngine;
using TMPro;

public class Scorebug : MonoBehaviour
{
    public GameManager gameManager;

    [Header("TeamName UI")]
    public TextMeshProUGUI teamANameText;
    public TextMeshProUGUI teamBNameText;
    public Sprite teamAFlag;
    public Sprite teamBFlag;

    [Header("Score UI")]
    public TextMeshProUGUI teamAScoreText;
    public TextMeshProUGUI teamBScoreText;

    [Header("Timer UI")]
    public TextMeshProUGUI timerText;

    
    void Start()
    {
        if (gameManager == null)
            gameManager = FindAnyObjectByType<GameManager>();

        // Hide entirely in scenes that have no GameManager (e.g. main menu)
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

        //Displays team names
        teamANameText.text = GetAbbreviation(gameManager.teamA.teamName);
        teamBNameText.text = GetAbbreviation(gameManager.teamB.teamName);

        //Show team flags under team names
        if (gameManager.teamA.teamFlag != null)
            teamANameText.text = $"<sprite name=\"{gameManager.teamA.teamFlag.name}\" tint=1>{teamANameText.text}";
        
        if (gameManager.teamB.teamFlag != null)
            teamBNameText.text = $"<sprite name=\"{gameManager.teamB.teamFlag.name}\" tint=1>{teamBNameText.text}";

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