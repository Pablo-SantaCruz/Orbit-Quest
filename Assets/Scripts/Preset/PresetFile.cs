using System;
using UnityEngine;

[Serializable]
public class PresetFile
{
    public int Version;
    public PresetSection[] Sections;
}

[Serializable]
public struct PresetSection
{
    public string Id;
    public string Payload;
}

public static class PresetPayloadJson
{
    public static bool TryFromJson<T>(string json, out T value) where T : struct
    {
        value = default;
        if (!IsJsonObject(json))
            return false;

        try
        {
            value = JsonUtility.FromJson<T>(json);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    static bool IsJsonObject(string json)
    {
        if (string.IsNullOrEmpty(json))
            return false;

        int start = 0;
        int end = json.Length - 1;
        while (start <= end && char.IsWhiteSpace(json[start]))
            start++;
        while (end >= start && char.IsWhiteSpace(json[end]))
            end--;

        return end > start && json[start] == '{' && json[end] == '}';
    }
}
