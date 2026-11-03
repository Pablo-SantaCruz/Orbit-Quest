using System;

public static class PresetEvents
{
    public static event Action<PresetNameComponent> OnPresetSelected;
    public static event Action<PresetFile> OnPresetFileLoaded;
    public static event Action OnPresetApplied;
    public static event Action<string[]> OnPresetNamesLoaded;
    public static event Action<string> OnPresetSaved;
    public static void RaisePresetSelectedEvent(PresetNameComponent preset)
    {
        OnPresetSelected?.Invoke(preset);
    }
    public static void RaisePresetFileLoadedEvent(PresetFile file)
    {
        OnPresetFileLoaded?.Invoke(file);
    }
    public static void RaisePresetAppliedEvent()
    {
        OnPresetApplied?.Invoke();
    }
    public static void RaisePresetNamesLoadedEvent(string[] presetNames)
    {
        OnPresetNamesLoaded?.Invoke(presetNames);
    }
    public static void RaisePresetSavedEvent(string presetName)
    {
        OnPresetSaved?.Invoke(presetName);
    }
}