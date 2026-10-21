using System;
using System.Collections.Generic;
using System.IO;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

[Serializable]
public class FrenzyLevelConfiguration
{
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
        
        if(!Directory.Exists(LevelsPath))
        {
            Directory.CreateDirectory(LevelsPath);
        }
    }
    
    // private Dictionary<string, BaseConfigurationSerializable> configurations;

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

    private void AddToLevel(ConfigurationEntry entry, GameObject prefab)
    {
        GameObject instance = Object.Instantiate(prefab, _parentLevel.transform, true);
        entry.value.Apply(instance);

        NetworkObject networkObject = instance.GetComponent<NetworkObject>();
        if (networkObject == null)
        {
            Object.Destroy(instance);
            throw new InvalidOperationException(
                $"Prefab '{prefab.name}' for '{entry.key}' must contain a NetworkObject.");
        }

        networkObject.Spawn(true);
        networkObject.TrySetParent(_parentLevel.transform);
        _parentLevel.SynchronizeElementConfiguration(
            networkObject,
            entry.key,
            JsonUtility.ToJson(entry.value));
    }

    public void Load(string json)
    {
        JsonUtility.FromJsonOverwrite(json, this);

        foreach (ConfigurationEntry configurationEntry in configurationEntries)
        {
            if (!_parentLevel.levelPrefabs.TryGetValue(configurationEntry.key, out GameObject prefab))
            {
                Debug.LogError($"No prefab configured for level element '{configurationEntry.key}'.");
                continue;
            }

            AddToLevel(configurationEntry, prefab);
        }

        _parentLevel.SetLoadedLevelScale(20f);
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