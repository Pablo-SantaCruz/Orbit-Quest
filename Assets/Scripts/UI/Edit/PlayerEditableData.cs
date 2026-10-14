using UnityEngine;
using UnityEngine.UI;
using System;

public class PlayerEditableData : IPanel
{
    public GameObject Root { get; private set; }
    readonly public DropdownComponent ImpulseMode;
    readonly public SliderComponent ImpulseForce;
    readonly public SliderComponent ImpuseCooldown;
    readonly public SliderComponent InertiaDampTime;
    readonly public SliderComponent ThrusterForce;
    readonly public SliderComponent MaxThrustSpeed;
    readonly public SliderComponent CostPerSecond;
    readonly public SliderComponent MaximunCapacity;
    readonly public ToggleComponent Recharge;
    readonly public SliderComponent TimeBeforeRecharge;
    readonly public PlayerData PlayerData;

    public PlayerEditableData(PlayerEditableDataDependencies dependencies)
    {
        Root = dependencies.Root;
        PlayerData = dependencies.PlayerData;
        ImpulseMode = new DropdownComponent(dependencies.ImpulseMode);
        ImpulseForce = new SliderComponent(dependencies.ImpulseForce);
        ImpuseCooldown = new SliderComponent(dependencies.ImpuseCooldown);
        InertiaDampTime = new SliderComponent(dependencies.InertiaDampTime);
        ThrusterForce = new SliderComponent(dependencies.ThrusterForce);
        MaxThrustSpeed = new SliderComponent(dependencies.MaxThrustSpeed);
        CostPerSecond = new SliderComponent(dependencies.CostPerSecond);
        MaximunCapacity = new SliderComponent(dependencies.MaximunCapacity);
        Recharge = new ToggleComponent(dependencies.Recharge);
        TimeBeforeRecharge = new SliderComponent(dependencies.TimeBeforeRecharge);
        dependencies.Fill.onClick.AddListener(PlayerData.FillFuel);
    }

    public void Toggle(bool active)
    {
        Root.SetActive(active);
        HandlePlayerDataUpdated(active);
    }

    private void HandlePlayerDataUpdated(bool value)
    {
        if(value)
        {
            ImpulseMode.OnValueChanged += PlayerData.UpdateImpulseMode;
            ImpulseForce.OnValueChanged += PlayerData.UpdateImpulseForce;
            ImpuseCooldown.OnValueChanged += PlayerData.UpdateImpuseCooldown;
            InertiaDampTime.OnValueChanged += PlayerData.UpdateInertiaDampTime;
            ThrusterForce.OnValueChanged += PlayerData.UpdateThrustForce;
            MaxThrustSpeed.OnValueChanged += PlayerData.UpdateStabilizerMaxThrustSpeed;
            CostPerSecond.OnValueChanged += PlayerData.UpdateFuelCostPerSecond;
            MaximunCapacity.OnValueChanged += PlayerData.UpdateFuelCapacity;
            Recharge.OnValueChanged += PlayerData.UpdateFuelRechargeWhenIdle;
            TimeBeforeRecharge.OnValueChanged += PlayerData.UpdateFuelRechargeDelay;
        }
        else
        {
            ImpulseMode.OnValueChanged -= PlayerData.UpdateImpulseMode;
            ImpulseForce.OnValueChanged -= PlayerData.UpdateImpulseForce;
            ImpuseCooldown.OnValueChanged -= PlayerData.UpdateImpuseCooldown;
            InertiaDampTime.OnValueChanged -= PlayerData.UpdateInertiaDampTime;
            ThrusterForce.OnValueChanged -= PlayerData.UpdateThrustForce;
            MaxThrustSpeed.OnValueChanged -= PlayerData.UpdateStabilizerMaxThrustSpeed;
            CostPerSecond.OnValueChanged -= PlayerData.UpdateFuelCostPerSecond;
            MaximunCapacity.OnValueChanged -= PlayerData.UpdateFuelCapacity;
            Recharge.OnValueChanged -= PlayerData.UpdateFuelRechargeWhenIdle;
            TimeBeforeRecharge.OnValueChanged -= PlayerData.UpdateFuelRechargeDelay;
        }
    }

    public void ConnectPlayerDataToUI()
    {
        ApplyRanges();

        ImpulseMode.UpdateValue((int)PlayerData.ImpulseResource.ImpulseMode);
        ImpulseForce.UpdateValue(PlayerData.ImpulseResource.ImpulseForce);
        ImpuseCooldown.UpdateValue(PlayerData.ImpulseResource.RechargeDuration);
        InertiaDampTime.UpdateValue(PlayerData.InertiaResource.InertiaDampTime);
        ThrusterForce.UpdateValue(PlayerData.ThrusterResource.ThrustForce);
        MaxThrustSpeed.UpdateValue(PlayerData.InertiaResource.StabilizerMaxThrustSpeed);
        if (PlayerData.FuelResource == null)
            return;

        CostPerSecond.UpdateValue(PlayerData.FuelResource.CostPerSecond);
        MaximunCapacity.UpdateValue(PlayerData.FuelResource.Capacity);
        Recharge.UpdateValue(PlayerData.FuelResource.RechargeWhenIdle);
        TimeBeforeRecharge.UpdateValue(PlayerData.FuelResource.RechargeDelay);
    }

    public void ApplyRanges()
    {
        ImpulseForce.UpdateValueRange(OrbiterTuning.ImpulseForceEditMin, OrbiterTuning.ImpulseForceEditMax);
        ImpuseCooldown.UpdateValueRange(OrbiterTuning.RechargeDurationEditMin, OrbiterTuning.RechargeDurationEditMax);
        InertiaDampTime.UpdateValueRange(OrbiterTuning.InertiaDampTimeEditMin, OrbiterTuning.InertiaDampTimeEditMax);
        ThrusterForce.UpdateValueRange(OrbiterTuning.ThrustForceEditMin, OrbiterTuning.ThrustForceEditMax);
        MaxThrustSpeed.UpdateValueRange(OrbiterTuning.StabilizerMaxThrustSpeedEditMin, OrbiterTuning.StabilizerMaxThrustSpeedEditMax);
        CostPerSecond.UpdateValueRange(OrbiterTuning.FuelCostPerSecondEditMin, OrbiterTuning.FuelCostPerSecondEditMax);
        MaximunCapacity.UpdateValueRange(OrbiterTuning.FuelCapacityEditMin, OrbiterTuning.FuelCapacityEditMax);
        TimeBeforeRecharge.UpdateValueRange(OrbiterTuning.FuelRechargeDelayEditMin, OrbiterTuning.FuelRechargeDelayEditMax);
    }
}

[Serializable]
public class PlayerEditableDataDependencies : PanelDependencies
{
    public PlayerData PlayerData;
    public Transform ImpulseMode;
    public Transform ImpulseForce;
    public Transform ImpuseCooldown;
    public Transform InertiaDampTime;
    public Transform ThrusterForce;
    public Transform MaxThrustSpeed;
    public Transform CostPerSecond;
    public Transform MaximunCapacity;
    public Transform Recharge;
    public Transform TimeBeforeRecharge;
    public Button Fill;
}
