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
        public bool sword;
    }

    public PlayerController player;
    public Camera sceneCamera;
    public RuntimeAnimatorController previewController;
    public AnimationClip previewBaseClip;
    public Preview[] previews;
    public GameObject sword;
    public TMP_Text caption;
    public TMP_Text details;

    private CharacterMotor motor;
    private Animator animator;
    private RuntimeAnimatorController locomotionController;
    private AnimatorOverrideController previewOverride;
    private Vector2 driveInput;
    private bool sprinting;
    private bool driving;
    private bool previewing;
    private int selected;

    public PlayerAnimationShowcase showcase;

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
        Vector3 offset = previewing ? new Vector3(2.8f, 1f, 3.6f) : new Vector3(-3.6f, 1.7f, -4.2f);
        sceneCamera.fieldOfView = previewing ? 38f : 45f;
        sceneCamera.transform.position = player.transform.position + offset;
        sceneCamera.transform.LookAt(player.transform.position + Vector3.up * 0.1f);
    }

    public void LiveMovement()
    {
        if (showcase != null)
            showcase.Cancel();
        RestoreLocomotion();
        driving = false;
        player.enabled = true;
        PlayerModeController.ForPlayer(player).ResetToGameplay();
        caption.text = "PLAYER MOVEMENT TEST";
        details.text = "WASD  |  Shift: run forward  |  Space: jump  |  Mouse: turn  |  Escape: release cursor";
    }

    private void RestoreLocomotion()
    {
        previewing = false;
        motor.enabled = true;
        if (animator.runtimeAnimatorController != locomotionController)
            animator.runtimeAnimatorController = locomotionController;
        sword.SetActive(false);
        Time.timeScale = 1f;
    }

    public void Drive(Vector2 input, bool sprint, string label)
    {
        if (showcase != null)
            showcase.Cancel();
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

    public void BeginShowcase(RuntimeAnimatorController controller)
    {
        RestoreLocomotion();
        player.enabled = false;
        animator.runtimeAnimatorController = controller;
        driving = true;
        SetDrive(Vector2.zero, false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void SetDrive(Vector2 input, bool sprint)
    {
        driveInput = input;
        sprinting = sprint;
    }

    public void SetAuthoredAction(bool active)
    {
        driving = !active;
        motor.enabled = !active;
    }

    public void ShowClip(int index)
    {
        if (showcase != null)
            showcase.Cancel();
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
        sword.SetActive(previews[selected].sword);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        caption.text = $"{selected + 1:00} / {previews.Length:00}   {previews[selected].name}";
        details.text = "Imported animation retargeted onto yBot  |  Pose preview; combat is not connected";
    }

    private void OnGUI()
    {
        GUI.Box(new Rect(12, 100, 230, 218), "Development controls");
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
        if (showcase != null && GUI.Button(new Rect(24, 276, 206, 28), "Movement + death showcase"))
            showcase.Play();
    }

    private void OnDestroy()
    {
        if (previewOverride != null)
            Destroy(previewOverride);
    }
}
