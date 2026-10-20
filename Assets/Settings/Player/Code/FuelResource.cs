using UnityEngine;

[CreateAssetMenu(fileName = "FuelResource", menuName = "Proto-Pablo/Fuel Resource", order = 5)]
public class FuelResource : ScriptableObject
{
    [SerializeField, Range(OrbiterTuning.FuelCapacityMin, OrbiterTuning.FuelCapacityMax)]
    float _capacity = 100f;

    [SerializeField, Range(OrbiterTuning.FuelCostPerSecondMin, OrbiterTuning.FuelCostPerSecondMax)]
    float _costPerSecond = 10f;

    [SerializeField] bool _rechargeWhenIdle = true;

    [SerializeField, Range(OrbiterTuning.FuelRechargeDelayMin, OrbiterTuning.FuelRechargeDelayMax)]
    float _rechargeDelay = 2f;

    [SerializeField] float _amount = 100f;

    float _idleTimer;

    public float Capacity => _capacity;
    public float CostPerSecond => _costPerSecond;
    public bool RechargeWhenIdle => _rechargeWhenIdle;
    public float RechargeDelay => _rechargeDelay;
    public float Amount => _amount;

    public void ResetForSpawn()
    {
        _amount = _capacity;
        _idleTimer = 0f;
    }

    public void Fill()
    {
        _amount = _capacity;
        _idleTimer = 0f;
    }

    /// <summary>
    /// Spends this step of thrust. The force itself is unchanged: fuel only allows it or blocks it.
    /// A cost of 0 allows thrust without spending. An empty tank blocks it.
    /// </summary>
    public bool TryConsumeThrust(float deltaTime)
    {
        if (_costPerSecond <= 0f)
            return true;

        if (_amount <= 0f)
            return false;

        if (deltaTime > 0f)
            _amount = Mathf.Max(0f, _amount - _costPerSecond * deltaTime);

        return true;
    }

    /// <summary>Spends one second of fuel. Returns false when the tank cannot cover that cost.</summary>
    public bool TrySpendImpulse()
    {
        if (_costPerSecond <= 0f)
            return true;

        if (_amount < _costPerSecond)
            return false;

        _amount -= _costPerSecond;
        return true;
    }

    public void TickRecharge(bool isIdle, float deltaTime)
    {
        if (!_rechargeWhenIdle || _amount >= _capacity || !isIdle)
        {
            _idleTimer = 0f;
            return;
        }

        _idleTimer += deltaTime;
        if (_idleTimer < _rechargeDelay)
            return;

        float gain = _capacity * OrbiterTuning.FuelRechargeFractionPerSecond * deltaTime;
        _amount = Mathf.Min(_capacity, _amount + gain);
    }

    public void UpdateCostPerSecond(float value)
    {
        _costPerSecond = Mathf.Clamp(value, OrbiterTuning.FuelCostPerSecondEditMin, OrbiterTuning.FuelCostPerSecondEditMax);
    }

    public void UpdateCapacity(float value)
    {
        _capacity = Mathf.Clamp(value, OrbiterTuning.FuelCapacityEditMin, OrbiterTuning.FuelCapacityEditMax);
        _amount = Mathf.Min(_amount, _capacity);
    }

    public void UpdateRechargeWhenIdle(bool value)
    {
        _rechargeWhenIdle = value;
        if (!_rechargeWhenIdle)
            _idleTimer = 0f;
    }

    public void UpdateRechargeDelay(float value)
    {
        _rechargeDelay = Mathf.Clamp(value, OrbiterTuning.FuelRechargeDelayEditMin, OrbiterTuning.FuelRechargeDelayEditMax);
    }
}
