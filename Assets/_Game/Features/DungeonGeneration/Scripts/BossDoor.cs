using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossDoor : InteractableBase
{
    [SerializeField] Transform endPoint;
    [SerializeField] private float moveDuration = 2.0f;

    public override void OnInteract()
    {
        base.OnInteract();

        StartCoroutine(OpenDoorRoutine());
    }

    public void closeDoor()
    {
        gameObject.SetActive(true);
    }

    private IEnumerator OpenDoorRoutine()
    {
        Vector3 startPosition = transform.position;
        Vector3 endPosition = endPoint.position;
        float elapsedTime = 0f;

        while (elapsedTime < moveDuration)
        {
            transform.position = Vector3.Lerp(startPosition, endPosition, elapsedTime / moveDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure the door reaches the end position
        transform.position = endPosition;

        // Optionally deactivate the door after it reaches the end position
        gameObject.SetActive(false);
    }
}
