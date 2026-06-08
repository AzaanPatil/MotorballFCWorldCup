using UnityEngine;
using UnityEngine.UI;

public class TeamSelector : MonoBehaviour
{
    public GameManager gameManager;
    public GameObject selectionPanel;
    public Button homeButton;
    public Button awayButton;
    public Text instructionText;

    void Start()
    {
        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();

        if (selectionPanel != null)
            selectionPanel.SetActive(true);

        if (homeButton != null)
            homeButton.onClick.AddListener(() => SelectTeam(Vehicle.Team.Friendly));

        if (awayButton != null)
            awayButton.onClick.AddListener(() => SelectTeam(Vehicle.Team.Opponent));

        if (instructionText != null)
            instructionText.text = "Choose your team:\nHome or Away";
    }

    void SelectTeam(Vehicle.Team selectedTeam)
    {
        if (gameManager != null)
        {
            gameManager.SetPlayerTeam(selectedTeam);
        }

        if (selectionPanel != null)
            selectionPanel.SetActive(false);

        if (instructionText != null)
            instructionText.text = (selectedTeam == Vehicle.Team.Friendly) ? "You are HOME (Friendly)" : "You are AWAY (Opponent)";

        // Start the match after a brief delay
        Invoke(nameof(StartMatch), 1f);
    }

    void StartMatch()
    {
        if (gameManager != null)
            gameManager.StartMatch();
    }
}
