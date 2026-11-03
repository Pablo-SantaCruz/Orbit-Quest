using System;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerSpawnPresetModule", menuName = "Proto-Pablo/Presets/Player Spawn", order = 3)]
public class PlayerSpawnPresetModule : PresetModuleAsset
{
    public const string ModuleId = "playerSpawn";

    public override string Id => ModuleId;

    public override string Capture(PresetSessionContext context)
    {
        return JsonUtility.ToJson(new PlayerSpawnPresetPayload
        {
            Position = context.PlayerSpawn.position
        });
    }

    public override void Apply(string payload, PresetSessionContext context)
    {
        if (string.IsNullOrEmpty(payload))
            return;

        try
        {
            if (!PresetPayloadJson.TryFromJson(payload, out PlayerSpawnPresetPayload parsed) || !payload.Contains("\"Position\""))
            {
                Debug.LogError($"Preset module '{Id}' could not parse payload.");
                return;
            }

            context.PlayerSpawn.position = parsed.Position;
        }
        catch (Exception exception)
        {
            Debug.LogError($"Preset module '{Id}' failed to apply payload: {exception}");
        }
    }
}

[Serializable]
public struct PlayerSpawnPresetPayload
{
    public Vector3 Position;
}
