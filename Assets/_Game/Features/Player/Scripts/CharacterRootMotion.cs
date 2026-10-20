using UnityEngine;

[RequireComponent(typeof(Animator))]
public class CharacterRootMotion : MonoBehaviour
{
    [SerializeField] private CharacterMotor motor;
    private Animator animator;

    private void Awake() => animator = GetComponent<Animator>();

    private void OnAnimatorMove()
    {
        if (motor != null && motor.isActiveAndEnabled)
            motor.ApplyRootMotion(animator.deltaPosition, Time.deltaTime);
    }
}
