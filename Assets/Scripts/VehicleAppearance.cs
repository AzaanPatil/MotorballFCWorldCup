using UnityEngine;

public class VehicleAppearance : MonoBehaviour
{
    public SpriteRenderer body;

    // Create method to apply team color to every vehicle's body sprite TeamA is the away team, TeamB is the home team.
    public void ApplyTeamColor(VehicleController.Team team)
    {
        if (body == null)
            return;

        Color teamColor = Color.white; // default color

        if (team == VehicleController.Team.Friendly)
        {
            teamColor = CountryManager.Instance.GetCountryData(MatchSettings.homeCountry)?.homeColor ?? Color.white;
        }
        else if (team == VehicleController.Team.Opponent)
        {
            teamColor = CountryManager.Instance.GetCountryData(MatchSettings.awayCountry)?.awayColor ?? Color.white;
        }

        body.color = teamColor;
    }
}