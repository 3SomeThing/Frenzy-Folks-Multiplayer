using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

[Serializable]
public class FrenzyLevelConfiguration
{
    private readonly FrenzyLevel _parent;

    [SerializeField]
    private List<ConfigurationEntry> configurationEntries = new()
    {
        new ConfigurationEntry
        {
            key = "movable_platform",
            value = new MovablePlatformConfiguration()
        },
        new ConfigurationEntry
        {
            key = "checkpoint",
            value = new CheckpointConfiguration()
        },
        new ConfigurationEntry
        {
            key = "fan_trap",
            value = new FanTrapConfiguration()
        },
        new ConfigurationEntry
        {
            key = "trampoline_trap",
            value = new TrampolineTrapConfiguration()
        },        
        new ConfigurationEntry
        {
            key = "bumper_trap",
            value = new BumperTrapConfiguration()
        }
    };

    public FrenzyLevelConfiguration(FrenzyLevel parent)
    {
        _parent = parent;
    }
    
    // private Dictionary<string, BaseConfigurationSerializable> configurations;

    public FrenzyLevelConfiguration Save()
    {
        // configurations = new Dictionary<string, BaseConfigurationSerializable>();
        // foreach (ConfigurationEntry entry in configurationEntries)
        //     configurations.Add(entry.key, entry.value);

        Debug.Log(JsonUtility.ToJson(this, true));

        return this;
    }

    public void AddToLevel<T>(ConfigurationEntry entry, GameObject prefab)
    where T : BaseConfigurationSerializable
    {
        GameObject instance = Object.Instantiate(prefab, _parent.transform, true);
        ((T)entry.value).Apply(instance);

        NetworkObject networkObject = instance.GetComponent<NetworkObject>();
        networkObject.Spawn(true);
        networkObject.TrySetParent(_parent.transform);
    }
    
    public static FrenzyLevelConfiguration Load(FrenzyLevel level, string json, Dictionary<string, GameObject> prefabs)
    {
        Debug.Log("Loading level elements...");
        FrenzyLevelConfiguration frenzyLevelConfiguration = new FrenzyLevelConfiguration(level);
        JsonUtility.FromJsonOverwrite(json, frenzyLevelConfiguration);
        
        foreach (ConfigurationEntry configurationEntry in frenzyLevelConfiguration.configurationEntries)
        {
            switch (configurationEntry.key)
            {
                case "movable_platform": 
                    frenzyLevelConfiguration.AddToLevel<MovablePlatformConfiguration>(configurationEntry, prefabs[configurationEntry.key]);
                    break;
            }
        }

        // Debug.Log(frenzyLevelConfiguration.configurationEntries[0].value.transform);

        return frenzyLevelConfiguration;
    }
}