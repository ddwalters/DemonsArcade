using NodeTree;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Keeps prototype conversations separate from the game's inventory/pause modes.
public class PrototypeDialogueSession : MonoBehaviour
{
    public DialogRunner runner;
    public TMP_Text continueHint;
    private PlayerController player;
    private InteractionController interaction;
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
    private bool playerWasEnabled;
    private bool interactionWasEnabled;
    private bool modeWasEnabled;
    private Scene ownerScene;

    private void Start()
    {
        ownerScene = SceneManager.GetActiveScene();
        SceneManager.activeSceneChanged += SceneChanged;
        player = FindAnyObjectByType<PlayerController>();
        if (player == null)
            return;

        interaction = player.GetComponent<InteractionController>();
        mode = player.GetComponent<PlayerModeController>();
        mode?.ResetToGameplay();
        // Clone the actual action, including any binding overrides. Disabling player
        // controls during conversation must not disable the continue action as well.
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
        if (runner == null || player == null)
            return;

        if (!runner.IsDialogActive && !locked)
        {
            continueInput?.Disable();
            backInput?.Disable();
        }

        if (runner.IsDialogActive && !locked)
        {
            // Settings may have changed since this scene started.
            continueInput.Disable();
            backInput.Disable();
            for (int i = 0; i < sourceInput.bindings.Count; i++)
                continueInput.ApplyBindingOverride(i, sourceInput.bindings[i].effectivePath);
            UpdateContinueHint();
            playerWasEnabled = player.enabled;
            interactionWasEnabled = interaction != null && interaction.enabled;
            modeWasEnabled = mode != null && mode.enabled;
            player.StopMotion();
            player.enabled = false;
            if (interaction != null)
                interaction.enabled = false;
            if (mode != null)
                mode.enabled = false;
            locked = true;
            continueInput.Enable();
            backInput.Enable();
        }

        if (runner.IsDialogActive &&
            (Keyboard.current?.escapeKey.wasPressedThisFrame == true ||
             Gamepad.current?.buttonEast.wasPressedThisFrame == true))
            runner.StopDialog();

        if (locked && !runner.IsDialogActive)
            ReleasePlayer();
    }

    private void ReleasePlayer()
    {
        continueInput?.Disable();
        backInput?.Disable();
        if (player != null)
            player.enabled = playerWasEnabled;
        if (interaction != null)
            interaction.enabled = interactionWasEnabled;
        if (mode != null)
            mode.enabled = modeWasEnabled;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
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

    private void SceneChanged(Scene previous, Scene next)
    {
        if (previous == ownerScene)
            Destroy(gameObject);
    }

    private void OnDestroy()
    {
        SceneManager.activeSceneChanged -= SceneChanged;
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
