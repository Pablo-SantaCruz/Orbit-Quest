using System;
using UnityEngine;

[CreateAssetMenu(fileName = "OrbPresetModule", menuName = "Proto-Pablo/Presets/Orb", order = 4)]
public class OrbPresetModule : PresetModuleAsset
{
    public const string ModuleId = "orb";

    public override string Id => ModuleId;

    public override string Capture(PresetSessionContext context)
    {
        PlayerData playerData = context.PlayerData;
        FuelResource fuel = playerData.FuelResource;
        bool hasFuel = fuel != null;
        if (!hasFuel)
            Debug.LogError($"Preset module '{Id}' skipped fuel because FuelResource is null.");

        // HasFuel is serialized so a missing tank does not write fuel values that a later apply would treat as real.
        var payload = new OrbPresetPayload
        {
            ImpulseMode = (int)playerData.ImpulseResource.ImpulseMode,
            ImpulseForce = playerData.ImpulseResource.ImpulseForce,
            RechargeDuration = playerData.ImpulseResource.RechargeDuration,
            InertiaDampTime = playerData.InertiaResource.InertiaDampTime,
            StabilizerMaxThrustSpeed = playerData.InertiaResource.StabilizerMaxThrustSpeed,
            ThrustForce = playerData.ThrusterResource.ThrustForce,
            HasFuel = hasFuel,
            FuelCostPerSecond = hasFuel ? fuel.CostPerSecond : 0f,
            FuelCapacity = hasFuel ? fuel.Capacity : 0f,
            FuelRechargeWhenIdle = hasFuel && fuel.RechargeWhenIdle,
            FuelRechargeDelay = hasFuel ? fuel.RechargeDelay : 0f
        };
        return JsonUtility.ToJson(payload);
    }

    public override void Apply(string payload, PresetSessionContext context)
    {
        if (string.IsNullOrEmpty(payload))
            return;

        try
        {
            if (!PresetPayloadJson.TryFromJson(payload, out OrbPresetPayload parsed) || !payload.Contains("\"ImpulseMode\""))
            {
                Debug.LogError($"Preset module '{Id}' could not parse payload.");
                return;
            }

            PlayerData playerData = context.PlayerData;
            playerData.UpdateImpulseMode(parsed.ImpulseMode);
            playerData.UpdateImpulseForce(parsed.ImpulseForce);
            playerData.UpdateImpuseCooldown(parsed.RechargeDuration);
            playerData.UpdateInertiaDampTime(parsed.InertiaDampTime);
            playerData.UpdateStabilizerMaxThrustSpeed(parsed.StabilizerMaxThrustSpeed);
            playerData.UpdateThrustForce(parsed.ThrustForce);

            if (!parsed.HasFuel)
                return;

            if (playerData.FuelResource == null)
            {
                Debug.LogError($"Preset module '{Id}' skipped fuel because FuelResource is null.");
                return;
            }

            playerData.UpdateFuelCostPerSecond(parsed.FuelCostPerSecond);
            playerData.UpdateFuelCapacity(parsed.FuelCapacity);
            playerData.UpdateFuelRechargeWhenIdle(parsed.FuelRechargeWhenIdle);
            playerData.UpdateFuelRechargeDelay(parsed.FuelRechargeDelay);
        }
        catch (Exception exception)
        {
            Debug.LogError($"Preset module '{Id}' failed to apply payload: {exception}");
        }
    }
}

[Serializable]
public struct OrbPresetPayload
{
    public int ImpulseMode;
    public float ImpulseForce;
    public float RechargeDuration;
    public float InertiaDampTime;
    public float StabilizerMaxThrustSpeed;
    public float ThrustForce;
    public float FuelCostPerSecond;
    public float FuelCapacity;
    public bool FuelRechargeWhenIdle;
    public float FuelRechargeDelay;
    public bool HasFuel;
}
