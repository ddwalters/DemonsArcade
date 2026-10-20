using UnityEngine;

// Only added by the development showcase. Gameplay continues to use CharacterRootMotion.
public class ShowcaseRootMotion : MonoBehaviour
{
    public CharacterController capsule;
    public bool activeAction;
    public bool useGravity;
    public float verticalSpeed;
    private Animator animator;

    private void Awake() => animator = GetComponent<Animator>();

    private void OnAnimatorMove()
    {
        if (!activeAction || capsule == null)
            return;

        // Full clip previews bake vertical travel into the pose; this is not a gameplay jump arc.
        Vector3 displacement = animator.deltaPosition;
        if (useGravity)
            verticalSpeed += -14f * Time.deltaTime;
        displacement.y = (useGravity ? verticalSpeed : -2f) * Time.deltaTime;
        capsule.Move(displacement);
        capsule.transform.rotation *= animator.deltaRotation;
    }
}
