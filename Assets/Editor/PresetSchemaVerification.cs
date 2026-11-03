using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PresetSchemaVerification
{
    const string Prefix = "preset-schema-verification-";

    public static void Run()
    {
        var files = new List<string>();
        try
        {
            Directory.CreateDirectory(Application.persistentDataPath);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Expect(UnityEngine.Object.FindAnyObjectByType<PresetSession>() == null, "verification scene was not empty");

            LegacyFileAdaptsWithoutOrbOrVersion(files);
            InvalidFileIsRejected(files);
            LegacyApplyRestoresLimitsSpawnAndSkipsPlayer(files);
            BadAstroPayloadDoesNotClearAndBadOrbDoesNotBlockOthers(files);
            EmptyAstroArrayClearsThenAppliesLimits(files);
            HasFuelFalseDoesNotWriteFuel();
            NullSchemaOrThrowingCaptureDoesNotReplaceFile(files);
            RemovingOrbModuleKeepsOrbSection(files);
            NextSaveWritesVersionTwoAndKeepsUnknownSections(files);
            ReadDoesNotWriteDisk(files);
            SceneWiringMatchesSchema();

            Debug.Log("PRESET_VERIFICATION_OK");
            DeleteFiles(files);
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogError("PRESET_VERIFICATION_FAILED " + exception);
            DeleteFiles(files);
            EditorApplication.Exit(1);
        }
    }

    static void LegacyFileAdaptsWithoutOrbOrVersion(List<string> files)
    {
        AstroPresetEntry[] entries = new AstroPresetEntry[1];
        entries[0].Position = new Vector3(4f, 5f, 6f);
        EditableData editable = entries[0].EditableData;
        EditableOrbiterData orbiter = editable.orbiter;
        orbiter.targetIndices = new[] { 1, 2 };
        editable.orbiter = orbiter;
        entries[0].EditableData = editable;

        var legacy = new PresetData(
            new LimitsData(new Vector2(-10f, -20f), new Vector2(30f, 40f)),
            entries,
            new Vector3(1f, 2f, 3f));
        string json = JsonUtility.ToJson(legacy);
        int closingBrace = json.LastIndexOf('}');
        json = json.Insert(closingBrace, ",\"Version\":2");

        Expect(PresetFileManager.TryParse(json, out PresetFile file), "legacy with a Version field was rejected");
        Expect(file.Sections != null && file.Sections.Length == 3, "legacy should adapt to three sections");
        Expect(Section(file, "astros") != null, "missing astros section");
        Expect(Section(file, "limits") != null, "missing limits section");
        Expect(Section(file, "playerSpawn") != null, "missing playerSpawn section");
        Expect(Section(file, "orb") == null, "legacy adapt invented an orb section");

        Expect(PresetPayloadJson.TryFromJson(Section(file, "astros"), out AstroListPresetPayload astros), "astro payload did not parse");
        Expect(astros.Entries != null && astros.Entries.Length == 1, "astro entries were not reused");
        Expect(astros.Entries[0].EditableData.orbiter.targetIndices != null, "target indices were dropped");
        Expect(astros.Entries[0].EditableData.orbiter.targetIndices.Length == 2, "target index count changed");
        Expect(astros.Entries[0].EditableData.orbiter.targetIndices[0] == 1, "first target index changed");
        Expect(astros.Entries[0].EditableData.orbiter.targetIndices[1] == 2, "second target index changed");
        ExpectNear(astros.Entries[0].Position.x, 4f, "astro position");

        Expect(PresetPayloadJson.TryFromJson(Section(file, "limits"), out LimitsData limits), "limits payload did not parse");
        ExpectNear(limits.min.x, -10f, "limits min");
        ExpectNear(limits.max.y, 40f, "limits max");
        Expect(PresetPayloadJson.TryFromJson(Section(file, "playerSpawn"), out PlayerSpawnPresetPayload spawn), "spawn payload did not parse");
        ExpectNear(spawn.Position.z, 3f, "spawn position");
        Expect(!Section(file, "astros").Contains("AstroPresetEntries"), "astro payload kept the legacy key");
    }

    static void InvalidFileIsRejected(List<string> files)
    {
        Expect(!PresetFileManager.TryParse("{\"hello\":1}", out _), "unrelated JSON was accepted");
        Expect(!PresetFileManager.TryParse("", out _), "empty JSON was accepted");
    }

    static void LegacyApplyRestoresLimitsSpawnAndSkipsPlayer(List<string> files)
    {
        using Fixture fixture = Fixture.Create(withSentinel: true);
        fixture.Player.UpdateImpulseForce(9f);
        fixture.Player.UpdateFuelCapacity(80f);

        var legacy = new PresetData(
            new LimitsData(new Vector2(-10f, -20f), new Vector2(30f, 40f)),
            Array.Empty<AstroPresetEntry>(),
            new Vector3(1f, 2f, 3f));
        string json = JsonUtility.ToJson(legacy);
        Expect(PresetFileManager.TryParse(json, out PresetFile file), "empty legacy preset was rejected");

        int applied = 0;
        void OnApplied() { applied++; }
        PresetEvents.OnPresetApplied += OnApplied;
        try
        {
            using (CreateSession(LoadSchemaAsset(), fixture))
            {
                PresetEvents.RaisePresetFileLoadedEvent(file);
            }
        }
        finally
        {
            PresetEvents.OnPresetApplied -= OnApplied;
        }

        Expect(applied == 1, "successful apply did not notify the UI");
        Expect(AstroCount(fixture.Manager) == 0, "empty astro array did not clear");
        ExpectNear(fixture.Limits.Min.x, -10f, "applied limits min");
        ExpectNear(fixture.Limits.Max.x, 30f, "applied limits max");
        ExpectNear(fixture.Spawn.position.x, 1f, "applied spawn");
        ExpectNear(fixture.Spawn.position.y, 2f, "applied spawn y");
        ExpectNear(fixture.Spawn.position.z, 3f, "applied spawn z");
        ExpectNear(fixture.Impulse.ImpulseForce, 9f, "legacy apply called impulse updates");
        ExpectNear(fixture.Fuel.Capacity, 80f, "legacy apply called fuel updates");
    }

    static void BadAstroPayloadDoesNotClearAndBadOrbDoesNotBlockOthers(List<string> files)
    {
        using Fixture fixture = Fixture.Create(withSentinel: true);
        fixture.Player.UpdateImpulseForce(9f);
        fixture.Player.UpdateFuelCapacity(80f);
        var file = new PresetFile
        {
            Version = 2,
            Sections = new[]
            {
                new PresetSection { Id = "astros", Payload = "not-json" },
                new PresetSection { Id = "limits", Payload = JsonUtility.ToJson(new LimitsData(new Vector2(-3f, -4f), new Vector2(5f, 6f))) },
                new PresetSection { Id = "playerSpawn", Payload = JsonUtility.ToJson(new PlayerSpawnPresetPayload { Position = new Vector3(7f, 8f, 9f) }) },
                new PresetSection { Id = "orb", Payload = "still-not-json" }
            }
        };

        LoadSchemaAsset().ApplyAll(file, fixture.Context);

        Expect(AstroCount(fixture.Manager) == 1, "illegible astro payload cleared the scene");
        ExpectNear(fixture.Limits.Min.x, -3f, "limits were skipped after a bad astro payload");
        ExpectNear(fixture.Limits.Max.y, 6f, "limits max were skipped");
        ExpectNear(fixture.Spawn.position.x, 7f, "spawn was skipped after a bad orb payload");
        ExpectNear(fixture.Impulse.ImpulseForce, 9f, "illegible orb payload changed impulse");
        ExpectNear(fixture.Fuel.Capacity, 80f, "illegible orb payload changed fuel");
    }

    static void EmptyAstroArrayClearsThenAppliesLimits(List<string> files)
    {
        using Fixture fixture = Fixture.Create(withSentinel: true);
        var file = new PresetFile
        {
            Version = 2,
            Sections = new[]
            {
                new PresetSection { Id = "astros", Payload = "{\"Entries\":[]}" },
                new PresetSection { Id = "limits", Payload = JsonUtility.ToJson(new LimitsData(new Vector2(2f, 3f), new Vector2(4f, 5f))) },
                new PresetSection { Id = "playerSpawn", Payload = JsonUtility.ToJson(new PlayerSpawnPresetPayload { Position = new Vector3(0f, 1f, 0f) }) }
            }
        };

        LoadSchemaAsset().ApplyAll(file, fixture.Context);
        Expect(AstroCount(fixture.Manager) == 0, "present empty astro array did not clear");
        ExpectNear(fixture.Limits.Min.y, 3f, "limits did not apply after an empty astro array");
        ExpectNear(fixture.Spawn.position.y, 1f, "spawn did not apply after an empty astro array");
    }

    static void HasFuelFalseDoesNotWriteFuel()
    {
        using Fixture fixture = Fixture.Create(withSentinel: false);
        fixture.Player.UpdateImpulseForce(9f);
        fixture.Player.UpdateFuelCapacity(80f);
        string payload = JsonUtility.ToJson(new OrbPresetPayload
        {
            ImpulseMode = 1,
            ImpulseForce = 12f,
            RechargeDuration = 2f,
            InertiaDampTime = 1.5f,
            StabilizerMaxThrustSpeed = 12f,
            ThrustForce = 7f,
            HasFuel = false,
            FuelCapacity = 10f,
            FuelCostPerSecond = 1f,
            FuelRechargeWhenIdle = false,
            FuelRechargeDelay = 1f
        });

        var module = ScriptableObject.CreateInstance<OrbPresetModule>();
        try
        {
            module.Apply(payload, fixture.Context);
            ExpectNear(fixture.Impulse.ImpulseForce, 12f, "orb impulse was not applied");
            Expect((int)fixture.Impulse.ImpulseMode == 1, "orb impulse mode was not applied");
            ExpectNear(fixture.Fuel.Capacity, 80f, "HasFuel false overwrote fuel capacity");
            ExpectNear(fixture.Fuel.CostPerSecond, fixture.Fuel.CostPerSecond, "fuel cost sanity");
            float costBefore = fixture.Fuel.CostPerSecond;
            Expect(Mathf.Abs(costBefore - 1f) > 0.001f, "HasFuel false wrote the fuel cost");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(module);
        }
    }

    static void NullSchemaOrThrowingCaptureDoesNotReplaceFile(List<string> files)
    {
        using Fixture fixture = Fixture.Create(withSentinel: false);
        string keepName = Prefix + "keep";
        string keepPath = Track(files, keepName);
        File.WriteAllText(keepPath, "KEEP");
        using (CreateSession(null, fixture))
        {
            PresetEvents.RaisePresetSavedEvent(keepName);
        }
        Expect(File.ReadAllText(keepPath) == "KEEP", "null schema replaced the file");

        string emptyModulesName = Prefix + "empty-modules";
        string emptyModulesPath = Track(files, emptyModulesName);
        File.WriteAllText(emptyModulesPath, "KEEP-EMPTY");
        var emptySchema = ScriptableObject.CreateInstance<PresetSchema>();
        try
        {
            SetField(emptySchema, "_modules", new PresetModuleAsset[] { null });
            using (CreateSession(emptySchema, fixture))
            {
                PresetEvents.RaisePresetSavedEvent(emptyModulesName);
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(emptySchema);
        }
        Expect(File.ReadAllText(emptyModulesPath) == "KEEP-EMPTY", "schema without modules replaced the file");

        string throwName = Prefix + "throw";
        string throwPath = Track(files, throwName);
        const string original = "{\"Version\":2,\"Sections\":[{\"Id\":\"notes\",\"Payload\":\"keep\"}]}";
        File.WriteAllText(throwPath, original);
        var throwing = ScriptableObject.CreateInstance<VerificationPresetModule>();
        var throwSchema = ScriptableObject.CreateInstance<PresetSchema>();
        try
        {
            throwing.forcedId = "astros";
            throwing.throwOnCapture = true;
            SetField(throwSchema, "_modules", new PresetModuleAsset[] { throwing });
            using (CreateSession(throwSchema, fixture))
            {
                PresetEvents.RaisePresetSavedEvent(throwName);
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(throwSchema);
            UnityEngine.Object.DestroyImmediate(throwing);
        }
        Expect(File.ReadAllText(throwPath) == original, "throwing capture replaced the file");
    }

    static void RemovingOrbModuleKeepsOrbSection(List<string> files)
    {
        using Fixture fixture = Fixture.Create(withSentinel: false);
        fixture.Limits.SetLimits(new Vector2(-2f, -3f), new Vector2(4f, 5f));
        fixture.Spawn.position = new Vector3(6f, 7f, 8f);

        string name = Prefix + "drop-orb";
        string path = Track(files, name);
        var existing = new PresetFile
        {
            Version = 2,
            Sections = new[]
            {
                new PresetSection { Id = "notes", Payload = "keep-notes" },
                new PresetSection { Id = "orb", Payload = "OLD-ORB" }
            }
        };
        File.WriteAllText(path, JsonUtility.ToJson(existing));

        var schema = ScriptableObject.CreateInstance<PresetSchema>();
        try
        {
            SetField(schema, "_modules", new PresetModuleAsset[]
            {
                LoadModule<AstroListPresetModule>("Assets/Settings/Presets/AstroListPresetModule.asset"),
                null,
                LoadModule<LimitsPresetModule>("Assets/Settings/Presets/LimitsPresetModule.asset"),
                LoadModule<PlayerSpawnPresetModule>("Assets/Settings/Presets/PlayerSpawnPresetModule.asset")
            });
            using (CreateSession(schema, fixture))
            {
                PresetEvents.RaisePresetSavedEvent(name);
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(schema);
        }

        string json = File.ReadAllText(path);
        Expect(PresetFileManager.TryParse(json, out PresetFile saved), "merged file without orb could not be read");
        Expect(saved.Version == 2, "merged file version");
        Expect(Section(saved, "orb") == "OLD-ORB", "removing the orb module deleted its section");
        Expect(Section(saved, "notes") == "keep-notes", "a null module slot deleted an unknown section");
        Expect(Section(saved, "limits") != null && Section(saved, "limits").Contains("\"min\""), "limits were not captured");
        Expect(Section(saved, "playerSpawn") != null && Section(saved, "playerSpawn").Contains("\"Position\""), "spawn was not captured");
    }

    static void NextSaveWritesVersionTwoAndKeepsUnknownSections(List<string> files)
    {
        using Fixture fixture = Fixture.Create(withSentinel: false);
        fixture.Player.UpdateImpulseMode(1);
        fixture.Player.UpdateImpulseForce(12f);
        fixture.Player.UpdateImpuseCooldown(2f);
        fixture.Player.UpdateInertiaDampTime(1.5f);
        fixture.Player.UpdateStabilizerMaxThrustSpeed(12f);
        fixture.Player.UpdateThrustForce(7f);
        fixture.Player.UpdateFuelCostPerSecond(4f);
        fixture.Player.UpdateFuelCapacity(80f);
        fixture.Player.UpdateFuelRechargeWhenIdle(false);
        fixture.Player.UpdateFuelRechargeDelay(3f);
        fixture.Limits.SetLimits(new Vector2(-8f, -9f), new Vector2(10f, 11f));
        fixture.Spawn.position = new Vector3(1f, 2f, 3f);

        string name = Prefix + "merge";
        string path = Track(files, name);
        var existing = new PresetFile
        {
            Version = 1,
            Sections = new[]
            {
                new PresetSection { Id = "notes", Payload = "keep-notes" },
                new PresetSection { Id = "orb", Payload = "OLD-ORB" }
            }
        };
        File.WriteAllText(path, JsonUtility.ToJson(existing));

        using (CreateSession(LoadSchemaAsset(), fixture))
        {
            PresetEvents.RaisePresetSavedEvent(name);
        }

        string json = File.ReadAllText(path);
        Expect(json.Contains("\"Version\": 2") || json.Contains("\"Version\":2"), "saved file is not version 2");
        Expect(PresetFileManager.TryParse(json, out PresetFile saved), "saved envelope could not be read");
        Expect(saved.Version == 2, "parsed version");
        Expect(saved.Sections.Length == 5, "expected notes plus four module sections, got " + saved.Sections.Length);
        Expect(Section(saved, "notes") == "keep-notes", "unknown section was dropped");
        Expect(Section(saved, "orb") != "OLD-ORB", "orb section was not replaced");
        Expect(Section(saved, "astros") != null && Section(saved, "astros").Contains("\"Entries\""), "astros section missing");
        Expect(Section(saved, "limits") != null, "limits section missing");
        Expect(Section(saved, "playerSpawn") != null, "playerSpawn section missing");

        Expect(PresetPayloadJson.TryFromJson(Section(saved, "orb"), out OrbPresetPayload orb), "saved orb payload did not parse");
        Expect(orb.HasFuel, "fuel was present but HasFuel was false");
        Expect(orb.ImpulseMode == 1, "saved impulse mode");
        ExpectNear(orb.ImpulseForce, 12f, "saved impulse force");
        ExpectNear(orb.RechargeDuration, 2f, "saved recharge");
        ExpectNear(orb.ThrustForce, 7f, "saved thrust");
        ExpectNear(orb.FuelCapacity, 80f, "saved fuel capacity");
        Expect(!orb.FuelRechargeWhenIdle, "saved fuel recharge flag");

        Expect(PresetPayloadJson.TryFromJson(Section(saved, "limits"), out LimitsData limits), "saved limits did not parse");
        ExpectNear(limits.min.x, -8f, "saved limits");
        Expect(PresetPayloadJson.TryFromJson(Section(saved, "playerSpawn"), out PlayerSpawnPresetPayload spawn), "saved spawn did not parse");
        ExpectNear(spawn.Position.y, 2f, "saved spawn");
        Expect(PresetPayloadJson.TryFromJson(Section(saved, "astros"), out AstroListPresetPayload astros), "saved astros did not parse");
        Expect(astros.Entries != null && astros.Entries.Length == 0, "empty astro capture was not an array");
    }

    static void ReadDoesNotWriteDisk(List<string> files)
    {
        string name = Prefix + "read";
        string path = Track(files, name);
        var legacy = new PresetData(
            new LimitsData(new Vector2(1f, 2f), new Vector2(3f, 4f)),
            Array.Empty<AstroPresetEntry>(),
            Vector3.zero);
        File.WriteAllText(path, JsonUtility.ToJson(legacy));
        string before = File.ReadAllText(path);

        PresetFile received = null;
        void OnLoaded(PresetFile file) { received = file; }
        PresetEvents.OnPresetFileLoaded += OnLoaded;
        try
        {
            PresetFileManager.Read(name);
        }
        finally
        {
            PresetEvents.OnPresetFileLoaded -= OnLoaded;
        }

        Expect(File.ReadAllText(path) == before, "Read wrote the disk");
        Expect(received != null && received.Sections != null && received.Sections.Length == 3, "Read did not deliver adapted sections");
        Expect(Section(received, "orb") == null, "Read invented an orb section");
    }

    static void SceneWiringMatchesSchema()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");
        PresetSession session = UnityEngine.Object.FindAnyObjectByType<PresetSession>();
        Expect(session != null, "Game scene has no PresetSession");
        Expect(session.gameObject.name == "Preset Session", "PresetSession object name");

        var serialized = new SerializedObject(session);
        var schema = serialized.FindProperty("_schema").objectReferenceValue as PresetSchema;
        var manager = serialized.FindProperty("_astroManager").objectReferenceValue as AstroManager;
        var limits = serialized.FindProperty("_limits").objectReferenceValue as Limits;
        var spawn = serialized.FindProperty("_playerSpawn").objectReferenceValue as Transform;
        var player = serialized.FindProperty("_playerData").objectReferenceValue as PlayerData;

        Expect(schema != null, "scene schema is missing");
        Expect(AssetDatabase.AssetPathToGUID("Assets/Settings/Presets/PresetSchema.asset") == "3550f60240f8437db4fcc0e70424b6cf", "schema guid");
        Expect(schema.Modules != null && schema.Modules.Length == 4, "schema module count");
        Expect(schema.Modules[0] is AstroListPresetModule && schema.Modules[0].Id == "astros", "module 0");
        Expect(schema.Modules[1] is LimitsPresetModule && schema.Modules[1].Id == "limits", "module 1");
        Expect(schema.Modules[2] is PlayerSpawnPresetModule && schema.Modules[2].Id == "playerSpawn", "module 2");
        Expect(schema.Modules[3] is OrbPresetModule && schema.Modules[3].Id == "orb", "module 3");
        Expect(manager != null && manager.gameObject.name == "Astro Manager", "scene AstroManager");
        Expect(limits != null && AssetDatabase.GetAssetPath(limits).EndsWith("Level Limits.asset"), "scene limits");
        Expect(spawn != null && spawn.name == "SpawnPoint", "scene spawn");
        Expect(player != null && AssetDatabase.GetAssetPath(player).EndsWith("PlayerData.asset"), "scene player data");

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    static PresetSchema LoadSchemaAsset()
    {
        var schema = AssetDatabase.LoadAssetAtPath<PresetSchema>("Assets/Settings/Presets/PresetSchema.asset");
        Expect(schema != null, "PresetSchema asset did not load");
        return schema;
    }

    static T LoadModule<T>(string path) where T : PresetModuleAsset
    {
        var module = AssetDatabase.LoadAssetAtPath<T>(path);
        Expect(module != null, "missing module asset " + path);
        return module;
    }

    static SessionScope CreateSession(PresetSchema schema, Fixture fixture)
    {
        var go = new GameObject("verification-session");
        var session = go.AddComponent<PresetSession>();
        SetField(session, "_schema", schema);
        SetField(session, "_astroManager", fixture.Manager);
        SetField(session, "_limits", fixture.Limits);
        SetField(session, "_playerSpawn", fixture.Spawn);
        SetField(session, "_playerData", fixture.Player);
        // Batch verification stays in edit mode. OnEnable only runs in play mode,
        // which is when the game scene actually saves and loads presets.
        if (!Application.isPlaying)
            InvokeLifecycle(session, "OnEnable");
        return new SessionScope(go, session);
    }

    static void InvokeLifecycle(object target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (method == null)
            throw new InvalidOperationException("Missing method " + methodName);
        method.Invoke(target, null);
    }

    static string Section(PresetFile file, string id)
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

    static int AstroCount(AstroManager manager)
    {
        var list = (List<Astro>)typeof(AstroManager).GetField("_astros", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(manager);
        return list.Count;
    }

    static string Track(List<string> files, string presetName)
    {
        string path = Path.Combine(Application.persistentDataPath, presetName + ".json");
        files.Add(path);
        return path;
    }

    static void DeleteFiles(List<string> files)
    {
        for (int i = 0; i < files.Count; i++)
        {
            if (File.Exists(files[i]))
                File.Delete(files[i]);
        }
    }

    static void SetField(object target, string name, object value)
    {
        FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null)
            throw new InvalidOperationException("Missing field " + name);
        field.SetValue(target, value);
    }

    static void Expect(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    static void ExpectNear(float actual, float expected, string message)
    {
        if (Mathf.Abs(actual - expected) > 0.001f)
            throw new InvalidOperationException(message + " expected " + expected + " but was " + actual);
    }

    sealed class Fixture : IDisposable
    {
        public AstroManager Manager;
        public Limits Limits;
        public Transform Spawn;
        public PlayerData Player;
        public ImpulseResource Impulse;
        public FuelResource Fuel;
        public PresetSessionContext Context;
        GameObject _root;
        ThrusterResource _thruster;
        InertiaResource _inertia;

        public static Fixture Create(bool withSentinel)
        {
            var fixture = new Fixture();
            fixture._root = new GameObject("verification-root");
            fixture._root.SetActive(false);
            fixture.Manager = fixture._root.AddComponent<AstroManager>();
            SetField(fixture.Manager, "_astroFactory", new AstroFactory(new AstroFactoryDependencies()));
            if (withSentinel)
            {
                var sentinelObject = new GameObject("sentinel");
                sentinelObject.SetActive(false);
                sentinelObject.transform.SetParent(fixture._root.transform);
                var sentinel = sentinelObject.AddComponent<Astro>();
                var list = (List<Astro>)typeof(AstroManager).GetField("_astros", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(fixture.Manager);
                list.Add(sentinel);
            }

            fixture.Limits = ScriptableObject.CreateInstance<Limits>();
            fixture.Spawn = new GameObject("verification-spawn").transform;
            fixture.Impulse = ScriptableObject.CreateInstance<ImpulseResource>();
            fixture._thruster = ScriptableObject.CreateInstance<ThrusterResource>();
            fixture._inertia = ScriptableObject.CreateInstance<InertiaResource>();
            fixture.Fuel = ScriptableObject.CreateInstance<FuelResource>();
            fixture.Player = ScriptableObject.CreateInstance<PlayerData>();
            SetField(fixture.Player, "_impulseResource", fixture.Impulse);
            SetField(fixture.Player, "_thrusterResource", fixture._thruster);
            SetField(fixture.Player, "_inertiaResource", fixture._inertia);
            SetField(fixture.Player, "_fuelResource", fixture.Fuel);
            fixture.Context = new PresetSessionContext(fixture.Manager, fixture.Limits, fixture.Spawn, fixture.Player);
            return fixture;
        }

        public void Dispose()
        {
            if (_root != null)
                UnityEngine.Object.DestroyImmediate(_root);
            if (Spawn != null)
                UnityEngine.Object.DestroyImmediate(Spawn.gameObject);
            Destroy(Limits);
            Destroy(Impulse);
            Destroy(_thruster);
            Destroy(_inertia);
            Destroy(Fuel);
            Destroy(Player);
        }

        static void Destroy(UnityEngine.Object obj)
        {
            if (obj != null)
                UnityEngine.Object.DestroyImmediate(obj);
        }
    }

    sealed class SessionScope : IDisposable
    {
        readonly GameObject _gameObject;
        readonly PresetSession _session;
        public SessionScope(GameObject gameObject, PresetSession session)
        {
            _gameObject = gameObject;
            _session = session;
        }
        public void Dispose()
        {
            if (!Application.isPlaying && _session != null)
                InvokeLifecycle(_session, "OnDisable");
            if (_gameObject != null)
                UnityEngine.Object.DestroyImmediate(_gameObject);
        }
    }
}

public class VerificationPresetModule : PresetModuleAsset
{
    public string forcedId;
    public bool throwOnCapture;
    public override string Id => forcedId;
    public override string Capture(PresetSessionContext context)
    {
        if (throwOnCapture)
            throw new InvalidOperationException("capture failed");
        return null;
    }
    public override void Apply(string payload, PresetSessionContext context) { }
}
