using UnityEditor.ShaderGraph;
using UnityEngine;
using UnityEngine.InputSystem;

public class State
{
    public Character character;
    public StateMachine stateMachine;

    protected Vector3 gravityVelocity;
    protected Vector3 velocity;
    protected Vector2 input;

    public InputAction moveAction;
    public InputAction lookAction;
    public InputAction jumpAction;
    public InputAction crouchAction;
    public InputAction sprintAction;

    public State(Character _character, StateMachine _stateMachine)
    {
        character = _character;
        stateMachine = _stateMachine;

        moveAction = character.playerInput.actions["Movement"];
        lookAction = character.playerInput.actions["Look"];
        jumpAction = character.playerInput.actions["Jump"];
        crouchAction = character.playerInput.actions["Crouch"];
        sprintAction = character.playerInput.actions["Sprint"];
    }

    public virtual void Enter()
    {
        Debug.Log("enter state: " + this.ToString());
    }

    public virtual void HandleInput()
    {
    }

    public virtual void LogicUpdate()
    {
    }

    public virtual void PhysicsUpdate()
    {
    }

    public virtual void Exit()
    {
    }


    //private float xRotation = 0f;
    //private float sensitivity = 2f;

    //public void LookAround()
    //{
    //    float mouseX = lookAction. * sensitivity * 0.01f;
    //    float mouseY = lookInput.y * sensitivity * 0.01f;
    //
    //    playerBody.Rotate(Vector3.up * mouseX);
    //
    //    xRotation -= mouseY;
    //    xRotation = Mathf.Clamp(xRotation, -90f, 90f);
    //
    //    cam.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    //}
}