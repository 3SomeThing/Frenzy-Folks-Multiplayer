using System;
using System.IO;
using UnityEngine;

namespace Frenzy_Folks.Scripts.Startup
{
    [Serializable]
    public class UserSettings
    {
        public static UserSettings Singleton = new UserSettings();
        
        // Player informations
        public string LastPlayerName = "";
        
        // Network specifics
        public string LastIP = "";
        public ushort LastPort = 0;

        private static string SavePath =>
            Path.Combine(Application.persistentDataPath, "user-settings.json");

        private UserSettings()
        {
            Load();
        }

        public void Save()
        {
            File.WriteAllText(SavePath, JsonUtility.ToJson(this, true));
        }

        public UserSettings Load()
        {
            if(File.Exists(SavePath)) JsonUtility.FromJsonOverwrite(File.ReadAllText(SavePath), this);
            return this;
        }
    }
}