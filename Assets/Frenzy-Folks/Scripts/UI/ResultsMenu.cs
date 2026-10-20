using Frenzy_Folks.Scripts.Core;
using TMPro;
using Unity.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class ResultsMenu : MonoBehaviour
{
    [SerializeField] private Canvas resultsCanvas;
    [SerializeField] private Transform playersLayoutParent;
    [SerializeField] private GameObject textTemplate;

    private int playerArrivalNumber = 1;
    private bool isCanvasShown;

    [SerializeField] private InputAction toggleCanvas = new InputAction(
        "Toggle Results canvas",
        InputActionType.Button,
        "<Keyboard>/tab");
    
    private void Awake()
    {
        toggleCanvas.Enable();
        
        if (resultsCanvas == null)
        {
            resultsCanvas = GetComponent<Canvas>();
        }

        if (GameState.Instance == null)
        {
            Debug.LogWarning("GameState is not available in the Results scene.", this);
            return;
        }

        if (!textTemplate)
        {
            Debug.Log($"No text template existing.");
            return;
        }

        foreach (FixedString128Bytes playerName in GameState.Instance.PlayerArrivalOrder)
        {
            string text = $"{playerArrivalNumber}. {playerName.Value}";
            Instantiate(textTemplate, playersLayoutParent).GetComponent<TextMeshProUGUI>().text = text;

            playerArrivalNumber++;
            Debug.Log($"{playerArrivalNumber}. {playerName.Value}");
        }

        foreach (FixedString128Bytes playerName in GameState.Instance.UnfinishedPlayerNames)
        {
            string text = $"N/A: {playerName.Value}";
            Instantiate(textTemplate, playersLayoutParent).GetComponent<TextMeshProUGUI>().text = text;

            Debug.Log($"N/A. {playerName.Value}");
        }
        
        OnCanvasShown(true);
    }

    public void OnDestroy()
    {
        OnCanvasShown(false);
        GameState.Instance.ClearPlayerArrivalOrder();    
    }

    public void OnCanvasShown(bool shown)
    {
        isCanvasShown = shown;

        if (resultsCanvas)
        {
            resultsCanvas.enabled = shown;
        }
    }

    public void Update()
    {
        if (toggleCanvas.WasPressedThisFrame())
        {
            OnCanvasShown(!isCanvasShown);
        }
    }
}