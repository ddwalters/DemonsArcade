using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private PlayerStatsManager statsManager;
    private Vector3 playerMovementInput;
    private float xRot;
    private float targetFOV;

    [Header("Settings")]
    [SerializeField] private float speed = 5f;
    [SerializeField] private float sprintSpeedMultiplier;
    [SerializeField] private float sensitivity = 2f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float sprintFOV = 90f;
    [SerializeField] private float normalFOV = 80f;
    [SerializeField] private float fovLerpSpeed = 5f;
    [Space]
    [Header("Components")]
    [SerializeField] private LayerMask floorMask;
    [SerializeField] private Transform feet;
    [SerializeField] private Camera cam;
    [SerializeField] private Rigidbody rb;

    private void Start()
    {
        statsManager = GetComponent<PlayerStatsManager>();
        cam = gameObject.GetComponentInChildren<Camera>();
        rb = gameObject.GetComponent<Rigidbody>();

        Cursor.lockState = CursorLockMode.Locked;

        // Set initial target FOV
        targetFOV = normalFOV;
    }

    void Update()
    {
        MovePlayerCamera();
        // Jumping
        if (Input.GetButtonDown("Jump"))
        {
            if (Physics.CheckSphere(feet.position, 0.1f, floorMask))
            {
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            }
        }

        // Unlock cursor if Escape key is pressed
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
        }
    }

    private void FixedUpdate()
    {
        playerMovementInput = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));

        MovePlayer();
    }

    private void MovePlayer()
    {
        bool isSprinting = false;

        if (Input.GetKey(KeyCode.LeftShift))
        {
            isSprinting = true;
            targetFOV = sprintFOV;
            StartCoroutine(statsManager.UseStamina(0.5f));

            // hardcoded stamina for now-- can update for player stats later
            if (statsManager.GetStamina() <= 0)
            {
                isSprinting = false;
                targetFOV = normalFOV;
            }
        }
        else if (!Input.GetKey(KeyCode.LeftShift))
        {
            isSprinting = false;
            targetFOV = normalFOV;
            statsManager.StartStaminaRegen();
            StartCoroutine(statsManager.RegenerateStamina());
        }

        Vector3 moveVector = transform.TransformDirection(playerMovementInput) * speed;
        if (isSprinting && statsManager.GetStamina() > 0)
        {
            StartCoroutine(statsManager.UseStamina(0.5f));
            moveVector *= sprintSpeedMultiplier;
        }
        else
        {
            statsManager.StartStaminaRegen();
            StartCoroutine(statsManager.RegenerateStamina());
        }
        rb.velocity = new Vector3(moveVector.x, rb.velocity.y, moveVector.z);

        // Smoothly adjust FOV
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, fovLerpSpeed * Time.deltaTime);
    }

    private void MovePlayerCamera()
    {
        float mouseX = Input.GetAxis("Mouse X") * sensitivity;
    float mouseY = Input.GetAxis("Mouse Y") * sensitivity;

    xRot -= mouseY;
    xRot = Mathf.Clamp(xRot, -90f, 90f);

    cam.transform.localRotation = Quaternion.Euler(xRot, 0f, 0f);
    transform.Rotate(Vector3.up * mouseX);
    }
}
