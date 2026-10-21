using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Orb : MonoBehaviour, ILimitable
{
#region ILevelBounds
    public Limits Limits { get; private set; }
    public void SetLimits(Limits limits)
    {
        Limits = limits;
    }
#endregion
    public static event Action OnOrbitEnter, OnOrbitExit, OnSpawn, OnDespawn;

    [SerializeField] PlayerData _playerData;
    [SerializeField] LevelData _levelData;
    [SerializeField] LineRenderer _trajectoryRenderer;
    [SerializeField] LineRenderer _directionRenderer;
    [SerializeField] LineRenderer _tensionRenderer;
    private RigidbodyOrbiter _orbiterController;
    private LineRendererController _lineRendererController;
    private Rigidbody _rb;
    private Vector3 _thrustInput;
    private Vector2 _aimDirection;
    private bool _isAiming;

    public Astro CapturedAstro
    {
        get
        {
            IOrbitable captured = _orbiterController?.CapturedOrbit;
            if (captured is not Component component || component == null)
                return null;

            return component.GetComponentInParent<Astro>();
        }
    }

    public void SetInertiaStabilizer(bool value)
    {
        if (_playerData.InertiaResource == null) return;
        _playerData.UpdateInertiaStabilizer(value);
        SyncOrbiterFromConfig();
    }

    public void ToggleInertiaStabilizer()
    {
        if (_playerData.InertiaResource == null) return;
        SetInertiaStabilizer(!_playerData.InertiaResource.InertiaStabilizer);
    }

    void Awake()
    {
        if (_playerData == null || _playerData.ImpulseResource == null)
            Debug.LogError("Orb requires OrbiterConfig and ImpulseResource references.", this);

        _rb = GetComponent<Rigidbody>();
        _orbiterController = new RigidbodyOrbiter(_rb, transform, OnOrbitEnter, OnOrbitExit,
            _playerData != null ? _playerData.ToOrbiterSettings() : OrbiterSettings.Default);
        _lineRendererController = new LineRendererController(_trajectoryRenderer, _directionRenderer, _tensionRenderer, _orbiterController, transform.localScale.x);
    }

    void OnEnable()
    {
        _thrustInput = Vector3.zero;
        _aimDirection = Vector2.zero;
        _orbiterController?.OnEnable();
        _playerData.ImpulseResource?.ResetForSpawn();
        _playerData.FuelResource?.ResetForSpawn();
        OnSpawn?.Invoke();

        _playerData.OrbiterConfigUpdated += SyncOrbiterFromConfig;
        _playerData.SetPlayerStatus(PlayerStatus.Alive);
    }

    void FixedUpdate()
    {
        bool thrustAllowed = _playerData.FuelResource == null
            || _thrustInput.sqrMagnitude <= 0.0001f
            || _playerData.FuelResource.TryConsumeThrust(Time.fixedDeltaTime);

        _orbiterController?.FixedUpdate(_thrustInput);
        if (thrustAllowed)
            _orbiterController?.ApplyThrust(_thrustInput);
    }

    void Update()
    {
        _playerData.ImpulseResource?.Tick(Time.deltaTime);
        TickCapturedExploration();

        if (_playerData.FuelResource == null || _rb == null)
            return;

        float idleSpeed = OrbiterTuning.FuelIdleSpeed;
        bool idle = _rb.linearVelocity.sqrMagnitude <= idleSpeed * idleSpeed;
        _playerData.FuelResource.TickRecharge(idle, Time.deltaTime);
    }

    /// <summary>Called by the Move input action (e.g. from PlayerController). Passes the current movement direction.</summary>
    public void SetThrustInput(Vector3 value)
    {
        _thrustInput = value;
    }

    /// <summary>Sets the aim direction (world space, e.g. from mouse). Used to orient the orb and for thrust direction.</summary>
    public void SetAimDirection(Vector2 worldDirection)
    {
        _aimDirection = worldDirection;
    }

    /// <summary>Applies a single thrust impulse in the given world-space direction. Called e.g. on right click.</summary>
    public void ApplyThrustImpulse(Vector3 worldDirection)
    {
        if (worldDirection.sqrMagnitude < 0.0001f) return;
        _orbiterController.ApplyThrust(worldDirection);
    }

    void LateUpdate()
    {
        if (!Limits.IsInside(transform.position.x, transform.position.z)) gameObject.SetActive(false);

        if (_isAiming)
        {
            _lineRendererController.UpdateTrajectory(_playerData.CursorWorld);
        }

        _lineRendererController.UpdateDirection(transform.position, _rb.linearVelocity);

        float deviation = 0f;
        Vector3 center = transform.position;
        bool precision = _levelData != null && _levelData.CurrentState == GameState.Precision;
        bool sampled = precision && _orbiterController.TryEvaluateCircularDeviation(out deviation, out center);
        float stability = sampled
            ? Mathf.Clamp01(1f - deviation / OrbiterTuning.CircularRedDeviation)
            : 0f;
        _lineRendererController.UpdateTension(sampled, transform.position, center, stability);

        if (_aimDirection.sqrMagnitude > 0.0001f)
        {
            float angleDeg = Mathf.Atan2(_aimDirection.x, _aimDirection.y) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, angleDeg, 0f);
        }
        _playerData.ThrusterResource.UpdateSpeed(_orbiterController.Speed);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out IOrbitable orbit))
            _orbiterController.AddGravitySource(orbit);
    }

    void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out IOrbitable orbit))
            _orbiterController.RemoveGravitySource(orbit);
    }

    void OnValidate()
    {
        if (!Application.isPlaying || _orbiterController == null || _playerData == null)
            return;
        SyncOrbiterFromConfig();
    }

    void OnCollisionEnter(Collision collision)
    {
        gameObject.SetActive(false);
    }

    void OnDisable()
    {
        _thrustInput = Vector3.zero;
        SetAiming(false);
        _orbiterController?.OnDisable();
        OnDespawn?.Invoke();

        _playerData.OrbiterConfigUpdated -= SyncOrbiterFromConfig;
        _playerData.SetPlayerStatus(PlayerStatus.Dead);
    }

    public void SetAiming(bool active)
    {
        _isAiming = active;
        bool cursorMode = _playerData != null && _playerData.ImpulseResource.ImpulseMode == ImpulseMode.Cursor;
        _lineRendererController.SetAiming(active && cursorMode);
    }

    public void Impulse(Vector3 cursorWorldPosition)
    {
        if (_playerData.ImpulseResource == null || !_playerData.ImpulseResource.IsReady)
            return;

        if (_playerData.FuelResource != null && !_playerData.FuelResource.TrySpendImpulse())
            return;

        if (!_playerData.ImpulseResource.TryConsumeImpulse())
            return;

        _orbiterController.Impulse(cursorWorldPosition);
    }

    void SyncOrbiterFromConfig()
    {
        if (_playerData == null) return;
        _orbiterController?.UpdateSettings(_playerData.ToOrbiterSettings());
    }

    void TickCapturedExploration()
    {
        if (_levelData == null || _levelData.IsPaused)
            return;

        GameState state = _levelData.CurrentState;
        if (state != GameState.Precision && state != GameState.Contemplative)
            return;

        Astro captured = CapturedAstro;
        if (captured == null || !captured.isActiveAndEnabled)
            return;

        captured.TickExploration(Time.deltaTime);
    }
}
