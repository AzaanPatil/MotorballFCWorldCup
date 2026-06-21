using UnityEngine;

[System.Serializable]
public struct VehicleTypeSprite
{
    public VehicleController.VehicleType vehicleType;
    public Sprite sprite;
}

public class VehicleAppearance : MonoBehaviour
{
    [Tooltip("The vehicle's body SpriteRenderer. Leave empty to auto-find on the same GameObject.")]
    public SpriteRenderer body;

    [Tooltip("Map each selectable vehicle type to its sprite. Set this up once per vehicle in the Inspector.")]
    public VehicleTypeSprite[] vehicleSprites;

    void Awake()
    {
        if (body == null)
            body = GetComponent<SpriteRenderer>();
    }

    // Swaps the sprite for the given vehicle type. Call this before ApplyTeamColor so the tint lands on the right sprite.
    public void ApplyVehicleType(VehicleController.VehicleType type)
    {
        if (body == null) return;
        foreach (var entry in vehicleSprites)
        {
            if (entry.vehicleType == type)
            {
                body.sprite = entry.sprite;
                return;
            }
        }
    }

    // Tints the current sprite with the country's home or away color.
    public void ApplyTeamColor(VehicleController.Team team)
    {
        if (body == null) return;

        Color teamColor = Color.white;

        if (CountryManager.Instance != null)
        {
            teamColor = team == VehicleController.Team.Friendly
                ? CountryManager.Instance.GetCountryData(MatchSettings.homeCountry)?.homeColor ?? Color.white
                : CountryManager.Instance.GetCountryData(MatchSettings.awayCountry)?.awayColor ?? Color.white;
        }

        body.color = teamColor;
    }
}
