using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonLoad : MonoBehaviour
{
    public GameObject GameManager;
    public GameManager gameManager;

    public void Start()
    {
        gameManager = GameManager.GetComponent<GameManager>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // if colliding with a room/hallway, relay that info to the spawn node
        if (other.CompareTag("Player"))
        {
            gameManager.loadScene(2);
        }
    }
}
