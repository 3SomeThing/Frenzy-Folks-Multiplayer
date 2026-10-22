using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

[Serializable]
public class FrenzyLevelConfiguration
{
    // The JSON levels are authored at 1/20 of their final size.
    private const float LoadedLevelScale = 20f;

    // Delay between two spawns, to avoid flooding the network with spawn messages in one frame.
    private const float SpawnIntervalSeconds = 0.05f;

    // Checkpoints are compared with ">" so their indices MUST be unique and follow the course order.
    // When true, indices are assigned from the order of the entries in the JSON file
    // (start platform = 0, checkpoints = 1..N, end platform = N + 1) and the indices stored in the file are ignored.
    private const bool RenumberCheckpointsByFileOrder = true;

    private const string StartPlatformKey = "start_platform";
    private const string CheckpointKey = "checkpoint";
    private const string EndPlatformKey = "end_platform";

    private readonly FrenzyLevel _parentLevel;

    private static readonly Dictionary<string, Func<BaseConfigurationSerializable>> ConfigurationDefinitions = new()
    {
        {
            "movable_platform",
            () => new MovablePlatformConfiguration()
        },
        {
            "checkpoint",
            () => new CheckpointConfiguration()
        },
        {
            "fan_trap",
            () => new FanTrapConfiguration()
        },
        {
            "trampoline_trap",
            () => new TrampolineTrapConfiguration()
        },
        {
            "bumper_trap",
            () => new BumperTrapConfiguration()
        },
        {
            "platform",
            () => new PlatformConfiguration()
        },
        {
            "turret_trap",
            () => new TurretTrapConfiguration()
        },
        {
            "start_platform",
            () => new StartPlatformConfiguration()
        },
        {
            "end_platform",
            () => new EndPlatformConfiguration()
        },
        {
            "big_pink_building",
            () => new BuildingConfiguration()
        },
        {
            "medium_pink_building",
            () => new BuildingConfiguration()
        },
        {
            "medium_white_building",
            () => new BuildingConfiguration()
        },
        {
            "small_blue_building",
            () => new BuildingConfiguration()
        },
        {
            "small_white_building",
            () => new BuildingConfiguration()
        }
    };

    public static readonly string LevelsPath = Path.Combine(Application.streamingAssetsPath, "Levels");

    [SerializeField]
    private List<ConfigurationEntry> configurationEntries = new();

    public FrenzyLevelConfiguration(FrenzyLevel parentLevel)
    {
        _parentLevel = parentLevel;

        if (!Directory.Exists(LevelsPath))
        {
            Directory.CreateDirectory(LevelsPath);
        }
    }

    public FrenzyLevelConfiguration SaveLevelToFile(string levelName = "test-level.json")
    {
        configurationEntries.Clear();

        Debug.Log("Saving level elements...");
        Debug.Log("Prefabs count: " + _parentLevel.levelPrefabs.Count);

        List<GameObject> serializedElements = new List<GameObject>();

        foreach (SerializedLevelElement levelElement in _parentLevel.transform.GetComponentsInChildren<SerializedLevelElement>())
        {
            if (!levelElement.gameObject || serializedElements.Contains(levelElement.gameObject)) continue;

            serializedElements.Add(levelElement.gameObject);

            Debug.Log(levelElement.name);

            // Get the key for the current prefab
            bool exists = ConfigurationDefinitions.TryGetValue(levelElement.elementName, out Func<BaseConfigurationSerializable> value);

            Debug.Log("Processing prefab with key: " + levelElement.elementName + ", exists: " + exists);
            if (!exists)
            {
                Debug.LogWarning($"No configuration entry found for key: {levelElement.elementName}");
                continue;
            }

            BaseConfigurationSerializable configuration = value.Invoke();
            configuration.From(levelElement.gameObject);
            configurationEntries.Add(new ConfigurationEntry
            {
                key = levelElement.elementName,
                value = configuration
            });
        }

        Debug.Log("Configuration entries count: " + configurationEntries.Count);
        Debug.Log("Serialized elements count: " + serializedElements.Count);

        string path = Path.Combine(LevelsPath, levelName);
        File.WriteAllText(path, JsonUtility.ToJson(this, true));

        return this;
    }

    private void AddToLevel(ConfigurationEntry entry, GameObject prefab, int checkpointIndex)
    {
        GameObject instance = Object.Instantiate(prefab, _parentLevel.transform, true);
        entry.value.Apply(instance);
        ApplyCheckpointIndex(instance, checkpointIndex);
        NotifyElementConfigured(instance);

        NetworkObject networkObject = instance.GetComponent<NetworkObject>();
        if (networkObject == null)
        {
            Object.Destroy(instance);
            throw new InvalidOperationException(
                $"Prefab '{prefab.name}' for '{entry.key}' must contain a NetworkObject.");
        }

        networkObject.Spawn(true);
        networkObject.TrySetParent(_parentLevel.transform);
        _parentLevel.RegisterSpawnedElement(
            networkObject,
            entry.key,
            JsonUtility.ToJson(entry.value),
            checkpointIndex);
    }

    public void Load(string json)
    {
        JsonUtility.FromJsonOverwrite(json, this);

        // Resets the previous level and puts the level back at its authored scale.
        _parentLevel.BeginLevelLoad();
        _parentLevel.StartCoroutine(SpawnAllElements());
    }

    // Final checkpoint index for every entry (-1 = not a checkpoint / keep whatever the file says).
    private int[] BuildCheckpointIndices()
    {
        int[] indices = new int[configurationEntries.Count];
        for (int i = 0; i < indices.Length; i++)
        {
            indices[i] = -1;
        }

        if (!RenumberCheckpointsByFileOrder)
        {
            return indices;
        }

        int next = 1;
        for (int i = 0; i < indices.Length; i++)
        {
            if (configurationEntries[i].key == CheckpointKey)
            {
                indices[i] = next++;
            }
        }

        for (int i = 0; i < indices.Length; i++)
        {
            string key = configurationEntries[i].key;
            if (key == StartPlatformKey)
            {
                indices[i] = 0;
            }
            else if (key == EndPlatformKey)
            {
                indices[i] = next;
            }
        }

        Debug.Log(
            $"Checkpoint indices assigned from file order: start = 0, checkpoints = 1..{next - 1}, end = {next}.");
        return indices;
    }

    public static void ApplyCheckpointIndex(GameObject target, int checkpointIndex)
    {
        if (checkpointIndex < 0)
        {
            return;
        }

        Checkpoint checkpoint = target.GetComponentInChildren<Checkpoint>(true);
        if (checkpoint == null)
        {
            return;
        }

        checkpoint.checkpointIndex = checkpointIndex;
        checkpoint.RefreshRegistration();
    }

    private IEnumerator SpawnAllElements()
    {
        int[] checkpointIndices = BuildCheckpointIndices();

        for (int i = 0; i < configurationEntries.Count; i++)
        {
            ConfigurationEntry entry = configurationEntries[i];

            if (!_parentLevel.levelPrefabs.TryGetValue(entry.key, out GameObject prefab))
            {
                Debug.LogError($"No prefab configured for level element '{entry.key}'.");
                continue;
            }

            try
            {
                AddToLevel(entry, prefab, checkpointIndices[i]);
            }
            catch (Exception exception)
            {
                // One broken element must not prevent the rest of the level (and the final scale) from loading.
                Debug.LogException(exception, _parentLevel);
            }

            yield return new WaitForSeconds(SpawnIntervalSeconds);
        }

        // Scale only once everything is spawned. The level waits for every client to confirm
        // each element's configuration before the scale is actually applied.
        _parentLevel.EndLevelLoad(LoadedLevelScale);
    }

    // Some elements keep static registries keyed by values that only exist once the configuration
    // has been applied (e.g. the checkpoint index). Tell them to re-register, on the server and on clients.
    private static void NotifyElementConfigured(GameObject target)
    {
        Checkpoint checkpoint = target.GetComponentInChildren<Checkpoint>(true);
        if (checkpoint != null)
        {
            checkpoint.RefreshRegistration();
        }
    }

    public static void ApplyToElement(
        GameObject target,
        string elementKey,
        string configurationJson)
    {
        if (!ConfigurationDefinitions.TryGetValue(
                elementKey,
                out Func<BaseConfigurationSerializable> createConfiguration))
        {
            Debug.LogWarning($"No configuration entry found for key: {elementKey}");
            return;
        }

        BaseConfigurationSerializable configuration = createConfiguration.Invoke();
        JsonUtility.FromJsonOverwrite(configurationJson, configuration);

        configuration.Apply(target);
        NotifyElementConfigured(target);
    }

    public static bool TryGetConfigurationJson(
        GameObject target,
        string elementKey,
        out string configurationJson)
    {
        configurationJson = null;
        if (!ConfigurationDefinitions.TryGetValue(
                elementKey,
                out Func<BaseConfigurationSerializable> createConfiguration))
        {
            return false;
        }

        BaseConfigurationSerializable configuration = createConfiguration.Invoke();
        configuration.From(target);
        configurationJson = JsonUtility.ToJson(configuration);
        return true;
    }

    public static FrenzyLevelConfiguration Load(FrenzyLevel level, string levelName)
    {
        string path = Path.Combine(LevelsPath, levelName);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Level configuration file '{path}' was not found.", path);
        }

        FrenzyLevelConfiguration frenzyLevelConfiguration = new FrenzyLevelConfiguration(level);
        frenzyLevelConfiguration.Load(File.ReadAllText(path));
        return frenzyLevelConfiguration;
    }
}