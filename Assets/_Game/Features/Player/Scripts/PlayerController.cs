using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterMotor))]
public class PlayerController : MonoBehaviour
{
    [Header("Look")]
    [SerializeField] private float sensitivity = 2f;
    [SerializeField] private float joystickScalingFactor = 2f;

    [Header("Movement")]
    [SerializeField, Min(0f), Tooltip("Character turning speed in degrees per second.")]
    private float turnSpeed = 720f;

    [Header("Sprint")]
    [SerializeField] private float staminaUsageRate = 1f;

    public int goldAmount;

    private PlayerStatsManager statsManager;
    private CharacterMotor motor;
    private PlayerCameraRig cameraRig;
    private PlayerModeController mode;
    private NewControls controls;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool sprintInput;
    private bool sprintCooldown;

    private void Awake()
    {
        foreach (var existing in FindObjectsByType<PlayerController>())
        {
            if (existing != this && existing.controls != null)
            {
                existing.RestoreLocation(transform.position, transform.rotation);
                gameObject.SetActive(false);
                Destroy(gameObject);
                return;
            }
        }

        DontDestroyOnLoad(gameObject);
        motor = GetComponent<CharacterMotor>();
        cameraRig = GetComponent<PlayerCameraRig>();
        mode = PlayerModeController.ForPlayer(this);
        controls = new NewControls();

        controls.BasicActionMap.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        controls.BasicActionMap.Look.canceled += ctx => lookInput = Vector2.zero;
        controls.BasicActionMap.Movement.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        controls.BasicActionMap.Movement.canceled += ctx => moveInput = Vector2.zero;
        controls.BasicActionMap.Jump.performed += ctx =>
        {
            if (mode.AllowsGameplay)
                motor.RequestJump();
        };
        controls.BasicActionMap.Sprint.performed += ctx => sprintInput = true;
        controls.BasicActionMap.Sprint.canceled += ctx => sprintInput = false;
    }

    private void OnEnable() => controls?.BasicActionMap.Enable();
    private void OnDisable() => controls?.BasicActionMap.Disable();
    private void OnDestroy() => controls?.Dispose();

    private void Start()
    {
        statsManager = GetComponent<PlayerStatsManager>();
        sensitivity = PlayerPrefs.GetFloat("Settings.Sensitivity", sensitivity);
    }

    private void Update()
    {
        if (controls == null)
            return;

        if (mode.AllowsGameplay && !motor.IsActionActive)
            LookAround();

        Vector2 input = Vector2.ClampMagnitude(moveInput, 1f);
        Vector3 movement = cameraRig.GetMovementDirection(input);
        if (mode.AllowsGameplay && !motor.IsActionActive && movement.sqrMagnitude > 0.001f)
        {
            Quaternion facing = Quaternion.LookRotation(movement, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, facing, turnSpeed * Time.deltaTime);
        }

        bool sprinting = UpdateSprint();
        motor.Tick(Vector2.up * input.magnitude, sprinting, mode.AllowsGameplay, Time.deltaTime, movement);
    }

    public NewControls GetNewControls() => controls;

    private bool UpdateSprint()
    {
        if (statsManager.GetStamina() <= 0f)
            sprintCooldown = true;
        else if (sprintCooldown && statsManager.GetStamina() >= statsManager.GetMaxStamina() - 1f)
            sprintCooldown = false;

        bool sprinting = mode.AllowsGameplay && !motor.IsActionActive && sprintInput && !sprintCooldown && moveInput.sqrMagnitude > 0.01f;
        statsManager.SetStaminaUse(sprinting);
        if (sprinting)
            statsManager.UseStamina(staminaUsageRate * Time.deltaTime);

        return sprinting;
    }

    private void LookAround()
    {
        bool mouse = controls.BasicActionMap.Look.activeControl?.device is Mouse;
        float inputMultiplier = mouse ? 0.01f : joystickScalingFactor * Time.deltaTime;
        float yaw = lookInput.x * sensitivity * inputMultiplier;
        float pitch = lookInput.y * sensitivity * inputMultiplier;

        cameraRig.Look(new Vector2(yaw, pitch));
    }

    public float LookSensitivity => sensitivity;
    public void SetSensitivity(float value) => sensitivity = value;

    public void RestoreLocation(Vector3 position, Quaternion rotation)
    {
        StopMotion();
        motor = GetComponent<CharacterMotor>();
        motor.Teleport(position, rotation);
        cameraRig = GetComponent<PlayerCameraRig>();
        cameraRig.ResetView();
    }

    public void StopMotion()
    {
        motor = GetComponent<CharacterMotor>();
        motor.Stop();
        GetComponent<PlayerStatsManager>()?.SetStaminaUse(false);
    }
}
