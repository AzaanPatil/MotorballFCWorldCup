using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public enum GameState { Kickoff, Playing, Goal, Paused, GameOver }

    public enum GameMode { OneVOne, TwoVTwo, ThreeVThree, FiveVFive }

    [System.Serializable]
    public class GameModeSettings
    {
        public GameMode mode;
        public int playersPerTeam;
        public float matchDuration;
        public bool useGoalie;
    }

    // One entry per mode. Drag each mode's SpawnPoints child object's spawn Transforms here.
    [System.Serializable]
    public class TeamSpawnGroup
    {
        public GameMode mode;
        public Transform[] teamASpawns;
        public Transform[] teamBSpawns;
        public Transform teamAGoalieSpawn;
        public Transform teamBGoalieSpawn;
    }

    [Header("Game Mode")]
    public GameMode currentMode;
    public GameModeSettings[] gameModeSettings;

    [Header("Teams")]
    public TeamData teamA;
    public TeamData teamB;

    // Assign Player1–Player5 in order for each team. The first N are activated for the selected mode.
    [Header("Team A Vehicles")]
    public GameObject[] teamAPlayers;
    public GameObject teamAGoalie;

    [Header("Team B Vehicles")]
    public GameObject[] teamBPlayers;
    public GameObject teamBGoalie;

    // One entry per mode — drag the spawn-point Transforms for each mode here.
    [Header("Spawn Points (per mode)")]
    public TeamSpawnGroup[] spawnGroups;

    [Header("Goals (for AI targeting)")]
    // Assign the goal that Team A shoots AT (i.e. GoalB — Team B's net)
    public Transform teamAShootsAt;
    // Assign the goal that Team B shoots AT (i.e. GoalA — Team A's net)
    public Transform teamBShootsAt;

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
    public float switchCooldown = 0.3f;

    private VehicleController[] allVehicles;
    private float goalTimer;
    private float lastSwitchTime;

    void Start()
    {
        currentMode = MatchSettings.selectedMode;

        if (ball != null && ballRb == null)
            ballRb = ball.GetComponent<Rigidbody2D>();

        // Step 1: activate/deactivate vehicles for this mode
        ConfigureMatch();

        // Step 2: collect only the now-active VehicleControllers
        allVehicles = FindObjectsByType<VehicleController>();

        // Step 3: auto-assign each vehicle's opponent goal based on team
        foreach (var v in allVehicles)
            v.opponentGoal = (v.team == VehicleController.Team.Friendly) ? teamAShootsAt : teamBShootsAt;

        // Step 4: reset scores, positions, and ball
        teamAScore = 0;
        teamBScore = 0;
        gameTime = 0f;
        goalTimer = 0f;
        ResetRound();
        UpdateUI();

        // Step 5: give player control to the first active friendly non-goalie vehicle
        foreach (var v in allVehicles)
            v.SetPlayerControlled(false);

        foreach (var v in allVehicles)
        {
            if (v.team == playerTeam && v.gameObject.activeSelf && !v.isGoalie)
            {
                SetActivePlayer(v);
                break;
            }
        }

        currentState = GameState.Playing;
        SetMessage("Playing");
    }

    void Update()
    {
        if (currentState == GameState.Playing)
        {
            gameTime += Time.deltaTime;
            if (gameTime >= gameDuration)
                EndGame();
        }
        else if (currentState == GameState.Goal)
        {
            goalTimer += Time.deltaTime;
            if (goalTimer >= goalPauseDuration)
                StartKickoff();
        }

        if (Input.GetKeyDown(KeyCode.Tab))
            SwitchPlayer();

        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetGame();
            StartKickoff();
        }
    }

    // ── Public match flow ───────────────────────────────────────────────────

    public void StartMatch()
    {
        ResetGame();
        StartKickoff();
    }

    public void StartKickoff()
    {
        ResetRound();
        currentState = GameState.Playing;
        goalTimer = 0f;
        SetMessage("Playing");
        if (audioManager != null) audioManager.PlayKickoff();
    }

    public void PauseGame()
    {
        if (currentState != GameState.Playing) return;
        currentState = GameState.Paused;
        SetMessage("Paused");
    }

    public void ResumeGame()
    {
        if (currentState != GameState.Paused) return;
        currentState = GameState.Playing;
        SetMessage("Playing");
    }

    public void EndGame()
    {
        currentState = GameState.GameOver;
        if (teamAScore > teamBScore)      SetMessage(teamA.teamName + " WINS!!!");
        else if (teamBScore > teamAScore) SetMessage(teamB.teamName + " WINS!!!");
        else                              SetMessage("DRAW!");
    }

    public void GoalScored(bool teamAScored)
    {
        if (currentState == GameState.GameOver) return;

        if (teamAScored) teamAScore++; else teamBScore++;

        UpdateUI();
        currentState = GameState.Goal;
        goalTimer = 0f;
        SetMessage($"Goal! {(teamAScored ? teamA.teamName : teamB.teamName)} scored!");
        if (audioManager != null) audioManager.PlayGoal();

        if (useScoreLimit && (teamAScore >= winningScore || teamBScore >= winningScore))
            EndGame();
    }

    public void ResetRound()
    {
        if (ball != null)
            ball.position = kickoffPoint != null ? kickoffPoint.position : Vector3.zero;

        if (ballRb != null)
        {
            ballRb.linearVelocity = Vector2.zero;
            ballRb.angularVelocity = 0f;
        }

        SpawnPlayers();
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

    // ── Player control ──────────────────────────────────────────────────────

    public void SetActivePlayer(VehicleController player)
    {
        if (activePlayer == player) return;
        if (activePlayer != null) activePlayer.SetPlayerControlled(false);
        activePlayer = player;
        if (activePlayer != null) activePlayer.SetPlayerControlled(true);
    }

    public void SetPlayerTeam(VehicleController.Team selectedTeam)
    {
        playerTeam = selectedTeam;
        if (allVehicles == null || allVehicles.Length == 0)
            allVehicles = FindObjectsByType<VehicleController>();
    }

    void SwitchPlayer()
    {
        if (allVehicles == null) return;
        if (Time.time - lastSwitchTime < switchCooldown) return;

        VehicleController best = null;
        float closestDist = float.MaxValue;

        foreach (var v in allVehicles)
        {
            if (v.team != playerTeam || v == activePlayer || v.isGoalie) continue;
            float d = v.DistanceToBall();
            if (d < closestDist) { closestDist = d; best = v; }
        }

        if (best != null)
        {
            SetActivePlayer(best);
            lastSwitchTime = Time.time;
        }
    }

    // ── Match configuration ─────────────────────────────────────────────────

    GameModeSettings GetCurrentModeSettings()
    {
        if (gameModeSettings != null)
            foreach (var s in gameModeSettings)
                if (s.mode == currentMode) return s;

        int perTeam = currentMode switch
        {
            GameMode.OneVOne     => 1,
            GameMode.TwoVTwo     => 2,
            GameMode.ThreeVThree => 3,
            GameMode.FiveVFive   => 5,
            _                    => 1
        };
        Debug.LogWarning($"gameModeSettings not configured for {currentMode} — using defaults ({perTeam} per team). Configure in Inspector to override.");
        return new GameModeSettings { mode = currentMode, matchDuration = gameDuration, useGoalie = false, playersPerTeam = perTeam };
    }

    TeamSpawnGroup GetSpawnGroup()
    {
        if (spawnGroups == null) return null;
        foreach (var g in spawnGroups)
            if (g.mode == currentMode) return g;
        return null;
    }

    void ConfigureMatch()
    {
        GameModeSettings settings = GetCurrentModeSettings();
        int needed = settings.playersPerTeam;

        for (int i = 0; i < teamAPlayers.Length; i++)
            if (teamAPlayers[i] != null) teamAPlayers[i].SetActive(i < needed);

        if (teamAGoalie != null) teamAGoalie.SetActive(settings.useGoalie);

        for (int i = 0; i < teamBPlayers.Length; i++)
            if (teamBPlayers[i] != null) teamBPlayers[i].SetActive(i < needed);

        if (teamBGoalie != null) teamBGoalie.SetActive(settings.useGoalie);

        Debug.Log($"Match configured: {currentMode} — {needed}v{needed}, goalie={settings.useGoalie}");
    }

    void SpawnPlayers()
    {
        GameModeSettings settings = GetCurrentModeSettings();
        int needed = settings.playersPerTeam;
        TeamSpawnGroup group = GetSpawnGroup();

        Transform[] spawnA        = group?.teamASpawns      ?? System.Array.Empty<Transform>();
        Transform[] spawnB        = group?.teamBSpawns      ?? System.Array.Empty<Transform>();
        Transform   goalieSpawnA  = group?.teamAGoalieSpawn;
        Transform   goalieSpawnB  = group?.teamBGoalieSpawn;

        for (int i = 0; i < teamAPlayers.Length && i < needed; i++)
            PlaceVehicle(teamAPlayers[i], spawnA, i);

        if (settings.useGoalie)
            PlaceVehicle(teamAGoalie, goalieSpawnA);

        for (int i = 0; i < teamBPlayers.Length && i < needed; i++)
            PlaceVehicle(teamBPlayers[i], spawnB, i);

        if (settings.useGoalie)
            PlaceVehicle(teamBGoalie, goalieSpawnB);
    }

    void PlaceVehicle(GameObject go, Transform[] points, int index)
    {
        if (go == null || !go.activeSelf) return;
        Transform pt = (points != null && index < points.Length) ? points[index] : null;
        PlaceAt(go, pt);
    }

    void PlaceVehicle(GameObject go, Transform point)
    {
        if (go == null || !go.activeSelf) return;
        PlaceAt(go, point);
    }

    void PlaceAt(GameObject go, Transform point)
    {
        if (point != null)
            go.transform.SetPositionAndRotation(point.position, point.rotation);

        if (go.TryGetComponent<Rigidbody2D>(out var rb))
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    // ── UI helpers ──────────────────────────────────────────────────────────

    void UpdateUI()
    {
        if (scoreText != null)
            scoreText.text = $"{teamAScore} - {teamBScore}";
    }

    void SetMessage(string message)
    {
        if (messageText != null)
            messageText.text = message;
    }
}
