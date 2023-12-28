using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterMovement : MonoBehaviour
{
    Animator animator;

    PlayerInput input;

    int isWalkingHash;

    int isRunningHash;

    void Awake()
    {
        input = new PlayerInput();

        input.actions["Movement"].performed += ctx => ctx.ReadValueAsObject();
    }

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();

        isWalkingHash = Animator.StringToHash("isWalking");
        isRunningHash = Animator.StringToHash("isRunning");
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void HandleMovement()
    {
        bool isWalking = animator.GetBool(isWalkingHash);
        bool isRunning = animator.GetBool(isRunningHash);
    }

    void OnEnable()
    {
        input.
    }
}
