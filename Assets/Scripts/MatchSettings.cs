using UnityEngine;

public static class MatchSettings
{
    public static GameManager.GameMode selectedMode;

    public static Country selectedCountry;

    public static VehicleController.VehicleType[] homeVehicles;
    public static VehicleController.VehicleType[] awayVehicles;

    public enum Country
    {
        USA,
        Brazil,
        Germany,
        Portugul,
        Argentina,
        Canada,
        England,
        France
    }

    

    

}