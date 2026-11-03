using System;
using UnityEngine;

public class PresetSessionContext
{
    public AstroManager AstroManager { get; }
    public Limits Limits { get; }
    public Transform PlayerSpawn { get; }
    public PlayerData PlayerData { get; }

    public PresetSessionContext(AstroManager astroManager, Limits limits, Transform playerSpawn, PlayerData playerData)
    {
        AstroManager = astroManager;
        Limits = limits;
        PlayerSpawn = playerSpawn;
        PlayerData = playerData;
    }
}

public class PresetSession : MonoBehaviour
{
    [SerializeField] PresetSchema _schema;
    [SerializeField] AstroManager _astroManager;
    [SerializeField] Limits _limits;
    [SerializeField] Transform _playerSpawn;
    [SerializeField] PlayerData _playerData;

    void OnEnable()
    {
        PresetEvents.OnPresetSaved += HandlePresetSaved;
        PresetEvents.OnPresetFileLoaded += HandlePresetFileLoaded;
    }

    void OnDisable()
    {
        PresetEvents.OnPresetSaved -= HandlePresetSaved;
        PresetEvents.OnPresetFileLoaded -= HandlePresetFileLoaded;
    }

    void HandlePresetSaved(string presetName)
    {
        if (_schema == null || !_schema.HasAnyModule)
        {
            Debug.LogError("Preset save aborted: schema is missing or has no modules.");
            return;
        }

        try
        {
            if (!PresetFileManager.TryRead(presetName, out PresetFile file))
            {
                Debug.LogError($"Preset save aborted: '{presetName}' was not replaced.");
                return;
            }

            file.Sections = _schema.CaptureAll(CreateContext(), file.Sections);
            file.Version = PresetFileManager.CurrentVersion;
            PresetFileManager.Write(presetName, file);
        }
        catch (Exception exception)
        {
            Debug.LogError($"Preset save aborted: '{presetName}' was not replaced. {exception}");
        }
    }

    void HandlePresetFileLoaded(PresetFile file)
    {
        if (_schema == null)
        {
            Debug.LogError("Preset apply aborted: schema is missing.");
            return;
        }

        try
        {
            _schema.ApplyAll(file, CreateContext());
        }
        catch (Exception exception)
        {
            Debug.LogError($"Preset apply aborted: {exception}");
            return;
        }

        PresetEvents.RaisePresetAppliedEvent();
    }

    PresetSessionContext CreateContext()
    {
        return new PresetSessionContext(_astroManager, _limits, _playerSpawn, _playerData);
    }
}
