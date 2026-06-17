using UnityEngine;

public class CountryManager : MonoBehaviour
{
    public CountryData[] countries;

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
        CountryData data =
            GetCountryData(
                MatchSettings.selectedCountry);

        if(data != null)
        {
            Debug.Log(
                "Selected Country: " +
                data.countryName +
                " (" +
                data.abbreviation +
                ")");
        }
    }
}