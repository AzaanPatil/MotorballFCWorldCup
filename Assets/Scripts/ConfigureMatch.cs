using UnityEngine;

public class MatchConfigurator : MonoBehaviour
{
    [Header("Team A Players")]
    public GameObject[] teamAPlayers;

    [Header("Team B Players")]
    public GameObject[] teamBPlayers;

    [Header("Goalies")]
    public GameObject teamAGoalie;
    public GameObject teamBGoalie;

    void Start()
    {
        ConfigureMatch();
    }

    void ConfigureMatch()
    {
        GameManager.GameMode mode = MatchSettings.selectedMode;

        int playersPerTeam = 1;
        bool useGoalies = false;

        switch (mode)
        {
            case GameManager.GameMode.OneVOne:
                playersPerTeam = 1;
                useGoalies = false;
                break;

            case GameManager.GameMode.TwoVTwo:
                playersPerTeam = 2;
                useGoalies = false;
                break;

            case GameManager.GameMode.ThreeVThree:
                playersPerTeam = 3;
                useGoalies = true;
                break;

            case GameManager.GameMode.FiveVFive:
                playersPerTeam = 5;
                useGoalies = true;
                break;
        }

        ConfigureVehicles(teamAPlayers, playersPerTeam);
        ConfigureVehicles(teamBPlayers, playersPerTeam);

        if (teamAGoalie != null)
            teamAGoalie.SetActive(useGoalies);

        if (teamBGoalie != null)
            teamBGoalie.SetActive(useGoalies);

        Debug.Log($"Configured {mode}");
    }

    void ConfigureVehicles(GameObject[] vehicles, int activeCount)
    {
        for (int i = 0; i < vehicles.Length; i++)
        {
            vehicles[i].SetActive(i < activeCount);
        }
    }
}