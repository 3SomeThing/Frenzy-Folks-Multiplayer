using System.Collections.Generic;
using UnityEngine;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject hostMenu;

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
    
    public void OnHostClick()
    {
        foreach (GameObject menu in _menus) menu.SetActive(false);
        hostMenu.SetActive(true);
        _menus.Add(hostMenu);
    }
    
    public void OnJoinClick()
    {
        // Code to handle joining a game
        Debug.Log("Join button clicked");
    }
    
    public void OnSettingsClick()
    {
        // Code to handle opening settings
        Debug.Log("Settings button clicked");
    }

    public void OnQuitClick()
    {
        Application.Quit();
    }
    
    public void OnBackClick()
    {
        if (_menus.Count <= 1) return;
        _menus[^1].SetActive(false);
        _menus.RemoveAt(_menus.Count - 1);
        _menus[^1].SetActive(true);
    }
}
