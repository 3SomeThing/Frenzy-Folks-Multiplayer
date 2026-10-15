using System;
using System.Collections.Generic;
using System.IO;
using Unity.Netcode;
using UnityEngine;

[Serializable]
public class FrenzyLevel : NetworkBehaviour
{
    private string _levelName;

    private int _maxTime;

    [SerializeField] private Dictionary<string, GameObject> levelPrefabs;

    private void Awake()
    {
        // string name = "movable_platform";
        // GameObject go = levelPrefabs[name];
        // // MovablePlatformConfiguration configuration = (MovablePlatformConfiguration)configurations[name];
        // configuration.Apply(go);    
        //
        // Debug.Log(JsonUtility.ToJson(configuration));
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        
        string path = Path.Combine(Application.streamingAssetsPath, "test-level.json");
        // File.Create(path).Close();
        // File.WriteAllText(path, JsonUtility.ToJson(new FrenzyLevelConfiguration(this).Save(), true));
        // const string json = "{\"configurationEntries\":[{\"key\":\"movable_platform\",\"value\":{\"rid\":1000}}],\"references\":{\"version\":2,\"RefIds\":[{\"rid\":1000,\"type\":{\"class\":\"MovablePlatformConfiguration\",\"ns\":\"\",\"asm\":\"Assembly-CSharp\"},\"data\":{\"name\":\"\",\"transform\":{\"position\":{\"x\":10,\"y\":0,\"z\":0},\"rotation\":{\"x\":0,\"y\":0,\"z\":0,\"w\":0},\"scale\":{\"x\":0,\"y\":0,\"z\":0}},\"pathPoints\":[{\"x\":10,\"y\":10,\"z\":10},{\"x\":2,\"y\":2,\"z\":2},{\"x\":3,\"y\":3,\"z\":3}]}}]}}";
        string json = File.ReadAllText(path);
        FrenzyLevelConfiguration.Load(this, json, levelPrefabs);
    }
}