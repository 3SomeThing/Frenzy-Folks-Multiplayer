using Frenzy_Folks.Scripts.Startup;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuLoader : MonoBehaviour
{
    private void Awake()
    {
        LoadMainMenu();
        UserSettings.Singleton.Load();
    }

    public static void LoadMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}