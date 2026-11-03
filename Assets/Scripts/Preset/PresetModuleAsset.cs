using UnityEngine;

public abstract class PresetModuleAsset : ScriptableObject, IPresetModule
{
    public abstract string Id { get; }
    public abstract string Capture(PresetSessionContext context);
    public abstract void Apply(string payload, PresetSessionContext context);
}
