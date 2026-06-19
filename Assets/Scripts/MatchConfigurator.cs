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

    // Configuration is handled entirely by GameManager.ConfigureMatch().
    // This script is kept as a placeholder; its arrays are no longer used.
    void ConfigureMatch() { }
}