using NodeTree;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// Connects the shared dialogue UI and reward confirmation to player input and modes.
public class DialogueSession : MonoBehaviour
{
    public DialogRunner runner;
    public TMP_Text continueHint;
    public QuestReward questReward;
    private PlayerController player;
    private PlayerModeController mode;
    private InputAction continueInput;
    private InputAction backInput;
    private InputAction sourceInput;
    private InputActionAsset dialogueControls;
    private InputActionReference continueReference;
    private InputActionReference backReference;
    private InputActionReference previousContinueReference;
    private InputActionReference previousBackReference;
    private bool locked;
    private PlayerMode previousMode;

    private void Start() => BindPlayer();

    private void BindPlayer()
    {
        player = FindAnyObjectByType<PlayerController>();
        if (player == null)
            return;

        mode = PlayerModeController.ForPlayer(player);

        sourceInput = player.GetNewControls().BasicActionMap.Interact;
        InputActionMap map = sourceInput.actionMap.Clone();
        dialogueControls = ScriptableObject.CreateInstance<InputActionAsset>();
        dialogueControls.AddActionMap(map);
        continueInput = map.FindAction(sourceInput.name, true);
        continueInput.AddBinding("<Mouse>/leftButton");
        continueInput.AddBinding("<Keyboard>/space");
        continueInput.AddBinding("<Keyboard>/rightArrow");

        backInput = map.AddAction("DialogueBack", InputActionType.Button);
        backInput.AddBinding("<Keyboard>/x");
        backInput.AddBinding("<Mouse>/rightButton");
        backInput.AddBinding("<Keyboard>/leftArrow");

        continueReference = InputActionReference.Create(continueInput);
        backReference = InputActionReference.Create(backInput);
        previousContinueReference = runner.continueAction;
        previousBackReference = runner.backAction;
        runner.continueAction = continueReference;
        runner.backAction = backReference;
        continueInput.Disable();
        backInput.Disable();
        UpdateContinueHint();
    }

    private void LateUpdate()
    {
        if (runner == null)
            return;
        if (player == null)
        {
            BindPlayer();
            if (player == null)
                return;
        }

        if (locked && mode != null && mode.Mode != PlayerMode.Dialogue)
        {
            runner.StopDialog();
            questReward?.Dismiss();
        }

        bool dialogIsActive = runner.IsDialogActive;
        bool rewardModalIsOpen = questReward != null && questReward.IsOpen;
        bool gameplayMustStayLocked = dialogIsActive || rewardModalIsOpen;

        if (!gameplayMustStayLocked && !locked)
        {
            continueInput?.Disable();
            backInput?.Disable();
        }

        if (gameplayMustStayLocked && !locked)
        {
            // Settings may have changed since this scene started.
            continueInput.Disable();
            backInput.Disable();
            for (int i = 0; i < sourceInput.bindings.Count; i++)
                continueInput.ApplyBindingOverride(i, sourceInput.bindings[i].effectivePath);
            UpdateContinueHint();
            previousMode = mode != null ? mode.Mode : PlayerMode.Gameplay;
            mode?.SetMode(PlayerMode.Dialogue);
            SaveManager.Instance?.BlockSaving(this, "Finish the conversation or reward confirmation before saving.");
            locked = true;
        }

        if (rewardModalIsOpen)
        {
            continueInput?.Disable();
            backInput?.Disable();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (dialogIsActive)
        {
            continueInput?.Enable();
            backInput?.Enable();
        }

        if (dialogIsActive && !rewardModalIsOpen &&
            (Keyboard.current?.escapeKey.wasPressedThisFrame == true ||
             Gamepad.current?.buttonEast.wasPressedThisFrame == true))
            runner.StopDialog();

        if (locked && !dialogIsActive && !rewardModalIsOpen)
            ReleasePlayer();
    }

    private void ReleasePlayer()
    {
        continueInput?.Disable();
        backInput?.Disable();
        if (mode != null && mode.Mode == PlayerMode.Dialogue)
            mode.SetMode(previousMode);
        SaveManager.Instance?.AllowSaving(this);
        locked = false;
    }

    private void UpdateContinueHint()
    {
        if (continueHint == null || sourceInput == null)
            return;

        string interactBinding = sourceInput.GetBindingDisplayString(group: "Keyboard & Mouse");
        if (string.IsNullOrEmpty(interactBinding))
            interactBinding = "E";

        continueHint.text = interactBinding +
            " / LMB / Space / Right: Continue   |   " +
            "X / RMB / Left: Back   |   Esc / Gamepad Cancel: Close";
    }

    private void OnDestroy()
    {
        if (runner != null && runner.IsDialogActive)
            runner.StopDialog();
        if (locked)
            ReleasePlayer();
        if (runner != null)
        {
            if (runner.continueAction == continueReference)
                runner.continueAction = previousContinueReference;
            if (runner.backAction == backReference)
                runner.backAction = previousBackReference;
        }
        dialogueControls?.Disable();
        if (continueReference != null)
            Destroy(continueReference);
        if (backReference != null)
            Destroy(backReference);
        if (dialogueControls != null)
            Destroy(dialogueControls);
    }
}
