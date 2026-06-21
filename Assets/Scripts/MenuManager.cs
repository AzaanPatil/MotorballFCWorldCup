using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [Header("Vehicle Selection Panels")]
    [Tooltip("The whole midfielder vehicle-picker panel (label + dropdown). Hidden for 1v1 and 2v2.")]
    public GameObject midfielderVehiclePanel;
    [Tooltip("The whole defender vehicle-picker panel (label + dropdown). Hidden unless 5v5.")]
    public GameObject defenderVehiclePanel;

    void Start()
    {
        RefreshVehiclePanels();
    }

    public void SelectGameMode(int modeIndex)
    {
        MatchSettings.selectedMode = (GameManager.GameMode)modeIndex;
        Debug.Log("Mode: " + MatchSettings.selectedMode);
        RefreshVehiclePanels();
    }

    void RefreshVehiclePanels()
    {
        int mode = (int)MatchSettings.selectedMode;
        if (midfielderVehiclePanel != null) midfielderVehiclePanel.SetActive(mode >= 2); // 3v3+
        if (defenderVehiclePanel   != null) defenderVehiclePanel.SetActive(mode >= 3);   // 5v5 only
    }

    public void SelectHomeCountry(int countryIndex)
    {
        MatchSettings.homeCountry =
            (MatchSettings.Country)countryIndex;

        Debug.Log("Home: " + MatchSettings.homeCountry);
    }

    public void SelectAwayCountry(int countryIndex)
    {
        MatchSettings.awayCountry =
            (MatchSettings.Country)countryIndex;

        Debug.Log("Away: " + MatchSettings.awayCountry);
    }
    
    public void SelectTeam(int teamIndex)
    {
        // 0 = Friendly (TeamA / Home), 1 = Opponent (TeamB / Away)
        MatchSettings.playerTeam = (VehicleController.Team)teamIndex;
        Debug.Log("Player team: " + MatchSettings.playerTeam);
    }

    // Vehicle type dropdowns — options must be ordered Car(0), MonsterTruck(1), Quad(2)
    // Goalie is always Tank; no dropdown needed for it
    public void SelectStrikerVehicle(int typeIndex)
    {
        MatchSettings.strikerType = (VehicleController.VehicleType)typeIndex;
        Debug.Log("Striker vehicle: " + MatchSettings.strikerType);
    }

    public void SelectMidfielderVehicle(int typeIndex)
    {
        MatchSettings.midfielderType = (VehicleController.VehicleType)typeIndex;
        Debug.Log("Midfielder vehicle: " + MatchSettings.midfielderType);
    }

    public void SelectDefenderVehicle(int typeIndex)
    {
        MatchSettings.defenderType = (VehicleController.VehicleType)typeIndex;
        Debug.Log("Defender vehicle: " + MatchSettings.defenderType);
    }

    public void StartGame()
    {
        SceneManager.LoadScene("GameScene");
    }

    
}