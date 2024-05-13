using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyStats : MonoBehaviour
{
    public int health;

    private void Awake()
    {
        Invoke("death", 2f);   
    }

    public void death()
    {
        Encounter encounter = GetComponentInParent<Encounter>();
        Destroy(gameObject);
        encounter.currentMonsters--;
    }
}
