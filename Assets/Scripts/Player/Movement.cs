using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Movement : MonoBehaviour
{
    Animator animator;

    PlayerInput input;

    Vector2 currentMovement;

    int isWalkingHash;

    //int isRunningHash;

    bool movementPressed;

    //bool runPressed;

    void Awake()
    {
        input = GetComponent<PlayerInput>();

        input.actions.FindAction("Movement").performed += ctx => {
            currentMovement = ctx.ReadValue<Vector2>();
            movementPressed = currentMovement.x != 0 || currentMovement.y != 0;
        };
        //input.actions.FindAction("Run").performed += ctx => ctx.ReadValueAsButton();
    }

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();

        isWalkingHash = Animator.StringToHash("isWalking");
        //isRunningHash = Animator.StringToHash("isRunning");
    }

    // Update is called once per frame
    void Update()
    {
        HandleMovement();
        HandleRotation();
    }

    void HandleMovement()
    {
        bool isWalking = animator.GetBool(isWalkingHash);
        //bool isRunning = animator.GetBool(isRunningHash);

        if (movementPressed && !isWalking) {
            animator.SetBool(isWalkingHash, true);
        }

        if (movementPressed && isWalking) { 
            animator.SetBool(isWalkingHash, false);
        }

        //if ((movementPressed || runPressed) && !isRunning)
        //{
        //    animator.SetBool(isRunningHash, true);
        //}
        //
        //if ((!movementPressed || !runPressed) && isRunning)
        //{
        //    animator.SetBool(isRunningHash, false);
        //}
    }

    void HandleRotation()
    {
        Vector3 currentPosition = transform.position;

        Vector3 newPosition = new Vector3(currentMovement.x, 0, currentMovement.y);

        Vector3 postionToLookAt = currentPosition + newPosition;

        transform.LookAt(postionToLookAt);
    }

    void OnEnable()
    {
        input.currentActionMap.Enable();
    }

    void OnDisable()
    {
        input.currentActionMap.Disable();
    }
}
