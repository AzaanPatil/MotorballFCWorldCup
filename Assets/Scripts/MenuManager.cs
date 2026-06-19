using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public void SelectGameMode(int modeIndex)
    {
        MatchSettings.selectedMode =
            (GameManager.GameMode)modeIndex;

        Debug.Log("Mode: " + MatchSettings.selectedMode);
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
    
    public void StartGame()
    {
        SceneManager.LoadScene("GameScene");
    }

    
}