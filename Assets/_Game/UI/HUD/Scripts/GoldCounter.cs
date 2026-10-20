using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GoldCounter : MonoBehaviour
{
    public int goldCount;
    public GameObject Player;
    public PlayerController controller;
    public TMP_Text text;

    // Start is called before the first frame update
    void Start()
    {
        Player = FindAnyObjectByType<PlayerController>().gameObject;
        controller = Player.GetComponent<PlayerController>();
    }

    // Update is called once per frame
    void Update()
    {
        text.text = controller.goldAmount + "";
    }
}
