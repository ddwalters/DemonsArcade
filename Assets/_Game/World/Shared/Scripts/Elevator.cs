using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Elevator : MonoBehaviour
{
    public GameObject elevator;
    public Transform endPoint;

    public LayerMask layerMask;

    public bool triggered = false;
    public float moveDuration = 2.0f; // Duration for the elevator to reach the end point
    public float pressurePlateMoveDuration = 0.5f; // Duration for the pressure plate to move down
    public float pressurePlateMoveDistance = 0.1f; // Distance for the pressure plate to move down

    private void OnTriggerEnter(Collider other)
    {
        if (layerMask == (layerMask | (1 << other.transform.gameObject.layer)))
        {
            if (triggered != false) { triggered = true; }
            else
            {
                StartCoroutine(MoveElevator());
                StartCoroutine(MovePressurePlate());
            }
        }
    }

    private IEnumerator MovePressurePlate()
    {
        Vector3 startPosition = transform.position;
        Vector3 endPosition = startPosition + Vector3.down * pressurePlateMoveDistance;
        float elapsedTime = 0f;

        while (elapsedTime < pressurePlateMoveDuration)
        {
            transform.position = Vector3.Lerp(startPosition, endPosition, elapsedTime / pressurePlateMoveDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure the pressure plate reaches the end position
        transform.position = endPosition;
    }

    private IEnumerator MoveElevator()
    {
        Vector3 startPosition = elevator.transform.position;
        Vector3 endPosition = endPoint.position;
        float elapsedTime = 0f;

        while (elapsedTime < moveDuration)
        {
            elevator.transform.position = Vector3.Lerp(startPosition, endPosition, elapsedTime / moveDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure the elevator reaches the end position
        elevator.transform.position = endPosition;
    }

}
