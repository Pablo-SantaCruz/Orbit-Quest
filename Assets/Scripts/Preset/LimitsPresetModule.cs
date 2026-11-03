using System;
using UnityEngine;

[CreateAssetMenu(fileName = "LimitsPresetModule", menuName = "Proto-Pablo/Presets/Limits", order = 2)]
public class LimitsPresetModule : PresetModuleAsset
{
    public const string ModuleId = "limits";

    public override string Id => ModuleId;

    public override string Capture(PresetSessionContext context)
    {
        return JsonUtility.ToJson(LimitsData.From(context.Limits));
    }

    public override void Apply(string payload, PresetSessionContext context)
    {
        if (string.IsNullOrEmpty(payload))
            return;

        try
        {
            if (!PresetPayloadJson.TryFromJson(payload, out LimitsData limits) || !payload.Contains("\"min\"") || !payload.Contains("\"max\""))
            {
                Debug.LogError($"Preset module '{Id}' could not parse payload.");
                return;
            }

            context.Limits.SetLimits(limits.min, limits.max);
        }
        catch (Exception exception)
        {
            Debug.LogError($"Preset module '{Id}' failed to apply payload: {exception}");
        }
    }
}
