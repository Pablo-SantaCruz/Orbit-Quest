using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class PlayerInfo : IPanel
{
    public GameObject Root {get; private set;}
    public readonly TextMeshProUGUI SpeedText;
    public readonly Image ImpulseBar;
    public readonly Image InertiaStabilizer;
    public readonly Image FuelBar;


    public PlayerInfo(PlayerInfoDependencies dependencies)
    {
        Root = dependencies.Root;
        SpeedText = dependencies.SpeedText;
        ImpulseBar = dependencies.ImpulseBar;
        InertiaStabilizer = dependencies.InertiaStabilizer;
        FuelBar = dependencies.FuelBar != null ? dependencies.FuelBar : FindFuelFill(dependencies.Root);
    }
    public void UpdateSpeedText(float speed)
    {
        SpeedText.text = speed.ToString("F1");
    }
    public void UpdateImpulseBar(float impulse)
    {
        Color status = impulse == 1f ? Color.white : Color.gray;
        ImpulseBar.color = status;
        ImpulseBar.fillAmount = impulse;
    }
    public void UpdateInertiaStabilizerText(bool inertiaStabilizer)
    {
        Color status = inertiaStabilizer ? Color.green : Color.yellow;
        InertiaStabilizer.color = status;
    }
    public void UpdateFuelBar(float normalizedFuel)
    {
        if (FuelBar == null)
            return;

        FuelBar.fillAmount = Mathf.Clamp01(normalizedFuel);
    }

    public void Toggle(bool active)
    {
        Root.SetActive(active);
    }

    static Image FindFuelFill(GameObject root)
    {
        if (root == null)
            return null;

        Transform fuel = root.transform.Find("Fuel");
        if (fuel == null)
            return null;

        Transform fill = fuel.Find("Fill");
        if (fill != null && fill.TryGetComponent(out Image fillImage))
            return fillImage;

        fuel.TryGetComponent(out Image fuelImage);
        return fuelImage;
    }
}
[Serializable]
public class PlayerInfoDependencies : PanelDependencies
{
    public TextMeshProUGUI SpeedText;
    public Image ImpulseBar;
    public Image InertiaStabilizer;
    public Image FuelBar;
}