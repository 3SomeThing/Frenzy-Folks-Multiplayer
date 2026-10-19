using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TestButton : MonoBehaviour
{
    private void Awake()
    {
        Debug.Log("TestButton.Awake");
    }

    public void OnButtonClick()
    {
        Debug.Log("TestButton.OnButtonClick");

        NetworkObject playerObject = NetworkManager.Singleton.LocalClient?.PlayerObject;
        CursorToggler cursorToggler = playerObject != null
            ? playerObject.GetComponentInChildren<CursorToggler>(true)
            : null;
        cursorToggler?.SetCursor(false);

        NetworkManager.Singleton.SceneManager.LoadScene("TestingPlayground", LoadSceneMode.Single);
        // NetworkManager.Singleton.SceneManager.LoadScene("Level", LoadSceneMode.Single);
    }
}
