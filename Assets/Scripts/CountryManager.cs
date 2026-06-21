using UnityEngine;

public class CountryManager : MonoBehaviour
{
    public static CountryManager Instance { get; private set; }

    public CountryData[] countries;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public CountryData GetCountryData(
        MatchSettings.Country country)
    {
        foreach(var c in countries)
        {
            if(c.country == country)
                return c;
        }

        return null;
    }

    void Start()
    {
        CountryData homeData =
            GetCountryData(
                MatchSettings.homeCountry);

        CountryData awayData =
            GetCountryData(
                MatchSettings.awayCountry);

        if (homeData != null)
        {
            Debug.Log(
                "Home Country: " +
                homeData.country +
                " (" +
                homeData.abbreviation +
                ")");
        }

        if (awayData != null)
        {
            Debug.Log(
                "Away Country: " +
                awayData.country +
                " (" +
                awayData.abbreviation +
                ")");
        }
    }
}