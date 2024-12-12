using UnityEngine;
using UnityEngine.AI;

public class SlimeBall : MonoBehaviour
{
    public LayerMask layerMask;
    public GameObject slime;
    public GameObject impactParticle;
    public float groundCheckDistance = 1.0f; // Distance to check below for ground

    private void OnCollisionEnter(Collision other)
    {
        // Check if collided object's layer matches the playerLayer
        if (((1 << other.gameObject.layer) & layerMask) != 0)
        {
            // Instantiate slime at the ground level, using NavMesh to find a valid position
            InstantiateSlimeOnGround(transform.position);

            // Optionally instantiate the impact particle effect
            if (impactParticle != null)
                Instantiate(impactParticle, transform.position, Quaternion.identity);

            Destroy(gameObject);
        }
    }

    private void InstantiateSlimeOnGround(Vector3 position)
    {
        // Adjust the initial position slightly above the ground check distance
        Vector3 initialPosition = position + Vector3.up * groundCheckDistance;

        // Use NavMesh.SamplePosition to find a valid ground position
        NavMeshHit hit;
        if (NavMesh.SamplePosition(initialPosition, out hit, groundCheckDistance * 2, NavMesh.AllAreas))
        {
            // Instantiate slime at the valid ground position
            GameObject newSlime = Instantiate(slime, hit.position, Quaternion.identity);
        }
        else
        {
            Debug.LogError("Failed to place slime on a valid NavMesh area.");
        }
    }
}
