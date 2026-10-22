using UnityEngine;
using UnityEngine.InputSystem;

public static class MenuInput
{
    private static NewControls controls;

    private static NewControls Controls
    {
        get
        {
            if (controls == null)
            {
                controls = new NewControls();
                controls.MenuActionMap.Enable();
            }

            return controls;
        }
    }

    public static bool PausePressed => Controls.MenuActionMap.Pause.WasPressedThisFrame();
    public static bool InventoryPressed => Controls.MenuActionMap.Inventory.WasPressedThisFrame();
    public static bool SelectPressed => Controls.MenuActionMap.Select.WasPressedThisFrame();
    public static bool RotateItemPressed => Controls.MenuActionMap.RotateItem.WasPressedThisFrame();
    public static Vector2 PointerPosition => Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Initialize()
    {
        Reset();
        Application.quitting -= Reset;
        Application.quitting += Reset;
    }

    private static void Reset()
    {
        controls?.Dispose();
        controls = null;
    }
}
