using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class EndScreen : MonoBehaviour
{
    [Header("Panel")]
    public GameObject panel;

    [Header("Result Text")]
    public TextMeshProUGUI winnerText;
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI teamANameText;
    public TextMeshProUGUI teamBNameText;

    [Header("Flags")]
    public Image teamAFlagImage;
    public Image teamBFlagImage;

    [Header("Stats")]
    public TextMeshProUGUI teamAGoalsText;
    public TextMeshProUGUI teamBGoalsText;
    public TextMeshProUGUI teamASavesText;
    public TextMeshProUGUI teamBSavesText;
    public TextMeshProUGUI teamAHitsText;
    public TextMeshProUGUI teamBHitsText;

    void Awake()
    {
        if (panel != null) panel.SetActive(false);
    }

    public void Show(GameManager gm)
    {
        if (panel != null) panel.SetActive(true);

        // Winner
        if (winnerText != null)
        {
            if      (gm.teamAScore > gm.teamBScore) winnerText.text = $"{gm.teamA.teamName} WINS!";
            else if (gm.teamBScore > gm.teamAScore) winnerText.text = $"{gm.teamB.teamName} WINS!";
            else                                    winnerText.text = "DRAW!";
        }

        if (finalScoreText != null)
            finalScoreText.text = $"{gm.teamAScore}  —  {gm.teamBScore}";

        if (teamANameText != null) teamANameText.text = gm.teamA.teamName;
        if (teamBNameText != null) teamBNameText.text = gm.teamB.teamName;

        if (teamAFlagImage != null && gm.teamA.teamFlag != null)
            teamAFlagImage.sprite = gm.teamA.teamFlag;
        if (teamBFlagImage != null && gm.teamB.teamFlag != null)
            teamBFlagImage.sprite = gm.teamB.teamFlag;

        // Stats
        if (MatchStats.Instance != null)
        {
            var s = MatchStats.Instance;
            SetText(teamAGoalsText, s.teamAGoals);
            SetText(teamBGoalsText, s.teamBGoals);
            SetText(teamASavesText, s.teamASaves);
            SetText(teamBSavesText, s.teamBSaves);
            SetText(teamAHitsText,  s.teamAHits);
            SetText(teamBHitsText,  s.teamBHits);
        }
    }

    void SetText(TextMeshProUGUI t, int value)
    {
        if (t != null) t.text = value.ToString();
    }

    public void PlayAgain()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMainMenu()
    {
        SceneManager.LoadScene("Main Menu");
    }
}
