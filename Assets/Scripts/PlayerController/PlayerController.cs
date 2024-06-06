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
    [SerializeField] private LayerMask floorMask;
    [SerializeField] private Camera cam;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Transform playerBody;

    public int goldAmount;
    public GameObject goldCounter;

    private Vector2 moveInput;

    private NewControls controls;
    private Vector2 lookInput;
    private bool canMoveCamera = true;
    private float xRotation = 0f;

    private bool isGrounded;
    private bool jumpInput;

    private bool sprintInput;
    private float currentSpeed;
    private bool sprintCooldown;
    private bool canMove = true;

    private void Awake()
    {
        DontDestroyOnLoad(this);
        controls = new NewControls();

        // look
        controls.BasicActionMap.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        controls.BasicActionMap.Look.canceled += ctx => lookInput = Vector2.zero;

        // move
        controls.BasicActionMap.Movement.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        controls.BasicActionMap.Movement.canceled += ctx => moveInput = Vector2.zero;

        // jump
        controls.BasicActionMap.Jump.performed += ctx => jumpInput = true;
        controls.BasicActionMap.Jump.canceled += ctx => jumpInput = false;

        // sprint
        controls.BasicActionMap.Sprint.performed += ctx => sprintInput = true;
        controls.BasicActionMap.Sprint.canceled += ctx => sprintInput = false;

        goldCounter = GameObject.Find("GoldCounter");
    }

    public void GetNewComponents()
    {
        goldCounter = GameObject.Find("GoldCounter");
    }

    private void OnEnable() => controls.BasicActionMap.Enable();
    private void OnDisable() => controls.BasicActionMap.Disable();

    private void Start()
    {
        statsManager = GetComponent<PlayerStatsManager>();
        cam = gameObject.GetComponentInChildren<Camera>();
        rb = gameObject.GetComponent<Rigidbody>();

        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        if (canMoveCamera)
            LookAround();
    }

    private void FixedUpdate()
    {
        if (canMove)
        {
            HandleMovement();
            Sprint();

            if (jumpInput && isGrounded)
                Jump();
        }
    }

    private void HandleMovement()
    {
        float moveX = moveInput.x;
        float moveZ = moveInput.y;
        Vector3 move = transform.right * moveX + transform.forward * moveZ;

        currentSpeed = sprintInput ? sprintSpeed : walkSpeed;
        Vector3 velocity = move * currentSpeed;
        velocity.y = rb.velocity.y;
        rb.velocity = velocity;

        if (sprintCooldown && statsManager.GetStamina() >= statsManager.GetMaxStamina() - 1)
            sprintCooldown = false;
    }

    private void Sprint()
    {
        if (statsManager.GetStamina() <= 0)
            sprintCooldown = true;

        if (sprintInput && !sprintCooldown)
        {
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, sprintFOV, Time.deltaTime * fovLerpSpeed);
            statsManager.StartUsingStamina();
            statsManager.UseStamina(staminaUsageRate * Time.deltaTime);
        }
        else
        {
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, normalFOV, Time.deltaTime * fovLerpSpeed);
            statsManager.StopUsingStamina();
        }
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

    public void EnableCameraMovement() => canMoveCamera = true;
    public void DisableCameraMovement() => canMoveCamera = false;

    public void EnableMovement() => canMove = true;
    public void DisableMovement() => canMove = false;

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