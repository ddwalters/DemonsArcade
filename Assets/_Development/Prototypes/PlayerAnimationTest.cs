using System;
using TMPro;
using UnityEngine;

// Development scene controls only. None of these preview controls are added to the game player prefab.
public class PlayerAnimationTest : MonoBehaviour
{
    [Serializable]
    public class Preview
    {
        public string name;
        public AnimationClip clip;
    }

    public PlayerController player;
    public Camera sceneCamera;
    public RuntimeAnimatorController previewController;
    public AnimationClip previewBaseClip;
    public Preview[] previews;
    public TMP_Text caption;
    public TMP_Text details;
    public PlayerAnimationShowcase showcase;

    private CharacterMotor motor;
    private Animator animator;
    private RuntimeAnimatorController locomotionController;
    private AnimatorOverrideController previewOverride;
    private Vector2 driveInput;
    private bool sprinting;
    private bool driving;
    private bool previewing;
    private int selected;
    private bool playerView;

    public Animator Animator => animator;

    private void Awake()
    {
        motor = player.GetComponent<CharacterMotor>();
        animator = motor.Animator;
        locomotionController = animator.runtimeAnimatorController;
        previewOverride = new AnimatorOverrideController(previewController);
    }

    private void Start() => LiveMovement();

    private void Update()
    {
        if (driving)
            motor.Tick(driveInput, sprinting, true, Time.deltaTime);
        else if (previewing && animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f)
            animator.Play("Preview", 0, 0f);
    }

    private void LateUpdate()
    {
        if (playerView)
            return;
        Vector3 offset = previewing ? new Vector3(2.8f, 1f, 3.6f) : new Vector3(-3.6f, 1.7f, -4.2f);
        sceneCamera.fieldOfView = previewing ? 38f : 45f;
        sceneCamera.transform.position = player.transform.position + offset;
        sceneCamera.transform.LookAt(player.transform.position + Vector3.up * 0.1f);
    }

    public void LiveMovement()
    {
        showcase?.Stop();
        RestoreLocomotion();
        driving = false;
        player.enabled = true;
        PlayerModeController.ForPlayer(player).ResetToGameplay();
        caption.text = "PLAYER MOVEMENT TEST";
        details.text = "WASD / Shift / Space / Mouse  |  Third-person camera  |  Camera button: player / overview";
    }

    private void RestoreLocomotion()
    {
        previewing = false;
        motor.enabled = true;
        if (animator.runtimeAnimatorController != locomotionController)
            animator.runtimeAnimatorController = locomotionController;
        Time.timeScale = 1f;
    }

    public void Drive(Vector2 input, bool sprint, string label)
    {
        showcase?.Stop();
        RestoreLocomotion();
        player.enabled = false;
        driveInput = input;
        sprinting = sprint;
        driving = true;
        motor.Teleport(new Vector3(0f, 1.04f, -2f), Quaternion.identity);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        caption.text = label;
        details.text = "Live locomotion controller  |  Animation root travel + collision motor";
    }

    public void Jump() => motor.RequestJump();

    public void BeginShowcase()
    {
        SetPlayerView(false);
        RestoreLocomotion();
        player.enabled = false;
        PlayerModeController.ForPlayer(player).ResetToGameplay();
        driveInput = Vector2.zero;
        sprinting = false;
        driving = true;
        motor.Teleport(new Vector3(0f, 1.04f, -2f), Quaternion.identity);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void SetDrive(Vector2 input, bool sprint)
    {
        driveInput = input;
        sprinting = sprint;
    }

    public void ShowClip(int index)
    {
        showcase?.Stop();
        SetPlayerView(false);
        Time.timeScale = 1f;
        player.enabled = false;
        driving = false;
        previewing = true;
        selected = (index + previews.Length) % previews.Length;
        animator.runtimeAnimatorController = locomotionController;
        motor.Teleport(new Vector3(0f, 1.04f, 0f), Quaternion.identity);
        motor.enabled = false;
        previewOverride[previewBaseClip] = previews[selected].clip;
        animator.runtimeAnimatorController = previewOverride;
        animator.Play("Preview", 0, 0f);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        caption.text = $"{selected + 1:00} / {previews.Length:00}   {previews[selected].name}";
        details.text = "Imported locomotion animation retargeted onto yBot";
    }

    private void OnGUI()
    {
        GUI.Box(new Rect(12, 100, 230, 252), "Development controls");
        if (GUI.Button(new Rect(24, 130, 206, 28), "Live movement / reset"))
            LiveMovement();
        if (GUI.Button(new Rect(24, 164, 100, 28), "Previous clip"))
            ShowClip(selected - 1);
        if (GUI.Button(new Rect(130, 164, 100, 28), "Next clip"))
            ShowClip(selected + 1);
        if (GUI.Button(new Rect(24, 198, 100, 28), "Walk demo"))
            Drive(Vector2.up, false, "WALKING");
        if (GUI.Button(new Rect(130, 198, 100, 28), "Run demo"))
            Drive(Vector2.up, true, "RUNNING");
        GUI.Label(new Rect(24, 236, 210, 35), "Escape releases the mouse.");
        if (showcase != null && GUI.Button(new Rect(24, 276, 206, 28), "Movement blend showcase"))
            showcase.Play();
        if (GUI.Button(new Rect(24, 310, 206, 28), playerView ? "Camera: player (click for overview)" : "Camera: overview (click for player)"))
        {
            LiveMovement();
            SetPlayerView(!playerView);
        }
    }

    public void SetPlayerView(bool value)
    {
        playerView = value;
        var camera = player.GetComponent<PlayerCameraRig>().ViewCamera;
        camera.enabled = value;
        camera.GetComponent<AudioListener>().enabled = value;
        sceneCamera.enabled = !value;
        var overviewListener = sceneCamera.GetComponent<AudioListener>();
        if (overviewListener != null)
            overviewListener.enabled = !value;
        caption.canvas.worldCamera = value ? camera : sceneCamera;
    }

    private void OnDestroy()
    {
        if (previewOverride != null)
            Destroy(previewOverride);
    }
}
