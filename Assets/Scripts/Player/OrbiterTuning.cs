/// <summary>
/// Single source for orbiter slider clamps and ranges shared by OrbiterConfig, Orb, and UI.
/// </summary>
public static class OrbiterTuning
{
    public const float ThrustForceMin = 0f;
    public const float ThrustForceMax = 20f;
    public static float ThrustForceEditMin = ThrustForceMin;
    public static float ThrustForceEditMax = ThrustForceMax;

    public const float MinThrustAssistMin = 0f;
    public const float MinThrustAssistMax = 10f;

    public const float ImpulseForceMin = 0f;
    public const float ImpulseForceMax = 30f;
    public static float ImpulseForceEditMin = ImpulseForceMin;
    public static float ImpulseForceEditMax = ImpulseForceMax;

    public const float RechargeDurationMin = 1f;
    public const float RechargeDurationMax = 5f;
    public static float RechargeDurationEditMin = RechargeDurationMin;
    public static float RechargeDurationEditMax = RechargeDurationMax;

    public const float InertiaDampTimeMin = 0.5f;
    public const float InertiaDampTimeMax = 5f;
    public static float InertiaDampTimeEditMin = InertiaDampTimeMin;
    public static float InertiaDampTimeEditMax = InertiaDampTimeMax;

    public const float StabilizerMaxThrustSpeedMin = 1f;
    public const float StabilizerMaxThrustSpeedMax = 25f;
    public static float StabilizerMaxThrustSpeedEditMin = StabilizerMaxThrustSpeedMin;
    public static float StabilizerMaxThrustSpeedEditMax = StabilizerMaxThrustSpeedMax;

    /// <summary>Tangential escape error, sqrt(2) - 1. That deviation maps to stability 0.</summary>
    public const float CircularRedDeviation = 0.41421356f;

    public const float FuelCostPerSecondMin = 0f;
    public const float FuelCostPerSecondMax = 40f;
    public static float FuelCostPerSecondEditMin = FuelCostPerSecondMin;
    public static float FuelCostPerSecondEditMax = FuelCostPerSecondMax;

    public const float FuelCapacityMin = 10f;
    public const float FuelCapacityMax = 400f;
    public static float FuelCapacityEditMin = FuelCapacityMin;
    public static float FuelCapacityEditMax = FuelCapacityMax;

    public const float FuelRechargeDelayMin = 0f;
    public const float FuelRechargeDelayMax = 10f;
    public static float FuelRechargeDelayEditMin = FuelRechargeDelayMin;
    public static float FuelRechargeDelayEditMax = FuelRechargeDelayMax;

    /// <summary>Speed at or below this counts as standing still for fuel recharge.</summary>
    public const float FuelIdleSpeed = 0.25f;

    /// <summary>Fraction of capacity restored each second after the idle wait. 0.2 fills the tank in 5 seconds.</summary>
    public const float FuelRechargeFractionPerSecond = 0.2f;
}
