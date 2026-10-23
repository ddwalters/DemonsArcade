using UnityEngine;

/// <summary>
/// Turns an NPC toward the participant in its conversation and adds a small, reversible head look.
/// This component is opt-in so ordinary interactables such as signs never rotate.
/// </summary>
public sealed class NpcConversationFacing : MonoBehaviour
{
    private const float HeadYawLimit = 35f;
    private const float HeadPitchLimit = 20f;
    private const float HeadLookWeight = 0.65f;
    private const float HeadLookSmoothness = 8f;

    [SerializeField, Min(1f)] private float bodyTurnSpeed = 360f;

    private Transform participant;
    private Transform participantHead;
    private Collider participantUpperBody;
    private Transform headBone;
    private Quaternion smoothedHeadLookOffset = Quaternion.identity;
    private Quaternion lastAnimatedHeadRotation;
    private bool conversationActive;
    private bool hasAppliedHeadLook;

    private void Awake()
    {
        ResolveHeadBone();
    }

    private void Update()
    {
        // Remove last frame's offset before the Animator evaluates this frame.
        RestoreAnimatedHeadPose();
    }

    private void LateUpdate()
    {
        if (conversationActive)
        {
            if (!IsParticipantAvailable())
            {
                EndConversation();
            }
            else
            {
                TurnTowardParticipant();
            }
        }

        if (headBone == null)
            return;

        Vector3 targetDirection = conversationActive
            ? GetParticipantFacePosition() - headBone.position
            : Vector3.zero;
        Quaternion targetHeadOffset = Quaternion.identity;

        if (targetDirection.sqrMagnitude > 0.0001f)
        {
            Vector3 localDirection = transform.InverseTransformDirection(targetDirection.normalized);
            float horizontalLength = new Vector2(localDirection.x, localDirection.z).magnitude;
            float yaw = Mathf.Atan2(localDirection.x, localDirection.z) * Mathf.Rad2Deg;
            float pitch = Mathf.Atan2(localDirection.y, horizontalLength) * Mathf.Rad2Deg;
            yaw = Mathf.Clamp(yaw, -HeadYawLimit, HeadYawLimit) * HeadLookWeight;
            pitch = Mathf.Clamp(pitch, -HeadPitchLimit, HeadPitchLimit) * HeadLookWeight;
            targetHeadOffset = Quaternion.Euler(-pitch, yaw, 0f);
        }

        float blend = 1f - Mathf.Exp(-HeadLookSmoothness * Time.deltaTime);
        smoothedHeadLookOffset = Quaternion.Slerp(smoothedHeadLookOffset, targetHeadOffset, blend);

        if (smoothedHeadLookOffset == Quaternion.identity)
            return;

        lastAnimatedHeadRotation = headBone.localRotation;
        Quaternion worldOffset = transform.rotation * smoothedHeadLookOffset *
                                 Quaternion.Inverse(transform.rotation);
        headBone.rotation = worldOffset * headBone.rotation;
        hasAppliedHeadLook = true;
    }

    public void BeginConversation(Transform conversationParticipant)
    {
        RestoreAnimatedHeadPose();
        participant = conversationParticipant;
        conversationActive = IsParticipantAvailable();
        participantHead = null;
        participantUpperBody = null;

        if (conversationActive)
            ResolveParticipantFace();
    }

    public void EndConversation()
    {
        conversationActive = false;
        participant = null;
        participantHead = null;
        participantUpperBody = null;
        RestoreAnimatedHeadPose();
    }

    private void OnDisable()
    {
        EndConversation();
        smoothedHeadLookOffset = Quaternion.identity;
    }

    private void OnDestroy()
    {
        EndConversation();
    }

    private bool IsParticipantAvailable()
    {
        return participant != null && participant.gameObject.activeInHierarchy;
    }

    private void TurnTowardParticipant()
    {
        Vector3 direction = participant.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.0001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation, targetRotation, Mathf.Max(1f, bodyTurnSpeed) * Time.deltaTime);
    }

    private void ResolveHeadBone()
    {
        Animator animator = GetComponentInChildren<Animator>(true);
        if (animator != null && animator.isHuman)
            headBone = animator.GetBoneTransform(HumanBodyBones.Head);
    }

    private void ResolveParticipantFace()
    {
        Animator animator = participant.GetComponentInChildren<Animator>(true);
        if (animator != null && animator.isHuman)
            participantHead = animator.GetBoneTransform(HumanBodyBones.Head);

        if (participantHead == null)
            participantUpperBody = FindUpperBodyCollider(participant, null);
    }

    private static Collider FindUpperBodyCollider(Transform root, Transform excludedTransform)
    {
        Collider[] colliders = root.GetComponentsInChildren<Collider>();
        Collider upperBody = null;
        float highestCenter = float.NegativeInfinity;

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider candidate = colliders[i];
            if (candidate == null || !candidate.enabled || candidate.isTrigger ||
                candidate.transform == excludedTransform)
                continue;

            float centerHeight = candidate.bounds.center.y;
            if (centerHeight <= highestCenter)
                continue;

            highestCenter = centerHeight;
            upperBody = candidate;
        }

        return upperBody;
    }

    private Vector3 GetParticipantFacePosition()
    {
        if (participantHead != null)
            return participantHead.position;

        if (participantUpperBody != null)
        {
            Bounds bounds = participantUpperBody.bounds;
            return new Vector3(bounds.center.x, bounds.center.y + bounds.extents.y * 0.75f,
                bounds.center.z);
        }

        return participant.position + Vector3.up * 1.6f;
    }

    private void RestoreAnimatedHeadPose()
    {
        if (!hasAppliedHeadLook || headBone == null)
            return;

        headBone.localRotation = lastAnimatedHeadRotation;
        hasAppliedHeadLook = false;
    }
}
