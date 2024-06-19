using UnityEngine;

public class StandingState : State
{
    float gravityValue;
    bool jump;
    bool crouch;
    Vector3 currentVelocity;
    bool grounded;
    bool sprint;
    float playerSpeed;

    Vector3 cVelocity;

    public StandingState(Character _character, StateMachine _stateMachine) : base(_character, _stateMachine)
    {
        character = _character;
        stateMachine = _stateMachine;
    }

    public override void Enter()
    {
        base.Enter();

        jump = false;
        crouch = false;
        sprint = false;
        input = Vector2.zero;
        velocity = Vector3.zero;
        currentVelocity = Vector3.zero;
        gravityVelocity = Vector3.zero;

        playerSpeed = character.playerSpeed;
        grounded = character.controller.isGrounded;
        gravityValue = character.gravityValue;
    }

    public override void HandleInput()
    {
        base.HandleInput();

        if (jumpAction.triggered)
            jump = true;

        if (crouchAction.triggered)
            crouch = true;

        if (sprintAction.triggered)
            sprint = true;

        input = moveAction.ReadValue<Vector2>();
        velocity = new Vector3(input.x, 0f, input.y);

        velocity = velocity.x * character.cameraTransform.right.normalized + velocity.z * character.cameraTransform.forward.normalized;
        velocity.y = 0f;

    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        character.animator.SetFloat("Speed", input.magnitude, character.speedDampTime, Time.deltaTime);

        if (sprint)
            stateMachine.ChangeState(character.sprinting);

        if (jump)
            stateMachine.ChangeState(character.jumping);

        if (crouch)
            stateMachine.ChangeState(character.crouching);
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        if (!grounded)
            gravityVelocity.y += gravityValue * Time.deltaTime;
        else 
            gravityVelocity.y = -0.01f;

        grounded = character.controller.isGrounded;

        if (grounded && gravityVelocity.y < 0)
            gravityVelocity.y = -0.01f;

        currentVelocity = Vector3.SmoothDamp(currentVelocity, velocity, ref cVelocity, character.velocityDampTime);

        if (currentVelocity.sqrMagnitude < 0.001f)
            currentVelocity = Vector3.zero;

        character.controller.Move(currentVelocity * Time.deltaTime * playerSpeed + gravityVelocity * Time.deltaTime);

        if (velocity.sqrMagnitude > 0)
        {
            character.transform.rotation =
                Quaternion.Slerp(
                    character.transform.rotation,
                    Quaternion.LookRotation(velocity),
                    character.rotationDampTime);
        }

        Debug.Log($"Current Velocity: {currentVelocity}, Gravity Velocity: {gravityVelocity}, Grounded: {grounded}");
    }

    public override void Exit()
    {
        base.Exit();

        gravityVelocity.y = -0.01f;
        character.playerVelocity = new Vector3(input.x, 0f, input.y);

        if (velocity.sqrMagnitude > 0)
            character.transform.rotation = Quaternion.LookRotation(velocity);
    }
}