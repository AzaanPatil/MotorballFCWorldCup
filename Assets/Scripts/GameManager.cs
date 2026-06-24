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
    public float ballStuckTimeout        = 4f;
    public float ballStuckSpeedThreshold = 1f;
    private float ballStuckTimer         = 0f;

    [Header("UI")]
    public Text scoreText;
    public Text messageText;
    public GameAudio audioManager;
    public EndScreen endScreen;

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

    [HideInInspector] public VehicleController[] allVehicles;
    public bool IsGameActive { get; private set; }
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
                new() { mode = GameMode.ThreeVThree, playersPerTeam = 3, matchDuration = 120f, useGoalie = true,
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
        IsGameActive = true;
        currentMode = MatchSettings.selectedMode;
        playerTeam  = MatchSettings.playerTeam;

        ApplyCountryData();

        if (ball != null && ballRb == null)
            ballRb = ball.GetComponent<Rigidbody2D>();

        AutoDiscoverTeams();
        ValidateSetup();
        ConfigureMatch();

        teamAScore = 0;
        teamBScore = 0;
        gameTime   = 0f;
        goalTimer  = 0f;
        UpdateUI();

        if (audioManager != null) audioManager.StartCrowdAmbience();

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

        // Assign to Player1 of the player's team, falling back to first available
        bool assigned = false;
        VehicleController fallback = null;
        foreach (var v in allVehicles)
        {
            if (v.team != playerTeam || !v.gameObject.activeSelf || v.isGoalie) continue;
            if (fallback == null) fallback = v;
            if (v.name.Contains("Player1") || v.name.EndsWith("1"))
            {
                SetActivePlayer(v);
                assigned = true;
                Debug.Log($"[GameManager] Player controlling: {v.name} (team={v.team})");
                break;
            }
        }
        if (!assigned && fallback != null)
        {
            SetActivePlayer(fallback);
            assigned = true;
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

        // Tint every vehicle with its country's color — catch errors so they can't prevent Playing state
        try
        {
            foreach (var v in allVehicles)
                if (v != null && v.TryGetComponent<VehicleAppearance>(out var appearance))
                    appearance.ApplyTeamColor(v.team);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GameManager] ApplyTeamColor threw: {e.Message}. Continuing anyway.");
        }

        // Log each vehicle's final state so any misconfiguration is obvious
        foreach (var v in allVehicles)
        {
            if (v == null) continue;
            Debug.Log($"[GameManager] Vehicle '{v.name}': team={v.team} pos={v.position} isGoalie={v.isGoalie} " +
                      $"ownGoal={(v.ownGoal != null ? v.ownGoal.name : "NULL ⚠")} " +
                      $"oppGoal={(v.opponentGoal != null ? v.opponentGoal.name : "NULL ⚠")} " +
                      $"playerControlled={v.isPlayerControlled}");
        }

        currentState = GameState.Playing;
        SetMessage("Playing");
    }

    // ── Setup validation ────────────────────────────────────────────────────

    void ValidateSetup()
    {
        string ok  = "✓";
        string bad = "✗ MISSING";

        string goalAInfo = teamAShootsAt != null
            ? $"{ok} {teamAShootsAt.name} @ {teamAShootsAt.position}"
            : $"{bad} — drag Team B net Transform into GameManager.TeamA Shoots At";
        string goalBInfo = teamBShootsAt != null
            ? $"{ok} {teamBShootsAt.name} @ {teamBShootsAt.position}"
            : $"{bad} — drag Team A net Transform into GameManager.TeamB Shoots At";

        Debug.Log("=== GameManager Setup Validation ===\n" +
            $"  Ball              : {(ball            != null ? ok + " " + ball.name            : bad + " — drag Ball into GameManager.Ball")}\n" +
            $"  KickoffPoint      : {(kickoffPoint    != null ? ok + " " + kickoffPoint.name + " @ " + kickoffPoint.position    : bad + " — drag KickoffPoint into GameManager.KickoffPoint")}\n" +
            $"  TeamA shoots at   : {goalAInfo}\n" +
            $"  TeamB shoots at   : {goalBInfo}\n" +
            $"  TeamA container   : {(teamAContainer  != null ? ok + " " + teamAContainer.name  : bad + " — drag Gameplay/TeamA into GameManager.Team A Container")}\n" +
            $"  TeamB container   : {(teamBContainer  != null ? ok + " " + teamBContainer.name  : bad + " — drag Gameplay/TeamB into GameManager.Team B Container")}\n" +
            $"  SpawnPoints root  : {(spawnPointsRoot != null ? ok + " " + spawnPointsRoot.name : bad + " — drag SpawnPoints root into GameManager.Spawn Points Root")}\n" +
            $"  TeamA goalie      : {(teamAGoalie     != null ? ok + " " + teamAGoalie.name     : "— none detected (only needed for modes with useGoalie=true)")}\n" +
            $"  TeamB goalie      : {(teamBGoalie     != null ? ok + " " + teamBGoalie.name     : "— none detected")}\n" +
            $"  Mode              : {currentMode}  |  playerTeam={playerTeam}\n" +
            "====================================");

        if (teamAShootsAt == null || teamBShootsAt == null)
            Debug.LogError("[GameManager] Goal targets are null — ALL AI will cluster at ball because ownGoal/opponentGoal will be null on every vehicle. Fix this first.");
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
                if (IsGoalieObject(child.gameObject)) { teamAGoalie = child.gameObject; Debug.Log($"[GameManager] TeamA goalie detected: {child.name}"); }
                else players.Add(child.gameObject);
            }
            if (players.Count > 0) teamAPlayers = players.ToArray();
            Debug.Log($"[GameManager] TeamA — {players.Count} player(s), goalie={(teamAGoalie != null ? teamAGoalie.name : "NONE")}");
        }

        if (teamBContainer != null)
        {
            var players = new List<GameObject>();
            foreach (Transform child in teamBContainer)
            {
                if (IsGoalieObject(child.gameObject)) { teamBGoalie = child.gameObject; Debug.Log($"[GameManager] TeamB goalie detected: {child.name}"); }
                else players.Add(child.gameObject);
            }
            if (players.Count > 0) teamBPlayers = players.ToArray();
            Debug.Log($"[GameManager] TeamB — {players.Count} player(s), goalie={(teamBGoalie != null ? teamBGoalie.name : "NONE")}");
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

    // A vehicle is a goalie if its flag is set OR its name contains "Goalie" — whichever is easier to configure.
    bool IsGoalieObject(GameObject go)
    {
        bool byName = go.name.IndexOf("Goalie", System.StringComparison.OrdinalIgnoreCase) >= 0;
        if (go.TryGetComponent<VehicleController>(out var vc)) return vc.isGoalie || byName;
        if (go.TryGetComponent<AI>(out var ai))                return ai.isGoalie  || byName;
        return byName;
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

        // VehicleController vehicles
        foreach (var v in allVehicles)
        {
            v.opponentGoal = (v.team == VehicleController.Team.Friendly) ? teamAShootsAt : teamBShootsAt;
            v.ownGoal      = (v.team == VehicleController.Team.Friendly) ? teamBShootsAt : teamAShootsAt;
        }

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

            if (ballRb != null)
            {
                if (ballRb.linearVelocity.magnitude < ballStuckSpeedThreshold)
                    ballStuckTimer += Time.deltaTime;
                else
                    ballStuckTimer = 0f;

                if (ballStuckTimer >= ballStuckTimeout)
                {
                    ballStuckTimer = 0f;
                    if (ball.TryGetComponent<BallScript>(out var bs))
                        bs.ResetBall(kickoffPoint);
                }
            }
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

        if (endScreen == null) endScreen = FindAnyObjectByType<EndScreen>();
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
        vc.isGoalie    = position == VehicleController.PlayerPosition.Goalie;
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

    private Coroutine passTrackCoroutine;

    public void BeginPassTracking(VehicleController receiver)
    {
        if (passTrackCoroutine != null) StopCoroutine(passTrackCoroutine);
        passTrackCoroutine = StartCoroutine(TrackPass(receiver));
    }

    IEnumerator TrackPass(VehicleController receiver)
    {
        float elapsed = 0f;
        float timeout = 4f;

        while (elapsed < timeout)
        {
            if (ball != null && receiver != null)
            {
                if (Vector2.Distance(ball.position, receiver.transform.position) < 1.5f)
                {
                    SetActivePlayer(receiver);
                    yield break;
                }
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    void SwitchPlayer()
    {
        if (allVehicles == null || activePlayer == null) return;
        if (Time.time - lastSwitchTime < switchCooldown) return;

        // Find the closest friendly (other than current player) to the ball
        VehicleController best = null;
        float closestDist = float.MaxValue;

        foreach (var v in allVehicles)
        {
            if (v.team != playerTeam || v == activePlayer || v.isGoalie || !v.gameObject.activeSelf) continue;
            float d = v.DistanceToBall();
            if (d < closestDist) { closestDist = d; best = v; }
        }

        float activeDist = activePlayer.DistanceToBall();

        if (best != null && closestDist < activeDist)
        {
            // Someone else is closer — switch to them
            SetActivePlayer(best);
        }
        else
        {
            // We're the closest — smart pass to most open teammate ahead
            activePlayer.TrySmartPass();
        }

        lastSwitchTime = Time.time;
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
        else if (settings.useGoalie) Debug.LogWarning($"[GameManager] {currentMode} needs a goalie but teamAGoalie is null. " +
            "Ensure one child of TeamA has isGoalie=true on its VehicleController, or 'Goalie' in its name.");

        if (teamBPlayers != null)
            for (int i = 0; i < teamBPlayers.Length; i++)
                if (teamBPlayers[i] != null) teamBPlayers[i].SetActive(i < needed);

        if (teamBGoalie != null) teamBGoalie.SetActive(settings.useGoalie);
        else if (settings.useGoalie) Debug.LogWarning($"[GameManager] {currentMode} needs a goalie but teamBGoalie is null. " +
            "Ensure one child of TeamB has isGoalie=true on its VehicleController, or 'Goalie' in its name.");

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

        if (spawnA.Length < needed)
            Debug.LogWarning($"[GameManager] SpawnPoints/{currentMode} has only {spawnA.Length} TeamA slots " +
                $"but {needed} are needed. Using computed fallback positions.");

        if (teamAPlayers != null)
            for (int i = 0; i < teamAPlayers.Length && i < needed; i++)
                PlaceVehicle(teamAPlayers[i], spawnA, i, ComputeSpawn(true, i, needed));

        if (settings.useGoalie)
        {
            if (goalieSpawnA == null)
                Debug.LogWarning($"[GameManager] No goalie spawn found for TeamA — using computed fallback.");
            PlaceVehicle(teamAGoalie, goalieSpawnA, ComputeGoalieSpawn(true));
        }

        if (teamBPlayers != null)
            for (int i = 0; i < teamBPlayers.Length && i < needed; i++)
                PlaceVehicle(teamBPlayers[i], spawnB, i, ComputeSpawn(false, i, needed));

        if (settings.useGoalie)
        {
            if (goalieSpawnB == null)
                Debug.LogWarning($"[GameManager] No goalie spawn found for TeamB — using computed fallback.");
            PlaceVehicle(teamBGoalie, goalieSpawnB, ComputeGoalieSpawn(false));
        }
    }

    // Returns the world-space centre of a goal trigger, reading BoxCollider2D offset rather than raw Transform position.
    // Mirrors the same helper in VehicleController so both use identical reference points.
    static Vector2 GoalCenter(Transform t)
    {
        if (t == null) return Vector2.zero;
        var col = t.GetComponent<BoxCollider2D>();
        return col != null ? (Vector2)t.TransformPoint(col.offset) : (Vector2)t.position;
    }

    // Computes a reasonable spawn position from goal transforms when named spawn points don't exist.
    Vector3 ComputeSpawn(bool forTeamA, int index, int total)
    {
        if (teamAShootsAt == null || teamBShootsAt == null)
            return kickoffPoint != null ? kickoffPoint.position : Vector3.zero;
        Vector2 ownGoalPos = forTeamA ? GoalCenter(teamBShootsAt) : GoalCenter(teamAShootsAt);
        Vector2 oppGoalPos = forTeamA ? GoalCenter(teamAShootsAt) : GoalCenter(teamBShootsAt);
        Vector2 mid        = (ownGoalPos + oppGoalPos) * 0.5f;
        Vector2 attackDir  = (oppGoalPos - ownGoalPos).normalized;
        Vector2 lateral    = new(-attackDir.y, attackDir.x);
        float   laneOffset = (index - (total - 1) * 0.5f) * 5f;
        return mid - attackDir * 4f + lateral * laneOffset;
    }

    Vector3 ComputeGoalieSpawn(bool forTeamA)
    {
        if (teamAShootsAt == null || teamBShootsAt == null)
            return kickoffPoint != null ? kickoffPoint.position : Vector3.zero;
        Vector2 ownGoalPos = forTeamA ? GoalCenter(teamBShootsAt) : GoalCenter(teamAShootsAt);
        Vector2 oppGoalPos = forTeamA ? GoalCenter(teamAShootsAt) : GoalCenter(teamBShootsAt);
        Vector2 attackDir  = (oppGoalPos - ownGoalPos).normalized;
        return ownGoalPos + attackDir * 3f;
    }

    void PlaceVehicle(GameObject go, Transform[] points, int index, Vector3 fallbackPos)
    {
        if (go == null || !go.activeSelf) return;
        if (points != null && index < points.Length && points[index] != null)
            PlaceAt(go, points[index].position, points[index].rotation);
        else
            PlaceAt(go, fallbackPos, Quaternion.identity);
    }

    void PlaceVehicle(GameObject go, Transform point, Vector3 fallbackPos)
    {
        if (go == null || !go.activeSelf) return;
        if (point != null)
            PlaceAt(go, point.position, point.rotation);
        else
            PlaceAt(go, fallbackPos, Quaternion.identity);
    }

    void PlaceAt(GameObject go, Vector3 position, Quaternion rotation)
    {
        go.transform.SetPositionAndRotation(position, rotation);
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

    // ── Editor diagnostics ──────────────────────────────────────────────────

    void OnDrawGizmos()
    {
        float r = 1f;

        if (teamAShootsAt != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(teamAShootsAt.position, r);
#if UNITY_EDITOR
            UnityEditor.Handles.Label(teamAShootsAt.position + Vector3.up * (r + 0.3f),
                $"TeamA shoots AT here\n(= Team B net)\n{teamAShootsAt.name} @ {teamAShootsAt.position}");
#endif
        }

        if (teamBShootsAt != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(teamBShootsAt.position, r);
#if UNITY_EDITOR
            UnityEditor.Handles.Label(teamBShootsAt.position + Vector3.up * (r + 0.3f),
                $"TeamB shoots AT here\n(= Team A net)\n{teamBShootsAt.name} @ {teamBShootsAt.position}");
#endif
        }

        if (kickoffPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(kickoffPoint.position, r * 0.5f);
        }
    }
}
