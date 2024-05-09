using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private PlayerStatsManager statsManager;
    private Vector3 PlayerMovementInput;
    private float xRot;

    [SerializeField] private LayerMask floorMask;
    [SerializeField] private Transform feet;
    [SerializeField] private Camera cam;
    [SerializeField] private Rigidbody rb;
    [Space]
    [SerializeField] private float Speed;
    [SerializeField] private float Sensitivity;
    [SerializeField] private float JumpForce;

    private void Start()
    {
        statsManager = GetComponent<PlayerStatsManager>();
        cam = gameObject.GetComponentInChildren<Camera>();
        rb = gameObject.GetComponent<Rigidbody>();

        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // Jumping
        if (Input.GetButtonDown("Jump"))
        {   
            if(Physics.CheckSphere(feet.position, 0.1f, floorMask))
            {
                rb.AddForce(Vector3.up * JumpForce, ForceMode.Impulse);
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
        PlayerMovementInput = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));

        MovePlayer();
        MovePlayerCamera();
    }

    private void MovePlayer()
    {
        bool isSprinting = false;

        if (Input.GetKeyDown(KeyCode.LeftShift) && statsManager.GetStamina() > 0)
        {
            isSprinting = true;
            cam.fieldOfView = 90;
        }
        else if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            isSprinting = false;
            cam.fieldOfView = 80;
        }

        Vector3 MoveVector = transform.TransformDirection(PlayerMovementInput) * Speed;
        if (isSprinting && statsManager.GetStamina() > 0)
        {
            statsManager.UseStamina(1);
            MoveVector = MoveVector * 1.35f;
        }
        rb.velocity = new Vector3(MoveVector.x, rb.velocity.y, MoveVector.z);
    }

    private void MovePlayerCamera()
    {
        float mouseX = Input.GetAxis("Mouse X") * Sensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * Sensitivity * 1.3f * Time.deltaTime;

        xRot -= mouseY;
        xRot = Mathf.Clamp(xRot, -90f, 90f);

        cam.transform.localRotation = Quaternion.Euler(xRot, 0f, 0f);
        gameObject.transform.Rotate(Vector3.up * mouseX);
    }
}
