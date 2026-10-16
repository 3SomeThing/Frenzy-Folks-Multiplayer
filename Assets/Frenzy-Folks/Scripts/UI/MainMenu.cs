using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private GameObject mainMenu;

    private readonly List<GameObject> _menus = new List<GameObject>();

    private void Awake()
    {
        _menus.Add(mainMenu);
    }

    public void OnQuitClick()
    {
        // if (EditorApplication.isPlaying)
        // {
        //     EditorApplication.isPlaying = false;
        // }
        // else
        // {
        //     Application.Quit();
        // }
        Application.Quit();
    }

    public void OpenMenu(GameObject menu)
    {
        if (menu == null) return;
        if (_menus.Count > 0 && _menus[^1] == menu) return;
        _menus[^1].SetActive(false);
        menu.SetActive(true);
        _menus.Add(menu);
    }
    
    public void OnBackClick()
    {
        if (_menus.Count <= 1) return;
        _menus[^1].SetActive(false);
        _menus.RemoveAt(_menus.Count - 1);
        _menus[^1].SetActive(true);
    }

    public void SavePlayerInfos()
    {
        // UserSettings.Singleton.LastPlayerName =
    }
}
