using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class Astro : MonoBehaviour, IPointerDownHandler, IEditable, IDragHandler
{
    public static event Action<IEditable> OnEditableClicked;
    public event Action OnEditableDragged;

    [Header("Settings")]
    [SerializeField] PlayerData _playerData;
    [SerializeField] LevelData _levelData;
    [SerializeField] OrbitData _orbitData;
    [SerializeField] BodyData _bodyData;
    [SerializeField] float _rotationSpeed = 5f;
    [SerializeField] float _explorationMinimumTime = AstroTuning.ExplorationMinimumTimeDefault;
    [SerializeField] Transform _transform, _baseTransform, _orbitTransform;
    [SerializeField] LineRenderer _orbitPathRenderer;
    [SerializeField] LineRenderer _explorationLine;

    private AstroSpawnPreset _spawnSource;
    private IOrbitable _orbit;
    private BodyShader _baseShader;
    private TransformOrbiter _orbiter;
    private bool _isSelected;
    private float _accumulatedExplorationTime;
    private bool _isExplored;

    public float BodyRadius => _bodyData.radius;
    public bool IsExplored => _isExplored;
    public float ExplorationPercent
    {
        get
        {
            float minimumTime = Mathf.Max(_explorationMinimumTime, 0.0001f);
            return Mathf.Clamp(_accumulatedExplorationTime / minimumTime, 0f, 1f) * 100f;
        }
    }
    public bool HasOrbiter() => _orbiter != null;
    public LineRenderer OrbitPathRenderer => _orbitPathRenderer;
    public GameState CurrentGameState => _levelData?.CurrentState ?? GameState.None;

    public EditableData Data => new EditableData
    {
        type = _orbitData.type,
        body = new EditableBodyData
        {
            radius = _bodyData.radius,
            rotationSpeed = _rotationSpeed,
            explorationMinimumTime = _explorationMinimumTime
        },
        orbit = new EditableOrbitData
        {
            radius = _orbitData.radius,
            gravity = _orbitData.gravity
        },
        orbiter = new EditableOrbiterData
        {
            speed = _orbiter?.Speed ?? 0f,
            radius = _orbiter?.Radius ?? 0f,
            eccentricity = _orbiter?.Eccentricity ?? 0f,
            targets = _orbiter?.TargetsEditable?? new IEditable[0]
        }
    };

    public AstroPresetEntry ToAstroPresetEntry()
    {
        return new AstroPresetEntry
        {
            Position = transform.position,
            EditableData = Data
        };
    }

    void Awake()
    {
        CacheReferences();
    }
    void OnEnable()
    {
        Apply();
        ApplyOrbitPathPresence();
    }
    void OnValidate()
    {
        CacheReferences();
        UpdateBaseValues();
        UpdateOrbitValues();
        ApplyOrbitPathPresence();
    }
    void Update()
    {
        _transform.Rotate(Vector3.forward * _rotationSpeed * Time.deltaTime);
    }
    void CacheReferences()
    {
        if(!_transform) _transform = transform;
        if(!_baseTransform) _baseTransform = _transform.Find("Base");
        if(!_orbitTransform) _orbitTransform = _transform.Find("Orbit");
        if(_orbit == null) _orbit = _orbitTransform?.GetComponent<IOrbitable>();
        if(_baseShader == null) _baseShader = new BodyShader(_baseTransform?.GetComponent<Renderer>());
        if(_orbiter == null) _orbiter = GetComponent<TransformOrbiter>();
        if(_explorationLine == null && _transform != null)
        {
            Transform line = _transform.Find("Exploration Line");
            if (line != null)
                _explorationLine = line.GetComponent<LineRenderer>();
        }
    }
    void UpdateBaseValues()
    {
        float diameter = _bodyData.radius * 2f;
        Color desiredColor = _isSelected ? _bodyData.selectedColor : _bodyData.baseColor;

        _baseTransform.localScale = Vector3.one * diameter;
        _baseShader?.SetColor(desiredColor);
        if (Application.isPlaying)
            ApplyExplorationLine();
    }
    void UpdateOrbitValues()
    {
        float diameter = _orbitData.radius * 2f;

        _orbitTransform.localScale = Vector3.one * diameter;
        _orbit?.SetData(_orbitData);
    }
    void Apply()
    {
        UpdateBaseValues();
        UpdateOrbitValues();
    }
    void ApplyOrbitPathPresence()
    {
        if (_orbitPathRenderer == null)
            return;

        GameObject pathObject = _orbitPathRenderer.gameObject;
        bool active = GetComponent<TransformOrbiter>() != null;
        if (pathObject.activeSelf != active)
            pathObject.SetActive(active);
    }

    public void Selected()
    {
        _isSelected = true;
        Apply();
    }

    public void Deselected()
    {
        _isSelected = false;
        Apply();
    }

    /// <summary>
    /// Sets orbit and body data (e.g. when spawning from pool) and refreshes visuals. Type is defined by the prefab, not here.
    /// </summary>
    public void Initialize(OrbitData orbitData, BodyData bodyData, AstroSpawnPreset spawnSource = null)
    {
        _orbitData = orbitData;
        _bodyData = bodyData;
        _spawnSource = spawnSource;
        // Pooled instances survive ClearAstros. A new spawn starts unexplored and
        // drops any minimum edited on the instance that previously used this object.
        ResetExploration();
        UpdateExplorationMinimumTime(AstroTuning.ExplorationMinimumTimeDefault);
        Apply();
    }

    public void TickExploration(float deltaTime)
    {
        if (_isExplored || deltaTime <= 0f)
            return;

        float minimumTime = Mathf.Max(_explorationMinimumTime, 0.0001f);
        _accumulatedExplorationTime = Mathf.Min(_accumulatedExplorationTime + deltaTime, minimumTime);
        if (_accumulatedExplorationTime >= minimumTime)
            _isExplored = true;
    }

    void ResetExploration()
    {
        _accumulatedExplorationTime = 0f;
        _isExplored = false;
    }

    void ApplyExplorationLine()
    {
        if (_explorationLine == null)
            return;

        _explorationLine.useWorldSpace = false;
        _explorationLine.positionCount = 2;

        float radius = Mathf.Max(_bodyData.radius, 0.01f);
        float width = Mathf.Max(0.03125f, radius * 0.08f);
        _explorationLine.startWidth = width;
        _explorationLine.endWidth = width;

        // Local Y is perpendicular to the spin axis (local forward). Sit just outside the mesh.
        float reach = radius * 1.05f;
        _explorationLine.SetPosition(0, new Vector3(reach, -radius, 0f));
        _explorationLine.SetPosition(1, new Vector3(reach, radius, 0f));
    }

    public void Deactivate()
    {
        gameObject.SetActive(false);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if(!_levelData.IsInEditMode || eventData.button != PointerEventData.InputButton.Left) return;

        Vector3 desiredPosition = _playerData.CursorWorld;
        desiredPosition.y = 0;
        _transform.position = desiredPosition;

        OnEditableDragged?.Invoke();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if(eventData.button != PointerEventData.InputButton.Left) return;

        OnEditableClicked?.Invoke(this);
    }

    public void UpdateBodyRadius(float radius)
    {
        _bodyData.radius = radius;
        UpdateBaseValues();
    }

    public void UpdateBodyRotationSpeed(float rotationSpeed)
    {
        _rotationSpeed = rotationSpeed;
    }

    public void UpdateExplorationMinimumTime(float minimumTime)
    {
        if (minimumTime <= 0f)
            minimumTime = AstroTuning.ExplorationMinimumTimeDefault;

        _explorationMinimumTime = minimumTime;
        if (_accumulatedExplorationTime < _explorationMinimumTime)
            _isExplored = false;
    }

    public void UpdateOrbitRadius(float radius)
    {
        _orbitData.radius = radius;
        UpdateOrbitValues();
    }

    public void UpdateOrbitGravity(float gravity)
    {
        _orbitData.gravity = gravity;
        UpdateOrbitValues();
    }

    public void UpdateOrbiterSpeed(float speed)
    {
        if (_orbiter == null) return;
        _orbiter.SetSpeed(speed);
    }

    public void UpdateOrbiterRadius(float radius)
    {
        if (_orbiter == null) return;
        _orbiter.SetRadius(radius);
    }

    public void UpdateOrbiterEccentricity(float eccentricity)
    {
        if (_orbiter == null) return;
        _orbiter.SetEccentricity(eccentricity);
    }

    public void UpdateOrbiterTargets(IEditable[] targets)
    {
        if (_orbiter == null) return;
        _orbiter.SetTargets(targets);
    }
}
[Serializable]
public struct BodyData
{
    [Range(0.5f, 7.5f)] public float radius;
    public Color baseColor;
    public Color selectedColor;
}
