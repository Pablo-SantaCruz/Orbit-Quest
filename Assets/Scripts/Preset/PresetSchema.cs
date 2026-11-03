using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PresetSchema", menuName = "Proto-Pablo/Presets/Schema", order = 0)]
public class PresetSchema : ScriptableObject
{
    [SerializeField] PresetModuleAsset[] _modules;

    public PresetModuleAsset[] Modules => _modules;

    public bool HasAnyModule
    {
        get
        {
            if (_modules == null)
                return false;

            for (int i = 0; i < _modules.Length; i++)
            {
                if (_modules[i] != null)
                    return true;
            }

            return false;
        }
    }

    public PresetSection[] CaptureAll(PresetSessionContext context, PresetSection[] existingSections)
    {
        var sections = new List<PresetSection>();
        if (existingSections != null)
            sections.AddRange(existingSections);

        if (_modules == null)
            return sections.ToArray();

        for (int i = 0; i < _modules.Length; i++)
        {
            PresetModuleAsset module = _modules[i];
            if (module == null)
                continue;

            string payload = module.Capture(context);
            if (payload == null)
                continue;

            ReplaceOrAdd(sections, module.Id, payload);
        }

        return sections.ToArray();
    }

    public void ApplyAll(PresetFile file, PresetSessionContext context)
    {
        if (_modules == null)
            return;

        for (int i = 0; i < _modules.Length; i++)
        {
            PresetModuleAsset module = _modules[i];
            if (module == null)
                continue;

            try
            {
                module.Apply(FindPayload(file, module.Id), context);
            }
            catch (Exception exception)
            {
                Debug.LogError($"Preset module '{module.Id}' failed: {exception}");
            }
        }
    }

    static string FindPayload(PresetFile file, string id)
    {
        if (file == null || file.Sections == null)
            return null;

        for (int i = 0; i < file.Sections.Length; i++)
        {
            if (file.Sections[i].Id == id)
                return file.Sections[i].Payload;
        }

        return null;
    }

    static void ReplaceOrAdd(List<PresetSection> sections, string id, string payload)
    {
        for (int i = 0; i < sections.Count; i++)
        {
            if (sections[i].Id != id)
                continue;

            PresetSection section = sections[i];
            section.Payload = payload;
            sections[i] = section;
            return;
        }

        sections.Add(new PresetSection { Id = id, Payload = payload });
    }
}
