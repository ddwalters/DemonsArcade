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
    private InputAction sourceInput;
    private InputActionAsset dialogueControls;
    private InputActionReference continueReference;
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
        continueReference = InputActionReference.Create(continueInput);
        runner.continueAction = continueReference;
        continueInput.Enable();
        if (continueHint != null)
            continueHint.text = continueInput.GetBindingDisplayString() + "  Continue / reveal text    |    Esc / gamepad Cancel  Close";
    }

    private void LateUpdate()
    {
        if (runner == null || player == null)
            return;

        if (runner.IsDialogActive && !locked)
        {
            // Settings may have changed since this scene started.
            continueInput.Disable();
            for (int i = 0; i < sourceInput.bindings.Count; i++)
                continueInput.ApplyBindingOverride(i, sourceInput.bindings[i].effectivePath);
            continueInput.Enable();
            if (continueHint != null)
                continueHint.text = continueInput.GetBindingDisplayString() + "  Continue / reveal text    |    Esc / gamepad Cancel  Close";
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
        dialogueControls?.Disable();
        if (continueReference != null)
            Destroy(continueReference);
        if (dialogueControls != null)
            Destroy(dialogueControls);
    }
}
