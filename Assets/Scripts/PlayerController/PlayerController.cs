using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float rotationSpeed = 180f;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        // Lock cursor to the center of the screen
        Cursor.lockState = CursorLockMode.Locked;
    }

    void FixedUpdate()
    {
        // Rotation
        float mouseX = Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;
        transform.Rotate(Vector3.up * mouseX);

        // Movement
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        Vector3 moveDirection = new Vector3(horizontalInput, 0, verticalInput).normalized;
        Vector3 movement = transform.TransformDirection(moveDirection) * moveSpeed * Time.deltaTime;
        rb.MovePosition(rb.position + movement);

        // Unlock cursor if Escape key is pressed
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
        }
    }
}
