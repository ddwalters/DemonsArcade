using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterMotor))]
public class PlayerController : MonoBehaviour
{
    [Header("Look")]
    [SerializeField] private float sensitivity = 2f;
    [SerializeField] private float joystickScalingFactor = 2f;

    [Header("Sprint")]
    [SerializeField] private float sprintFOV = 90f;
    [SerializeField] private float normalFOV = 80f;
    [SerializeField] private float fovLerpSpeed = 5f;
    [SerializeField] private float staminaUsageRate = 1f;

    [Header("Components")]
    [SerializeField] private Camera cam;

    public int goldAmount;

    private PlayerStatsManager statsManager;
    private CharacterMotor motor;
    private PlayerModeController mode;
    private NewControls controls;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private float xRotation;
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
        if (cam == null)
            cam = GetComponentInChildren<Camera>();

        sensitivity = PlayerPrefs.GetFloat("Settings.Sensitivity", sensitivity);
    }

    private void Update()
    {
        if (controls == null)
            return;

        if (mode.AllowsGameplay)
            LookAround();

        bool sprinting = UpdateSprint();
        motor.Tick(moveInput, sprinting, mode.AllowsGameplay, Time.deltaTime);
    }

    public NewControls GetNewControls() => controls;

    private bool UpdateSprint()
    {
        if (statsManager.GetStamina() <= 0f)
            sprintCooldown = true;
        else if (sprintCooldown && statsManager.GetStamina() >= statsManager.GetMaxStamina() - 1f)
            sprintCooldown = false;

        bool sprinting = mode.AllowsGameplay && sprintInput && !sprintCooldown && moveInput.y > 0.1f;
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, sprinting ? sprintFOV : normalFOV,
            Time.deltaTime * fovLerpSpeed);
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

        transform.Rotate(Vector3.up * yaw);
        xRotation = Mathf.Clamp(xRotation - pitch, -89f, 89f);
        cam.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    public float LookSensitivity => sensitivity;
    public void SetSensitivity(float value) => sensitivity = value;

    public void RestoreLocation(Vector3 position, Quaternion rotation)
    {
        StopMotion();
        motor = GetComponent<CharacterMotor>();
        motor.Teleport(position, rotation);
        xRotation = 0f;
        if (cam != null)
            cam.transform.localRotation = Quaternion.identity;
    }

    public void StopMotion()
    {
        motor = GetComponent<CharacterMotor>();
        motor.Stop();
        GetComponent<PlayerStatsManager>()?.SetStaminaUse(false);
    }
}
