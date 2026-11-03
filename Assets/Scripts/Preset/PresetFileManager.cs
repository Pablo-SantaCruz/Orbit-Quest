using System;
using System.IO;
using System.Linq;
using UnityEngine;

public static class PresetFileManager
{
    public const int CurrentVersion = 2;

    public static void LoadNames()
    {
        Debug.Log("Loading preset names from: " + Application.persistentDataPath);
        string[] presetNames = Directory.GetFiles(Application.persistentDataPath, "*.json").Select(path => Path.GetFileNameWithoutExtension(path)).ToArray();
        Debug.Log("Preset names: " + string.Join(", ", presetNames));
        PresetEvents.RaisePresetNamesLoadedEvent(presetNames);
    }

    public static void Write(string presetName, PresetFile file)
    {
        file.Version = CurrentVersion;
        string json = JsonUtility.ToJson(file, true);
        File.WriteAllText(GetPath(presetName), json);
    }

    public static void Read(string presetName)
    {
        string json = File.ReadAllText(GetPath(presetName));
        if (!TryParse(json, out PresetFile file))
            return;

        PresetEvents.RaisePresetFileLoadedEvent(file);
    }

    public static bool TryRead(string presetName, out PresetFile file)
    {
        string path = GetPath(presetName);
        if (!File.Exists(path))
        {
            file = new PresetFile
            {
                Version = CurrentVersion,
                Sections = Array.Empty<PresetSection>()
            };
            return true;
        }

        string json = File.ReadAllText(path);
        return TryParse(json, out file);
    }

    public static bool TryParse(string json, out PresetFile file)
    {
        file = null;
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogError("Preset file is empty.");
            return false;
        }

        PresetFile parsed = null;
        Exception parseError = null;
        try
        {
            parsed = JsonUtility.FromJson<PresetFile>(json);
        }
        catch (Exception exception)
        {
            parseError = exception;
        }

        // Legacy files have no Sections field, so JsonUtility leaves that array null.
        // Version is not enough to tell the formats apart.
        if (parsed != null && parsed.Sections != null)
        {
            file = parsed;
            return true;
        }

        if (json.Contains("AstroPresetEntries"))
        {
            try
            {
                PresetData legacy = JsonUtility.FromJson<PresetData>(json);
                file = FromLegacy(legacy);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"Legacy preset could not be adapted: {exception}");
                return false;
            }
        }

        if (parseError != null)
            Debug.LogError($"Preset file is invalid: {parseError}");
        else
            Debug.LogError("Preset file is invalid.");
        return false;
    }

    public static void Delete(string presetName)
    {
        File.Delete(GetPath(presetName));
        LoadNames();
    }

    public static bool NameExists(string presetName)
    {
        return File.Exists(GetPath(presetName));
    }

    static PresetFile FromLegacy(PresetData legacy)
    {
        return new PresetFile
        {
            Version = CurrentVersion,
            Sections = new[]
            {
                new PresetSection
                {
                    Id = AstroListPresetModule.ModuleId,
                    Payload = JsonUtility.ToJson(new AstroListPresetPayload { Entries = legacy.AstroPresetEntries })
                },
                new PresetSection
                {
                    Id = LimitsPresetModule.ModuleId,
                    Payload = JsonUtility.ToJson(legacy.Limits)
                },
                new PresetSection
                {
                    Id = PlayerSpawnPresetModule.ModuleId,
                    Payload = JsonUtility.ToJson(new PlayerSpawnPresetPayload { Position = legacy.PlayerSpawnPoint })
                }
            }
        };
    }

    static string GetPath(string presetName)
    {
        return Path.Combine(Application.persistentDataPath, presetName + ".json");
    }
}
