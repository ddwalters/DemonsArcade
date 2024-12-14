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

    void Start()
    {
        Player = FindAnyObjectByType<PlayerController>().gameObject;
        controller = Player.GetComponent<PlayerController>();
    }

    void Update()
    {
        //@DW Is this how we want to do gold?
        //text.text = controller.goldAmount + "";
    }
}
