using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private PlayerStatsManager statsManager;

    [Header("Settings")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 7f;
    [SerializeField] private float sensitivity = 2f;
    [SerializeField] private float joystickScalingFactor = 2f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float sprintFOV = 90f;
    [SerializeField] private float normalFOV = 80f;
    [SerializeField] private float fovLerpSpeed = 5f;
    [SerializeField] private float staminaUsageRate = 1f;
    [Space]
    [Header("Components")]
    [SerializeField] private Camera cam;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Transform playerBody;

    public int goldAmount;

    private Vector2 moveInput;

    private NewControls controls;
    private Vector2 lookInput;
    private float xRotation = 0f;

    private bool isGrounded;
    private bool jumpInput;

    private bool sprintInput;
    private bool sprintCooldown;
    private PlayerModeController mode;

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

        DontDestroyOnLoad(this);
        controls = new NewControls();
        mode = PlayerModeController.ForPlayer(this);

        controls.BasicActionMap.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        controls.BasicActionMap.Look.canceled += ctx => lookInput = Vector2.zero;

        controls.BasicActionMap.Movement.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        controls.BasicActionMap.Movement.canceled += ctx => moveInput = Vector2.zero;

        controls.BasicActionMap.Jump.performed += ctx => jumpInput = true;
        controls.BasicActionMap.Jump.canceled += ctx => jumpInput = false;

        controls.BasicActionMap.Sprint.performed += ctx => sprintInput = true;
        controls.BasicActionMap.Sprint.canceled += ctx => sprintInput = false;

    }

    private void OnEnable() => controls?.BasicActionMap.Enable();
    private void OnDisable() => controls?.BasicActionMap.Disable();

    private void Start()
    {
        statsManager = GetComponent<PlayerStatsManager>();
        cam = gameObject.GetComponentInChildren<Camera>();
        rb = gameObject.GetComponent<Rigidbody>();

        sensitivity = PlayerPrefs.GetFloat("Settings.Sensitivity", sensitivity);
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        if (mode.AllowsGameplay)
            LookAround();
    }

    private void FixedUpdate()
    {
        if (!mode.AllowsGameplay)
            return;

        Sprint();
        HandleMovement();

        if (jumpInput && isGrounded) 
            Jump();
    }

    public NewControls GetNewControls() => controls;

    private void HandleMovement()
    {
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;

        float currentSpeed = sprintInput && !sprintCooldown ? sprintSpeed : walkSpeed;
        Vector3 velocity = move * currentSpeed;
        velocity.y = rb.linearVelocity.y;
        rb.linearVelocity = velocity;

    }

    private void Sprint()
    {
        if (statsManager.GetStamina() <= 0)
            sprintCooldown = true;
        else if (sprintCooldown && statsManager.GetStamina() >= statsManager.GetMaxStamina() - 1)
            sprintCooldown = false;

        bool sprinting = sprintInput && !sprintCooldown;
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, sprinting ? sprintFOV : normalFOV, Time.deltaTime * fovLerpSpeed);
        statsManager.SetStaminaUse(sprinting);
        if (sprinting)
            statsManager.UseStamina(staminaUsageRate * Time.deltaTime);
    }

    private void Jump()
    {
        Vector3 jumpVelocity = Vector3.up * jumpForce;
        rb.AddForce(jumpVelocity, ForceMode.Impulse);
        jumpInput = false;
    }

    private void LookAround()
    {
        float inputMultiplier = 1f;

        if (controls.BasicActionMap.Look.activeControl != null)
            inputMultiplier = controls.BasicActionMap.Look.activeControl.device is Mouse ? 1f : joystickScalingFactor;

        float mouseX = lookInput.x * sensitivity * inputMultiplier * 0.01f;
        float mouseY = lookInput.y * sensitivity * inputMultiplier * 0.01f;

        playerBody.Rotate(Vector3.up * mouseX);

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        cam.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    public float LookSensitivity => sensitivity;

    public void SetSensitivity(float value) => sensitivity = value;

    public void RestoreLocation(Vector3 position, Quaternion rotation)
    {
        StopMotion();
        transform.SetPositionAndRotation(position, rotation);
        if (rb != null)
        {
            rb.position = position;
            rb.rotation = rotation;
            rb.angularVelocity = Vector3.zero;
        }

        xRotation = 0f;
        if (cam != null)
            cam.transform.localRotation = Quaternion.identity;
    }

    public void StopMotion()
    {
        if (rb != null)
            rb.linearVelocity = Vector3.zero;
        GetComponent<PlayerStatsManager>()?.SetStaminaUse(false);
        jumpInput = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
            isGrounded = true;
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
            isGrounded = false;
    }
}
