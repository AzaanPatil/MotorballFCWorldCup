using UnityEngine;
using UnityEngine.UI;

// Central game manager. Handles score, time, state, kickoff, and active player control.
public class GameManager : MonoBehaviour
{
    public enum GameState
    {
        Kickoff,
        Playing,
        Goal,
        Paused,
        GameOver
    }

    public TeamData teamA;
    public TeamData teamB;

    public bool teamCPUsHome = true;

    [Header("Game State")]
    public GameState currentState = GameState.Kickoff;
    public float gameTime;
    public float goalPauseDuration = 2.0f;

    [Header("Score")]
    public int teamAScore;
    public int teamBScore;
    public int winningScore = 5;
    public bool useScoreLimit = false;

    [Header("Ball")]
    public Transform ball;
    public Transform kickoffPoint;
    public Rigidbody2D ballRb;

    [Header("UI")]
    public Text scoreText;
    public Text messageText;
    public GameAudio audioManager;

    [Header("Active Player")]
    public Vehicle activePlayer;
    public Vehicle.Team playerTeam = Vehicle.Team.Friendly;

    private float goalTimer;

    void Start()
    {
        if (ball != null && ballRb == null)
            ballRb = ball.GetComponent<Rigidbody2D>();

        ResetGame();
        AssignTeamIDs();
        UpdateUI();
        SetMessage("Ready for kickoff");
    }

    void Update()
    {
        if (currentState == GameState.Playing)
        {
            gameTime += Time.deltaTime;
        }
        else if (currentState == GameState.Goal)
        {
            goalTimer += Time.deltaTime;
            if (goalTimer >= goalPauseDuration)
            {
                StartKickoff();
            }
        }
    }

    public void StartMatch()
    {
        ResetGame();
        currentState = GameState.Kickoff;
        SetMessage("Kickoff");
        StartKickoff();
    }

    public void StartKickoff()
    {
        ResetRound();
        currentState = GameState.Playing;
        goalTimer = 0f;
        SetMessage("Playing");
        if (audioManager != null)
            audioManager.PlayKickoff();
    }

    public void PauseGame()
    {
        if (currentState == GameState.Playing)
        {
            currentState = GameState.Paused;
            SetMessage("Paused");
        }
    }

    public void ResumeGame()
    {
        if (currentState == GameState.Paused)
        {
            currentState = GameState.Playing;
            SetMessage("Playing");
        }
    }

    public void EndGame()
    {
        currentState = GameState.GameOver;
        SetMessage("Game Over");
    }

    public void GoalScored(bool teamAScored)
    {
        if (currentState == GameState.GameOver)
            return;

        if (teamAScored)
            teamAScore++;
        else
            teamBScore++;

        UpdateUI();
        currentState = GameState.Goal;
        goalTimer = 0f;
        SetMessage($"Goal! {(teamAScored ? "Team A" : "Team B")} scored");
        if (audioManager != null)
            audioManager.PlayGoal();

        if (useScoreLimit && (teamAScore >= winningScore || teamBScore >= winningScore))
        {
            EndGame();
        }
    }

    public void ResetRound()
    {
        if (ball != null)
        {
            ball.position = (kickoffPoint != null) ? kickoffPoint.position : Vector3.zero;
        }

        if (ballRb != null)
            ballRb.linearVelocity = Vector2.zero;
    }

    public void ResetGame()
    {
        teamAScore = 0;
        teamBScore = 0;
        gameTime = 0f;
        goalTimer = 0f;
        currentState = GameState.Kickoff;
        ResetRound();
        UpdateUI();
        SetMessage("Ready");
    }

    private void UpdateUI()
    {
        if (scoreText != null)
            scoreText.text = $"{teamAScore} - {teamBScore}";
    }

    private void SetMessage(string message)
    {
        if (messageText != null)
            messageText.text = message;
    }

    public void SetActivePlayer(Vehicle player)
    {
        if (activePlayer == player)
            return;

        activePlayer = player;
    }

    public void ApplyTeamColors()
    {
        Color teamAColor = teamAIsHome ? teamA.teamColor : teamB.teamColor;
        Color teamBColor = teamAIsHome ? teamB.teamColor : teamA.team;

        playerVehicle.GetComponent<VehicleAppearance>().ApplyColor(teamAColor);
        cpuVehicle.GetComponent<VehicleAppearance>().ApplyColor(teamBColor);
    }

    void AssignTeamIDs()
    {
        // Assign teams to all vehicles based on playerTeam
        Vehicle[] allVehicles = FindObjectsOfType<Vehicle>();
        foreach (var vehicle in allVehicles)
        {
            // Determine if this vehicle should be on the player's team or opposing team
            // PlayerVehicle and activePlayer are on the player's team, rest are opponents
            if (vehicle == activePlayer)
            {
                vehicle.team = playerTeam;
            }
            else if (vehicle.GetComponent<PlayerVehicle>() != null)
            {
                vehicle.team = playerTeam;  // Friendly AI on same team
            }
            else
            {
                vehicle.team = (playerTeam == Vehicle.Team.Friendly) ? Vehicle.Team.Opponent : Vehicle.Team.Friendly;  // Opposing team
            }
        }
    }

    public void SetPlayerTeam(Vehicle.Team selectedTeam)
    {
        playerTeam = selectedTeam;
        AssignTeamIDs();
    }
}
