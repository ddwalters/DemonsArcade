using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    public Transform startPos; // Starting position of the platform
    public Transform endPos;   // Ending position of the platform
    public float speed = 2f;   // Speed at which the platform moves

    private Vector3 targetPosition;

    void Start()
    {
        // Start moving towards the end position initially
        targetPosition = endPos.position;
    }

    void Update()
    {
        // Move the platform towards the target position
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

        // If the platform reaches the target position, switch the target position
        if (transform.position == targetPosition)
        {
            if (targetPosition == startPos.position)
                targetPosition = endPos.position;
            else
                targetPosition = startPos.position;
        }
    }
}
