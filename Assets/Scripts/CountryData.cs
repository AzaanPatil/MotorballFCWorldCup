using UnityEngine;

[System.Serializable]
public class CountryLineup
{
    [Tooltip("Vehicle type per field player slot — index 0 = Player1, 1 = Player2, etc.")]
    public VehicleController.VehicleType[] fieldPlayerTypes;

    [Tooltip("Vehicle type for the goalkeeper position")]
    public VehicleController.VehicleType goalieType = VehicleController.VehicleType.Tank;
}

[System.Serializable]
public class CountryData
{
    public MatchSettings.Country country;

    [Tooltip("3-letter code shown in the ScoreBug (e.g. BRA, GER, USA)")]
    public string abbreviation;

    [Tooltip("Flag sprite shown beside the abbreviation in the ScoreBug")]
    public Sprite flag;

    [Tooltip("Color applied to this country's vehicles when playing as the Home team")]
    public Color homeColor = Color.white;

    [Tooltip("Color applied to this country's vehicles when playing as the Away team")]
    public Color awayColor = Color.white;

    public float speedMultiplier        = 1f;
    public float accelerationMultiplier = 1f;
    public float kickMultiplier         = 1f;
    public float hitMultiplier          = 1f;

    [Header("Future — Vehicle Lineup")]
    [Tooltip("Set vehicle types per position now; the game will apply them automatically once lineup selection is implemented")]
    public CountryLineup lineup;
}
