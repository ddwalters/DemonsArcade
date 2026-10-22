using System;
using UnityEngine;

public enum PlayerMode
{
    Gameplay,
    Inventory,
    Looting,
    Paused,
    Dead
}

[DefaultExecutionOrder(-100)]
public class PlayerModeController : MonoBehaviour
{
    public PlayerMode Mode { get; private set; } = PlayerMode.Gameplay;
    public int LootGridId { get; private set; } = -1;
    public int ChangedFrame { get; private set; } = -1;
    public bool AllowsGameplay => Mode == PlayerMode.Gameplay;
    public event Action<PlayerMode> ModeChanged;
    private float previousTimeScale = 1f;
    private bool cursorLockPending;

    public static PlayerModeController ForPlayer(PlayerController player)
    {
        if (player == null)
            return null;

        return player.GetComponent<PlayerModeController>() ?? player.gameObject.AddComponent<PlayerModeController>();
    }

    private void Update()
    {
        if (cursorLockPending && Time.frameCount > ChangedFrame)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            cursorLockPending = false;
        }

        NewControls controls = GetComponent<PlayerController>()?.GetNewControls();
        bool interactPressed = controls != null && controls.BasicActionMap.Interact.WasPressedThisFrame();
        HandleMenuInput(MenuInput.PausePressed, MenuInput.InventoryPressed, interactPressed);
    }

    private void HandleMenuInput(bool escape, bool tab, bool interact)
    {
        if (Mode == PlayerMode.Dead || SaveManager.Instance?.IsLoading == true)
            return;

        if (escape && UIController.Current != null && UIController.Current.HandleEscape())
            return;

        if (escape)
            SetMode(Mode == PlayerMode.Gameplay ? PlayerMode.Paused : PlayerMode.Gameplay);
        else if (tab && Mode != PlayerMode.Paused)
            SetMode(Mode == PlayerMode.Gameplay ? PlayerMode.Inventory : PlayerMode.Gameplay);
        else if (Mode == PlayerMode.Looting && interact)
            SetMode(PlayerMode.Gameplay);
    }

    public void OpenLoot(int gridId)
    {
        if (!AllowsGameplay)
            return;

        LootGridId = gridId;
        SetMode(PlayerMode.Looting);
    }

    public void SetMode(PlayerMode next)
    {
        if (Mode == next || (Mode == PlayerMode.Dead && next != PlayerMode.Dead))
            return;

        ApplyMode(next);
    }

    private void ApplyMode(PlayerMode next)
    {
        if (Mode == PlayerMode.Paused)
            Time.timeScale = previousTimeScale;

        if (next == PlayerMode.Paused)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        Mode = next;
        ChangedFrame = Time.frameCount;

        if (next != PlayerMode.Looting)
            LootGridId = -1;

        cursorLockPending = next == PlayerMode.Gameplay;
        if (!cursorLockPending)
        {
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
        }

        if (next != PlayerMode.Gameplay)
            GetComponent<PlayerController>()?.StopMotion();

        ModeChanged?.Invoke(next);
    }

    public void ResetToGameplay() => ApplyMode(PlayerMode.Gameplay);

    private void OnDestroy()
    {
        if (Mode == PlayerMode.Paused)
            Time.timeScale = previousTimeScale;
    }
}
