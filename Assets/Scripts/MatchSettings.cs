using UnityEngine;

public static class MatchSettings
{
    public static GameManager.GameMode selectedMode;

    public static Country homeCountry = Country.USA;
    public static Country awayCountry = Country.England;

    public static VehicleController.VehicleType[] homeVehicles;
    public static VehicleController.VehicleType[] awayVehicles;

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