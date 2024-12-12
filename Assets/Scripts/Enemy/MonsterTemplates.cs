using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterTemplates : MonoBehaviour
{
    public GameObject[] monsterPrefabs;

    [SerializeField] Dictionary<GameObject, int> floorOneWeight = new Dictionary<GameObject, int>();
}
