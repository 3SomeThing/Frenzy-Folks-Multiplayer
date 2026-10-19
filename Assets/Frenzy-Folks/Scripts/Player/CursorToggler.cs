using UnityEngine;
using UnityEngine.InputSystem;
using SUPERCharacter;

public class CursorToggler : MonoBehaviour
{
    [SerializeField] private InputAction toggleCursor = new InputAction(
        "Toggle Cursor",
        InputActionType.Button,
        "<Keyboard>/c");

    private SUPERCharacterAIO characterController;
    private bool cursorUnlocked;

    private void Awake()
    {
        characterController = GetComponentInParent<SUPERCharacterAIO>();
        toggleCursor.Enable();
    }

    private void Update()
    {
        if (toggleCursor.WasPressedThisFrame())
        {
            SetCursor(!cursorUnlocked);
        }
    }

    public void SetCursor(bool unlocked)
    {
        cursorUnlocked = unlocked;

        if (characterController)
        {
            characterController.lockAndHideMouse = !unlocked;
            characterController.ResetInputState();
        }

        Cursor.visible = unlocked;
        Cursor.lockState = unlocked
            ? CursorLockMode.None
            : CursorLockMode.Locked;
    }
}
