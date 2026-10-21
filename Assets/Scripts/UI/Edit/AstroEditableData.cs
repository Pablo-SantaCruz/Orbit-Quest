using UnityEngine;
using System;

public class AstroEditableData : IPanel
{
    public GameObject Root { get; private set; }
    readonly public DropdownComponent Type;
    readonly public SliderComponent OrbitGravity;
    readonly public SliderComponent OrbitRadius;
    readonly public SliderComponent BodyRadius;
    readonly public SliderComponent BodyRotationSpeed;
    readonly public SliderComponent ExplorationMinimumTime;
    public AstroEditableData(AstroEditableDataDependencies dependencies)
    {
        Root = dependencies.Root;
        Type = new DropdownComponent(dependencies.Type);
        OrbitGravity = new SliderComponent(dependencies.OrbitGravity);
        OrbitRadius = new SliderComponent(dependencies.OrbitRadius);
        BodyRadius = new SliderComponent(dependencies.BodyRadius);
        BodyRotationSpeed = new SliderComponent(dependencies.BodyRotationSpeed);
        ExplorationMinimumTime = new SliderComponent(dependencies.ExplorationMinimumTime);

        ApplyRanges();
    }

    public void ApplyRanges()
    {
        OrbitGravity.UpdateValueRange(AstroTuning.GravityEditMin, AstroTuning.GravityEditMax);
        OrbitRadius.UpdateValueRange(AstroTuning.OrbitRadiusEditMin, AstroTuning.OrbitRadiusEditMax);
        BodyRadius.UpdateValueRange(AstroTuning.BodyRadiusEditMin, AstroTuning.BodyRadiusEditMax);
        BodyRotationSpeed.UpdateValueRange(AstroTuning.RotationSpeedEditMin, AstroTuning.RotationSpeedEditMax);
        ExplorationMinimumTime.UpdateValueRange(AstroTuning.ExplorationMinimumTimeEditMin, AstroTuning.ExplorationMinimumTimeEditMax);
    }
    public void Toggle(bool active)
    {
        Root.SetActive(active);
    }
    public void InyectEditable(IEditable next, IEditable original = null)
    {
        if(next == null) return;

        if(original == next)
        {
            UpdateAstroDataToUI(next);
            return;
        }

        if(original != null)
        {
            UnsubscribeFromEditable(original);
        }
        SubscribeToEditable(next);
        
        UpdateAstroDataToUI(next);
    }
    public void ClearEditable(IEditable editable)
    {
        if (editable == null) return;
        UnsubscribeFromEditable(editable);
    }
    private void SubscribeToEditable(IEditable editable)
    {
        OrbitGravity.OnValueChanged += editable.UpdateOrbitGravity;
        OrbitRadius.OnValueChanged += editable.UpdateOrbitRadius;
        BodyRadius.OnValueChanged += editable.UpdateBodyRadius;
        BodyRotationSpeed.OnValueChanged += editable.UpdateBodyRotationSpeed;
        ExplorationMinimumTime.OnValueChanged += editable.UpdateExplorationMinimumTime;
    }
    private void UnsubscribeFromEditable(IEditable editable)
    {
        OrbitGravity.OnValueChanged -= editable.UpdateOrbitGravity;
        OrbitRadius.OnValueChanged -= editable.UpdateOrbitRadius;
        BodyRadius.OnValueChanged -= editable.UpdateBodyRadius;
        BodyRotationSpeed.OnValueChanged -= editable.UpdateBodyRotationSpeed;
        ExplorationMinimumTime.OnValueChanged -= editable.UpdateExplorationMinimumTime;
    }
    private void UpdateAstroDataToUI(IEditable editable)
    {
        Type.UpdateValue((int)editable.Data.type);
        OrbitGravity.UpdateValue(editable.Data.orbit.gravity);
        OrbitRadius.UpdateValue(editable.Data.orbit.radius);
        BodyRadius.UpdateValue(editable.Data.body.radius);
        BodyRotationSpeed.UpdateValue(editable.Data.body.rotationSpeed);

        if (editable.Data.type != AstroType.Sun)
        {
            ExplorationMinimumTime.UpdateValue(editable.Data.body.explorationMinimumTime);
            ExplorationMinimumTime.SetActive(true);
        }
        else
            ExplorationMinimumTime.SetActive(false);
    }
}

[Serializable]
public class AstroEditableDataDependencies : PanelDependencies
{
    public Transform Type;
    public Transform OrbitGravity;
    public Transform OrbitRadius;
    public Transform BodyRadius;
    public Transform BodyRotationSpeed;
    public Transform ExplorationMinimumTime;
}