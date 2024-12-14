using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private PlayerControls _playerControls;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 5f;

    [Header("Look Settings")]
    public float lookSpeed = 2f;
    public Vector2 lookSensitivity = new Vector2(1f, 1f);

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    private Vector2 _moveInput;
    private Vector2 _lookInput;
    private Transform _cameraTransform;
    private Rigidbody _rb;

    private float _verticalLookRotation = 0f;
    private bool _isGrounded;

    private bool _allowedControl;

    private void Awake()
    {
        _playerControls = new PlayerControls();

        _cameraTransform = GetComponentInChildren<Camera>().transform;
        _rb = GetComponent<Rigidbody>();
        _allowedControl = true;
    }

    private void OnEnable()
    {
        _playerControls.Enable();
    }

    private void OnDisable()
    {
        _playerControls.Disable();
    }


    float _sprintMultiplier = 1.0f;
    [SerializeField]bool _isSprinting = false;
    public void OnMove(InputAction.CallbackContext context)
    {
        if (!_isSprinting)
            _sprintMultiplier = 1.0f;

        _moveInput = context.ReadValue<Vector2>() * _sprintMultiplier;
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        if (_isSprinting)
            return;

        _isSprinting = true;
        float forwardFactor = Mathf.Lerp(0, 1, Mathf.Abs(_moveInput.y));
        _sprintMultiplier *= (1 + forwardFactor);
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        _lookInput = context.ReadValue<Vector2>() * lookSensitivity;
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed && _isGrounded && _allowedControl)
        {
            Vector3 jumpVelocity = Vector3.up * jumpForce;
            _rb.AddForce(jumpVelocity, ForceMode.Impulse);
        }
    }

    private void Update()
    {
        if (!_allowedControl)
            return;

        CheckGroundStatus();
        MovePlayer();
        LookAround();
    }

    private void CheckGroundStatus()
    {
        _isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
    }

    private void MovePlayer()
    {
        float moveX = _moveInput.x;
        float moveZ = _moveInput.y;
        Vector3 move = transform.right * moveX + transform.forward * moveZ;

        Vector3 velocity = move * moveSpeed;
        velocity.y = _rb.velocity.y;
        _rb.velocity = velocity;
    }

    private void LookAround()
    {
        transform.Rotate(Vector3.up * _lookInput.x * lookSpeed);

        _verticalLookRotation -= _lookInput.y * lookSpeed;
        _verticalLookRotation = Mathf.Clamp(_verticalLookRotation, -90f, 90f);

        _cameraTransform.localRotation = Quaternion.Euler(_verticalLookRotation, 0f, 0f);
    }

    public void TogglePlayerMovement()
    {
        _allowedControl = false;
    }
}