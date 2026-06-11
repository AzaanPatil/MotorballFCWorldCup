using UnityEngine;
using UnityEngine.UI;

// Central game manager. Handles score, time, state, kickoff, and active player control.
// Uses VehicleController for dynamic player switching based on proximity to ball.
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

    public Transform teamASpawn;
    public Transform teamBSpawn;

    [Header("Game State")]
    public GameState currentState = GameState.Kickoff;
    public float gameTime;
    public float goalPauseDuration = 2.0f;
    public float gameDuration = 120f;

    [Header("Score")]
    public int teamAScore;
    public int teamBScore;
    public int winningScore;
    public bool useScoreLimit = false;

    [Header("Ball")]
    public Transform ball;
    public Transform kickoffPoint;
    public Rigidbody2D ballRb;

    [Header("UI")]
    public Text scoreText;
    public Text messageText;
    public GameAudio audioManager;

    [Header("Player Control")]
    public VehicleController.Team playerTeam = VehicleController.Team.Friendly;

    public VehicleController activePlayer;
    private VehicleController[] allVehicles;
    private float goalTimer;

    void Start()
    { 
        if (ball != null && ballRb == null)
            ballRb = ball.GetComponent<Rigidbody2D>();

        // Find all vehicles at startup
        allVehicles = FindObjectsOfType<VehicleController>();
        
        ResetGame();
        StartKickoff();
        AssignTeams();
        UpdateUI();
        SetMessage("Ready for kickoff");
    }

    void Update()
    {
        if (currentState == GameState.Playing)
        {
            gameTime += Time.deltaTime;
            
            // Auto-switch player control to the closest friendly vehicle
            //UpdateActivePlayer();

            if (gameTime >= gameDuration)
            {
                EndGame();
            }
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

        string winnerMessage;

        if (teamAScore > teamBScore)
            winnerMessage = teamA.teamName + " WINS!!!";
        else if (teamBScore > teamAScore)
            winnerMessage = teamB.teamName + " WINS!!!";
        else
            winnerMessage = "DRAW!";

        SetMessage(winnerMessage);
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
        // Reset ball
    if (ball != null)
    {
        ball.position = (kickoffPoint != null) ? kickoffPoint.position : Vector3.zero;
    }

    if (ballRb != null)
        ballRb.linearVelocity = Vector2.zero;

    // Reset players
    VehicleController[] vehicles = FindObjectsOfType<VehicleController>();

    foreach (var v in vehicles)
    {
        if (v.team == VehicleController.Team.Friendly && teamASpawn != null)
            v.transform.position = teamASpawn.position;

        else if (v.team == VehicleController.Team.Opponent && teamBSpawn != null)
            v.transform.position = teamBSpawn.position;

        Rigidbody2D rb = v.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }
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

    public void SetActivePlayer(VehicleController player)
    {
        if (activePlayer == player)
            return;

        // Disable control on previous player
        //if (activePlayer != null)
        //    activePlayer.SetPlayerControlled(false);

        // Enable control on new player
        activePlayer = player;
        if (activePlayer != null)
            activePlayer.SetPlayerControlled(true);
    }

    /// Automatically finds and switches to the closest friendly vehicle to the ball.
    private void UpdateActivePlayer()
    {
        if (allVehicles == null || allVehicles.Length == 0)
            return;

        VehicleController closestFriendly = null;
        float closestDistance = float.MaxValue;

        foreach (var vehicle in allVehicles)
        {
            // Only consider friendly team vehicles
            if (vehicle.team != playerTeam)
                continue;

            float distance = vehicle.DistanceToBall();
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestFriendly = vehicle;
            }
        }

        // Switch to closest friendly vehicle
        if (closestFriendly != null && closestFriendly != activePlayer)
        {
            SetActivePlayer(closestFriendly);
        }
    }

    /// Assigns team affiliations to all vehicles.
    void AssignTeams()
    {
        if (allVehicles == null || allVehicles.Length == 0)
            allVehicles = FindObjectsOfType<VehicleController>();

        foreach (var vehicle in allVehicles)
        {
            // Assign teams: half friendly, half opponent
            // Or use a more sophisticated system based on initial setup
            //if (vehicle.team == playerTeam)
            //{
            //    vehicle.SetPlayerControlled(false); // AI by default
            //}
        }

        // Activate the first friendly vehicle
        //foreach (var vehicle in allVehicles)
        //{
        //    if (vehicle.team == playerTeam)
        //    {
        //        SetActivePlayer(vehicle);
        //        break;
        //    }
        //}
    }

    public void SetPlayerTeam(VehicleController.Team selectedTeam)
    {
        playerTeam = selectedTeam;
        AssignTeams();
    }
}
