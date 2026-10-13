using UnityEngine;
using UnityEngine.SceneManagement;

namespace Frenzy_Folks.Scripts.Startup
{
    public class MainMenuLoader : MonoBehaviour
    {
        private void Awake()
        {
            SceneManager.LoadScene("MainMenu");
            PlayerInfos.Singleton.Load();
        }
    }
}