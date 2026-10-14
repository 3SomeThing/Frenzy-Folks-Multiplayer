using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private GameObject mainMenu;

    private readonly List<GameObject> _menus = new List<GameObject>();
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _menus.Add(mainMenu);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnQuitClick()
    {
        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
        }
        else
        {
            Application.Quit();
        }
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
}
