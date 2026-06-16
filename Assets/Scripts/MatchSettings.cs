using UnityEngine;

public static class MatchSettings
{
    public static GameManager.GameMode selectedMode;

    public static bool playerIsHomeTeam;

    public static VehicleController.VehicleType[] homeVehicles;
    public static VehicleController.VehicleType[] awayVehicles;

    public enum GameMode
    {
        OneVOne,
        TwoVTwo,
        ThreeVThree,
        FiveVFive
    }

}