using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class FrenzyLevel : MonoBehaviour
{
    private string levelName;

    private int maxTime;

    [SerializeField] private Dictionary<string, GameObject> levelPrefabs;

    private Dictionary<string, BaseConfigurationSerializable> configurations = new Dictionary<string, BaseConfigurationSerializable>
    {
        { "movable_platform", new MovablePlatformConfiguration() },
    };

    private void Awake()
    {
        string name = "movable_platform";
        GameObject go = levelPrefabs[name];
        MovablePlatformConfiguration configuration = (MovablePlatformConfiguration)configurations[name];
        configuration.Apply(go);    

        Debug.Log(JsonUtility.ToJson(configuration));
    }
}