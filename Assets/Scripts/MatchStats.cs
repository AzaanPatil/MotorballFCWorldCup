using UnityEngine;

// Tracks per-match statistics. Add to the same GameObject as GameManager.
public class MatchStats : MonoBehaviour
{
    public static MatchStats Instance { get; private set; }

    public int teamAGoals;
    public int teamBGoals;
    public int teamASaves;
    public int teamBSaves;
    public int teamAHits;
    public int teamBHits;

    void Awake()
    {
        Instance = this;
    }

    public void Reset()
    {
        teamAGoals = teamBGoals = 0;
        teamASaves = teamBSaves = 0;
        teamAHits  = teamBHits  = 0;
    }

    public void RecordGoal(bool teamA)  { if (teamA) teamAGoals++; else teamBGoals++; }
    public void RecordSave(bool teamA)  { if (teamA) teamASaves++; else teamBSaves++; }
    public void RecordHit (bool teamA)  { if (teamA) teamAHits++;  else teamBHits++;  }
}
