using System.Collections;
using System.Collections.Generic;
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
        [Tooltip("Position assigned to each player slot by index — must match playersPerTeam length")]
        public VehicleController.PlayerPosition[] positions;
    }

    [Header("Game Mode")]
    public GameMode currentMode;
    public GameModeSettings[] gameModeSettings;

    [Header("Teams")]
    public TeamData teamA;
    public TeamData teamB;

    // ── Auto-discovery (drag the two parent GameObjects — everything else is found automatically) ──
    [Header("Auto-Discovery")]
    [Tooltip("Drag 'Gameplay/TeamA' here. Children are sorted automatically: the one with isGoalie=true becomes the goalie, the rest become players.")]
    public Transform teamAContainer;
    [Tooltip("Drag 'Gameplay/TeamB' here.")]
    public Transform teamBContainer;
    [Tooltip("Drag the 'SpawnPoints' root here. Children named 'OneVOne', 'TwoVTwo', etc. are auto-read, and within them 'TeamAPlayer1', 'TeamAGoalie', etc. must match your existing naming.")]
    public Transform spawnPointsRoot;

    [Header("Goals (for AI targeting)")]
    [Tooltip("The goal Team A shoots AT — i.e. Team B's net.")]
    public Transform teamAShootsAt;
    [Tooltip("The goal Team B shoots AT — i.e. Team A's net.")]
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

    // Auto-populated at runtime. Override in Inspector only if auto-discovery fails.
    [Header("Vehicle Arrays (auto-filled — override if needed)")]
    public GameObject[] teamAPlayers;
    public GameObject   teamAGoalie;
    public GameObject[] teamBPlayers;
    public GameObject   teamBGoalie;

    private VehicleController[] allVehicles;
    private float goalTimer;
    private float lastSwitchTime;

    void Awake()
    {
        // Pre-populate sensible defaults so Inspector never needs to be touched for basic modes
        if (gameModeSettings == null || gameModeSettings.Length == 0)
        {
            var S = VehicleController.PlayerPosition.Striker;
            var M = VehicleController.PlayerPosition.Midfielder;
            var D = VehicleController.PlayerPosition.Defender;

            gameModeSettings = new GameModeSettings[]
            {
                new() { mode = GameMode.OneVOne,     playersPerTeam = 1, matchDuration = 120f, useGoalie = false,
                        positions = new[] { S } },
                new() { mode = GameMode.TwoVTwo,     playersPerTeam = 2, matchDuration = 120f, useGoalie = false,
                        positions = new[] { S, S } },
                new() { mode = GameMode.ThreeVThree, playersPerTeam = 3, matchDuration = 120f, useGoalie = false,
                        positions = new[] { S, S, M } },
                new() { mode = GameMode.FiveVFive,   playersPerTeam = 5, matchDuration = 120f, useGoalie = true,
                        positions = new[] { S, S, M, D, D } },
            };
        }
    }

    void Start()
    {
        Debug.Log("[GameManager] Start() called.");

        if (FindAnyObjectByType<MenuManager>() != null)
        {
            Debug.Log("[GameManager] MenuManager found — skipping init (Main Menu is loaded).");
            foreach (var bug in FindObjectsByType<Scorebug>(FindObjectsInactive.Include))
                if (bug.gameObject.scene == gameObject.scene)
                    bug.gameObject.SetActive(false);
            return;
        }

        Debug.Log("[GameManager] No MenuManager — running InitializeGame().");
        InitializeGame();
    }

    void InitializeGame()
    {
        currentMode = MatchSettings.selectedMode;
        playerTeam  = MatchSettings.playerTeam;

        ApplyCountryData();

        if (ball != null && ballRb == null)
            ballRb = ball.GetComponent<Rigidbody2D>();

        AutoDiscoverTeams();
        ConfigureMatch();

        teamAScore = 0;
        teamBScore = 0;
        gameTime   = 0f;
        goalTimer  = 0f;
        UpdateUI();

        StartCoroutine(LateInitialize());
    }

    void ApplyCountryData()
    {
        if (CountryManager.Instance == null)
        {
            Debug.LogWarning("[GameManager] CountryManager not found — team names/colors will use Inspector defaults.");
            return;
        }

        var homeData = CountryManager.Instance.GetCountryData(MatchSettings.homeCountry);
        var awayData = CountryManager.Instance.GetCountryData(MatchSettings.awayCountry);

        if (homeData != null)
        {
            teamA.teamName  = homeData.abbreviation;
            teamA.teamFlag  = homeData.flag;
            teamA.homeColor = homeData.homeColor;
            Debug.Log($"[GameManager] Home: country={homeData.country} abbr={homeData.abbreviation} flag={( homeData.flag != null ? homeData.flag.name : "NULL")}");
        }

        if (awayData != null)
        {
            teamB.teamName  = awayData.abbreviation;
            teamB.teamFlag  = awayData.flag;
            teamB.homeColor = awayData.homeColor;
            Debug.Log($"[GameManager] Away: country={awayData.country} abbr={awayData.abbreviation} flag={(awayData.flag != null ? awayData.flag.name : "NULL")}");
        }
    }

    IEnumerator LateInitialize()
    {
        yield return null; // one frame — lets all newly-activated VehicleController.Start() complete

        // Collect all active VehicleControllers now that they're fully initialized
        allVehicles = FindObjectsByType<VehicleController>(FindObjectsInactive.Exclude);

        Debug.Log($"[GameManager] LateInitialize: found {allVehicles.Length} active VehicleController(s). playerTeam={playerTeam}");

        AssignGoals();
        ResetRound();

        // Apply vehicle types (stats + sprites) before tinting
        var modeSettings = GetCurrentModeSettings();
        ApplyPositionVehicleSetup(teamAPlayers, modeSettings);
        ApplyPositionVehicleSetup(teamBPlayers, modeSettings);
        if (teamAGoalie != null) ApplyVehicleSetup(teamAGoalie, VehicleController.PlayerPosition.Goalie);
        if (teamBGoalie != null) ApplyVehicleSetup(teamBGoalie, VehicleController.PlayerPosition.Goalie);

        // Clear the reference so SetActivePlayer never early-returns on a stale Inspector value
        activePlayer = null;

        // Clear control on everything first
        foreach (var v in allVehicles)
            v.SetPlayerControlled(false);

        // Assign to first active friendly non-goalie
        bool assigned = false;
        foreach (var v in allVehicles)
        {
            if (v.team == playerTeam && v.gameObject.activeSelf && !v.isGoalie)
            {
                SetActivePlayer(v);
                assigned = true;
                Debug.Log($"[GameManager] Player controlling: {v.name} (team={v.team})");
                break;
            }
        }

        if (!assigned)
        {
            // Emergency: first active non-goalie of any team
            foreach (var v in allVehicles)
            {
                if (v.isGoalie || !v.gameObject.activeSelf) continue;
                SetActivePlayer(v);
                Debug.LogWarning($"[GameManager] Fallback control → {v.name} (team={v.team}). " +
                                 "Fix: set Team=Friendly on your TeamA VehicleControllers.");
                break;
            }

            if (activePlayer == null)
                Debug.LogError($"[GameManager] No active VehicleControllers found. " +
                               $"A={teamAPlayers?.Length ?? 0} B={teamBPlayers?.Length ?? 0} vehicles discovered. " +
                               "Check Gameplay/TeamA and Gameplay/TeamB exist with VehicleController on their children.");
        }

        // Tint every vehicle with its country's color
        foreach (var v in allVehicles)
            if (v.TryGetComponent<VehicleAppearance>(out var appearance))
                appearance.ApplyTeamColor(v.team);

        currentState = GameState.Playing;
        SetMessage("Playing");
    }

    // ── Auto-discovery ──────────────────────────────────────────────────────

    void AutoDiscoverTeams()
    {
        // Level 1: use containers assigned in Inspector
        // Level 2: find containers by scene name
        if (teamAContainer == null || teamBContainer == null)
        {
            GameObject gameplay = GameObject.Find("Gameplay");
            if (gameplay != null)
            {
                if (teamAContainer == null) teamAContainer = gameplay.transform.Find("TeamA");
                if (teamBContainer == null) teamBContainer = gameplay.transform.Find("TeamB");
            }
        }
        if (spawnPointsRoot == null)
        {
            GameObject sp = GameObject.Find("SpawnPoints");
            if (sp != null) spawnPointsRoot = sp.transform;
        }

        if (teamAContainer != null)
        {
            var players = new List<GameObject>();
            foreach (Transform child in teamAContainer)
            {
                if (IsGoalieObject(child.gameObject)) teamAGoalie = child.gameObject;
                else players.Add(child.gameObject);
            }
            if (players.Count > 0) teamAPlayers = players.ToArray();
        }

        if (teamBContainer != null)
        {
            var players = new List<GameObject>();
            foreach (Transform child in teamBContainer)
            {
                if (IsGoalieObject(child.gameObject)) teamBGoalie = child.gameObject;
                else players.Add(child.gameObject);
            }
            if (players.Count > 0) teamBPlayers = players.ToArray();
        }

        // Level 3: scan every VehicleController in the scene grouped by team field
        // This fires when containers are not found or are empty
        if (teamAPlayers == null || teamAPlayers.Length == 0 ||
            teamBPlayers == null || teamBPlayers.Length == 0)
        {
            var allVC = FindObjectsByType<VehicleController>(FindObjectsInactive.Include);

            if (teamAPlayers == null || teamAPlayers.Length == 0)
            {
                var friendly = new List<GameObject>();
                foreach (var vc in allVC)
                {
                    if (vc.team != VehicleController.Team.Friendly) continue;
                    if (vc.isGoalie) { if (teamAGoalie == null) teamAGoalie = vc.gameObject; }
                    else friendly.Add(vc.gameObject);
                }
                if (friendly.Count > 0)
                {
                    teamAPlayers = friendly.ToArray();
                    Debug.Log($"[GameManager] Fallback scan found {teamAPlayers.Length} Friendly vehicles.");
                }
            }

            if (teamBPlayers == null || teamBPlayers.Length == 0)
            {
                var opponent = new List<GameObject>();
                foreach (var vc in allVC)
                {
                    if (vc.team != VehicleController.Team.Opponent) continue;
                    if (vc.isGoalie) { if (teamBGoalie == null) teamBGoalie = vc.gameObject; }
                    else opponent.Add(vc.gameObject);
                }
                // Also include AI.cs-only vehicles (no VehicleController) as team B
                foreach (var ai in FindObjectsByType<AI>(FindObjectsInactive.Include))
                    if (!ai.TryGetComponent<VehicleController>(out _) && !ai.isGoalie)
                        opponent.Add(ai.gameObject);

                if (opponent.Count > 0)
                {
                    teamBPlayers = opponent.ToArray();
                    Debug.Log($"[GameManager] Fallback scan found {teamBPlayers.Length} Opponent vehicles.");
                }
            }
        }

        if (teamAPlayers == null || teamAPlayers.Length == 0)
            Debug.LogError("[GameManager] No Team A vehicles found. Assign teamAContainer in Inspector, or ensure TeamA VehicleControllers have Team = Friendly.");
        if (teamBPlayers == null || teamBPlayers.Length == 0)
            Debug.LogWarning("[GameManager] No Team B vehicles found. They will be missing from the match.");
    }

    // Checks the isGoalie flag on whichever AI script is present
    bool IsGoalieObject(GameObject go)
    {
        if (go.TryGetComponent<VehicleController>(out var vc)) return vc.isGoalie;
        if (go.TryGetComponent<AI>(out var ai)) return ai.isGoalie;
        return false;
    }

    // Reads TeamXPlayer1, TeamXPlayer2, ... children under SpawnPoints/<ModeName>/
    Transform[] GetSpawnPoints(bool forTeamA)
    {
        if (spawnPointsRoot == null) return System.Array.Empty<Transform>();

        Transform modeRoot = spawnPointsRoot.Find(currentMode.ToString());
        if (modeRoot == null)
        {
            Debug.LogWarning($"No spawn folder named '{currentMode}' under {spawnPointsRoot.name}.");
            return System.Array.Empty<Transform>();
        }

        string prefix = forTeamA ? "TeamA" : "TeamB";
        var result = new List<Transform>();
        foreach (Transform child in modeRoot)
            if (child.name.StartsWith(prefix) && !child.name.EndsWith("Goalie"))
                result.Add(child);

        return result.ToArray();
    }

    Transform GetGoalieSpawn(bool forTeamA)
    {
        if (spawnPointsRoot == null) return null;

        Transform modeRoot = spawnPointsRoot.Find(currentMode.ToString());
        if (modeRoot == null) return null;

        return modeRoot.Find(forTeamA ? "TeamAGoalie" : "TeamBGoalie");
    }

    void AssignGoals()
    {
        Debug.Log($"[GameManager] AssignGoals: teamAShootsAt={(teamAShootsAt != null ? teamAShootsAt.name : "NULL")} teamBShootsAt={(teamBShootsAt != null ? teamBShootsAt.name : "NULL")}");

        // VehicleController vehicles (player team)
        foreach (var v in allVehicles)
            v.opponentGoal = (v.team == VehicleController.Team.Friendly) ? teamAShootsAt : teamBShootsAt;

        // AI.cs vehicles (enemy team — no VehicleController on them)
        foreach (var ai in FindObjectsByType<AI>(FindObjectsInactive.Exclude))
        {
            if (ai.opponentGoal == null) ai.opponentGoal = teamBShootsAt;
            if (ai.ownGoal == null)      ai.ownGoal      = teamAShootsAt;
        }
    }

    // ── Update & input ──────────────────────────────────────────────────────

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

        if (Input.GetKeyDown(KeyCode.Tab))   SwitchPlayer();
        if (Input.GetKeyDown(KeyCode.R))     { ResetGame(); StartKickoff(); }
    }

    // ── Public match flow ───────────────────────────────────────────────────

    public void StartMatch()  { ResetGame(); StartKickoff(); }

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

        EndScreen endScreen = FindAnyObjectByType<EndScreen>();
        if (endScreen != null) endScreen.Show(this);
    }

    public void GoalScored(bool teamAScored)
    {
        if (currentState == GameState.GameOver) return;

        if (teamAScored) teamAScore++; else teamBScore++;
        if (MatchStats.Instance != null) MatchStats.Instance.RecordGoal(teamAScored);

        UpdateUI();
        currentState = GameState.Goal;
        goalTimer = 0f;
        SetMessage($"Goal! {(teamAScored ? teamA.teamName : teamB.teamName)} scored!");

        if (GoalSequence.Instance != null)
            GoalSequence.Instance.Play(teamAScored, teamAScored ? teamA.teamName : teamB.teamName);
        else if (audioManager != null)
            audioManager.PlayGoal();

        if (useScoreLimit && (teamAScore >= winningScore || teamBScore >= winningScore))
            EndGame();
    }

    public void ResetRound()
    {
        if (ball != null)
            ball.position = kickoffPoint != null ? kickoffPoint.position : Vector3.zero;

        if (ballRb != null)
        {
            ballRb.linearVelocity  = Vector2.zero;
            ballRb.angularVelocity = 0f;
        }

        SpawnPlayers();
    }

    public void ResetGame()
    {
        teamAScore = 0;
        teamBScore = 0;
        gameTime   = 0f;
        goalTimer  = 0f;
        currentState = GameState.Kickoff;
        ResetRound();
        UpdateUI();
        SetMessage("Ready");
    }

    // ── Vehicle type / position setup ──────────────────────────────────────

    void ApplyPositionVehicleSetup(GameObject[] players, GameModeSettings settings)
    {
        if (players == null || settings.positions == null) return;
        for (int i = 0; i < players.Length && i < settings.positions.Length; i++)
            if (players[i] != null && players[i].activeSelf)
                ApplyVehicleSetup(players[i], settings.positions[i]);
    }

    void ApplyVehicleSetup(GameObject go, VehicleController.PlayerPosition position)
    {
        if (go == null) return;
        if (!go.TryGetComponent<VehicleController>(out var vc)) return;

        vc.position    = position;
        vc.vehicleType = GetVehicleTypeForPosition(position);
        vc.ApplyVehicleStats();

        if (go.TryGetComponent<VehicleAppearance>(out var appearance))
            appearance.ApplyVehicleType(vc.vehicleType);
    }

    VehicleController.VehicleType GetVehicleTypeForPosition(VehicleController.PlayerPosition pos) => pos switch
    {
        VehicleController.PlayerPosition.Striker    => MatchSettings.strikerType,
        VehicleController.PlayerPosition.Midfielder => MatchSettings.midfielderType,
        VehicleController.PlayerPosition.Defender   => MatchSettings.defenderType,
        _                                           => VehicleController.VehicleType.Tank,
    };

    // ── Player control ──────────────────────────────────────────────────────

    public void SetActivePlayer(VehicleController player)
    {
        if (activePlayer == player) return;
        if (activePlayer != null) activePlayer.SetPlayerControlled(false);
        activePlayer = player;
        if (activePlayer != null)
        {
            activePlayer.SetPlayerControlled(true);
            Debug.Log($"[GameManager] Control → {activePlayer.name} (team={activePlayer.team}, isPlayerControlled={activePlayer.isPlayerControlled})");
        }
    }

    public void SetPlayerTeam(VehicleController.Team selectedTeam)
    {
        playerTeam = selectedTeam;
        if (allVehicles == null || allVehicles.Length == 0)
            allVehicles = FindObjectsByType<VehicleController>(FindObjectsInactive.Exclude);
    }

    void SwitchPlayer()
    {
        if (allVehicles == null) return;
        if (Time.time - lastSwitchTime < switchCooldown) return;

        VehicleController best = null;
        float closestDist = float.MaxValue;

        foreach (var v in allVehicles)
        {
            if (v.team != playerTeam || v == activePlayer || v.isGoalie || !v.gameObject.activeSelf) continue;
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

        // Awake() pre-populates defaults, so reaching here should never happen
        int perTeam = currentMode switch
        {
            GameMode.OneVOne     => 1,
            GameMode.TwoVTwo     => 2,
            GameMode.ThreeVThree => 3,
            GameMode.FiveVFive   => 5,
            _                    => 1
        };
        return new GameModeSettings { mode = currentMode, matchDuration = gameDuration, useGoalie = false, playersPerTeam = perTeam };
    }

    void ConfigureMatch()
    {
        GameModeSettings settings = GetCurrentModeSettings();
        int needed = settings.playersPerTeam;

        if (teamAPlayers != null)
            for (int i = 0; i < teamAPlayers.Length; i++)
                if (teamAPlayers[i] != null) teamAPlayers[i].SetActive(i < needed);

        if (teamAGoalie != null) teamAGoalie.SetActive(settings.useGoalie);

        if (teamBPlayers != null)
            for (int i = 0; i < teamBPlayers.Length; i++)
                if (teamBPlayers[i] != null) teamBPlayers[i].SetActive(i < needed);

        if (teamBGoalie != null) teamBGoalie.SetActive(settings.useGoalie);

        Debug.Log($"[GameManager] Mode={currentMode} — {needed}v{needed}, goalie={settings.useGoalie} | A={teamAPlayers?.Length ?? 0} B={teamBPlayers?.Length ?? 0}");
    }

    void SpawnPlayers()
    {
        GameModeSettings settings = GetCurrentModeSettings();
        int needed = settings.playersPerTeam;

        Transform[] spawnA       = GetSpawnPoints(true);
        Transform[] spawnB       = GetSpawnPoints(false);
        Transform   goalieSpawnA = GetGoalieSpawn(true);
        Transform   goalieSpawnB = GetGoalieSpawn(false);

        if (teamAPlayers != null)
            for (int i = 0; i < teamAPlayers.Length && i < needed; i++)
                PlaceVehicle(teamAPlayers[i], spawnA, i);

        if (settings.useGoalie) PlaceVehicle(teamAGoalie, goalieSpawnA);

        if (teamBPlayers != null)
            for (int i = 0; i < teamBPlayers.Length && i < needed; i++)
                PlaceVehicle(teamBPlayers[i], spawnB, i);

        if (settings.useGoalie) PlaceVehicle(teamBGoalie, goalieSpawnB);
    }

    void PlaceVehicle(GameObject go, Transform[] points, int index)
    {
        if (go == null || !go.activeSelf) return;
        PlaceAt(go, (points != null && index < points.Length) ? points[index] : null);
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
            rb.linearVelocity  = Vector2.zero;
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
