using System;
using UnityEngine;

[CreateAssetMenu(fileName = "AstroListPresetModule", menuName = "Proto-Pablo/Presets/Astro List", order = 1)]
public class AstroListPresetModule : PresetModuleAsset
{
    public const string ModuleId = "astros";

    public override string Id => ModuleId;

    public override string Capture(PresetSessionContext context)
    {
        return JsonUtility.ToJson(new AstroListPresetPayload
        {
            Entries = context.AstroManager.CapturePresetEntries()
        });
    }

    public override void Apply(string payload, PresetSessionContext context)
    {
        if (string.IsNullOrEmpty(payload))
            return;

        try
        {
            if (!PresetPayloadJson.TryFromJson(payload, out AstroListPresetPayload parsed) || parsed.Entries == null)
            {
                Debug.LogError($"Preset module '{Id}' could not parse payload.");
                return;
            }

            context.AstroManager.ApplyPresetEntries(parsed.Entries);
        }
        catch (Exception exception)
        {
            Debug.LogError($"Preset module '{Id}' failed to apply payload: {exception}");
        }
    }
}

[Serializable]
public struct AstroListPresetPayload
{
    public AstroPresetEntry[] Entries;
}
