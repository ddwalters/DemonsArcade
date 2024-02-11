using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectInteractor : MonoBehaviour
{
    public Transform camera;
    public float playerActiveDistance;

    RaycastHit hit;
    bool active = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            active = Physics.Raycast(camera.position, camera.TransformDirection(Vector3.forward), out hit, playerActiveDistance);

            if (hit.transform.CompareTag("Crate") && active == true)
            {
                Debug.Log("Crate");
            }
        }
    }
}
