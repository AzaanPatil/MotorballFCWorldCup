using UnityEngine;

[System.Serializable]
public class CountryData
{
    public MatchSettings.Country country;

    public string abbreviation;

    public Sprite flag;

    public Color homeColor;
    public Color awayColor;

    public float speedMultiplier = 1f;
    public float accelerationMultiplier = 1f;
    public float kickMultiplier = 1f;
    public float hitMultiplier = 1f;
}