using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Encounter : MonoBehaviour
{
    public GameObject[] spawnPoints;
    public MonsterTemplates monsterTempates;
    static public List<GameObject> Monsters = new List<GameObject>();
    public int currentMonsters = 0;
    public Collider encounterTrigger;

    private GameObject player;

    public int maxOccupancy;
    public int roomArea;

    public bool activeEncounter = false;

    public LayerMask layerMask;

    private void Awake()
    {
        monsterTempates = GetComponentInParent<MonsterTemplates>();
        player = GameObject.Find("player");
    }

    public void OnTriggerEnter(Collider other)
    {
        if (layerMask == (layerMask | (1 << other.transform.gameObject.layer)))
        {
            Debug.Log("FIGHT!");
            activeEncounter = true;
        }
    }

    private void Update()
    {
        if (activeEncounter == true)
        {
            if (maxOccupancy != currentMonsters)
            {
                int rand = Random.Range(0, spawnPoints.Length - 1);
                GameObject cs = spawnPoints[rand];
                float distToPlayer = Vector3.Distance(player.transform.position, cs.transform.position);
                if (distToPlayer >= 2f)
                {
                    Vector3 direction = (player.transform.position - cs.transform.position).normalized;
                    Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
                    rand = Random.Range(0, monsterTempates.monsterPrefabs.Length - 1);
                    GameObject go = Instantiate(monsterTempates.monsterPrefabs[rand], cs.transform.position, lookRotation, gameObject.transform);
                    go.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                    currentMonsters++;
                    Monsters.Add(go);
                }
            }
        }
    }
}
