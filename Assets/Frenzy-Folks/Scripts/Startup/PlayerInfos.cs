using System;
using System.IO;
using UnityEngine;

namespace Frenzy_Folks.Scripts.Startup
{
    [Serializable]
    public class PlayerInfos
    {
        public static PlayerInfos Singleton = new PlayerInfos();
        
        // Player informations
        public string LastPlayerName = "";
        
        // Network specifics
        public string LastIP = "";
        public string LastPort = "";

        private static string SavePath =>
            Path.Combine(Application.persistentDataPath, "player-infos.json");
        
        private PlayerInfos() {}

        public void Save()
        {
            File.WriteAllText(SavePath, JsonUtility.ToJson(this, true));
        }

        public PlayerInfos Load()
        {
            if(File.Exists(SavePath)) JsonUtility.FromJsonOverwrite(File.ReadAllText(SavePath), this);
            return this;
        }
    }
}