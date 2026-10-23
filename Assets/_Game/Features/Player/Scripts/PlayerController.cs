using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterMotor))]
public class PlayerController : MonoBehaviour
{
    [Header("Look")]
    [SerializeField] private float sensitivity = 2f;
    [SerializeField] private float joystickScalingFactor = 2f;

    [Header("Sprint")]
    [SerializeField] private float staminaUsageRate = 4f;

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
        statsManager = GetComponent<PlayerStatsManager>();
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
        sensitivity = PlayerPrefs.GetFloat("Settings.Sensitivity", sensitivity);
    }

    private void Update()
    {
        if (controls == null)
            return;

        bool allowsMovement = mode != null && mode.AllowsGameplay;
        if (allowsMovement)
            LookAround();

        Vector2 input = Vector2.ClampMagnitude(moveInput, 1f);
        Vector3? movement = cameraRig != null ? cameraRig.GetMovementDirection(input) : null;
        bool sprinting = UpdateSprint(allowsMovement);
        motor.Tick(input, sprinting, allowsMovement, Time.deltaTime, movement);
    }

    public NewControls GetNewControls() => controls;

    private bool UpdateSprint(bool allowsMovement)
    {
        if (statsManager == null)
            return false;

        if (statsManager.GetStamina() <= 0f)
            sprintCooldown = true;
        else if (sprintCooldown && statsManager.GetStamina() >= statsManager.GetMaxStamina() * 0.99f)
            sprintCooldown = false;

        bool sprinting = allowsMovement && sprintInput && !sprintCooldown && moveInput.sqrMagnitude > 0.01f;
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

        cameraRig?.Look(new Vector2(yaw, pitch));
    }

    public float LookSensitivity => sensitivity;
    public void SetSensitivity(float value) => sensitivity = value;

    public void RestoreLocation(Vector3 position, Quaternion rotation)
    {
        StopMotion();
        motor.Teleport(position, rotation);
        cameraRig.ResetView();
    }

    public void StopMotion()
    {
        motor.Stop();
        statsManager?.SetStaminaUse(false);
    }
}
