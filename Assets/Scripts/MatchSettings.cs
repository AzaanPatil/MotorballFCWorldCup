using UnityEngine;

public static class MatchSettings
{
    public static GameManager.GameMode selectedMode;

    public static Country homeCountry = Country.USA;
    public static Country awayCountry = Country.England;

    public static VehicleController.VehicleType[] homeVehicles;
    public static VehicleController.VehicleType[] awayVehicles;

    // Which team the human player controls — set from the main menu
    public static VehicleController.Team playerTeam = VehicleController.Team.Friendly;

    // Vehicle type per position group — Goalie is always Tank, not selectable
    public static VehicleController.VehicleType strikerType    = VehicleController.VehicleType.Car;
    public static VehicleController.VehicleType midfielderType = VehicleController.VehicleType.Car;
    public static VehicleController.VehicleType defenderType   = VehicleController.VehicleType.Car;

    public enum Country
    {
        USA,
        Brazil,
        Germany,
        Portugal,
        Argentina,
        Canada,
        England,
        France
    }

    

    

}