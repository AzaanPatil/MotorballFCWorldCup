using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public void Set1v1()
    {
        MatchSettings.selectedMode = GameManager.GameMode.OneVOne;
    }

    public void Set2v2()
    {
        MatchSettings.selectedMode = GameManager.GameMode.TwoVTwo;
    }

    public void Set3v3()
    {
        MatchSettings.selectedMode = GameManager.GameMode.ThreeVThree;
    }

    public void Set5v5()
    {
        MatchSettings.selectedMode = GameManager.GameMode.FiveVFive;
    }

    public void SelectCountry(int countryIndex)
    {
        MatchSettings.selectedCountry =
            (MatchSettings.Country)countryIndex;
    }
    
    public void StartGame()
    {
        SceneManager.LoadScene("GameScene");
    }

    
}