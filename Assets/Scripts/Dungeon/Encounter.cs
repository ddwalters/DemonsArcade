using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Encounter : MonoBehaviour
{
    public GameObject[] spawnPoints;
    public ParticleTemplate particleTemplate;
    public MonsterTemplates monsterTempates;
    static public List<GameObject> Monsters = new List<GameObject>();
    public int totalMonsters = 0;
    public int currentMonsters = 0;
    public float distanceToPlayer;
    public Collider encounterTrigger;
    public GameObject chest;
    public GameObject chestSpawn;

    private GameObject player;

    private ParticleSystem spawnParticle;

    public int maxOccupancy;
    public int roomArea;

    public bool activeEncounter = false;
    public bool victory = false;

    public LayerMask layerMask;

    private void Awake()
    {
        particleTemplate = GetComponentInParent<ParticleTemplate>();
        spawnParticle = particleTemplate.spawnParticle;
        monsterTempates = GetComponentInParent<MonsterTemplates>();
        player = GameObject.Find("player");
    }

    public void OnTriggerEnter(Collider other)
    {
        if (layerMask == (layerMask | (1 << other.transform.gameObject.layer)))
        {
            if (victory == false)
            {
                activeEncounter = true;
                Debug.Log("FIGHT!");
            }
        }
    }

    private void Update()
    {
        if (activeEncounter == true)
        {

            if (maxOccupancy !> currentMonsters && totalMonsters < roomArea)
            {
                int rand = Random.Range(0, spawnPoints.Length - 1);
                GameObject cs = spawnPoints[rand];

                float distToPlayer = Vector3.Distance(player.transform.position, cs.transform.position);
                if (distToPlayer >= distanceToPlayer)
                {
                    float spawndelay = Random.Range(0f, 2f);
                    totalMonsters++;
                    currentMonsters++;
                    StartCoroutine(spawnMon(cs, spawndelay));
                }
            }
            if (currentMonsters == 0 && totalMonsters == roomArea)
            {
                Debug.Log("Victory!");
                victory = true;
                activeEncounter = false;
            }
        }
        if (victory == true)
        {
            Vector3 direction = (player.transform.position - chestSpawn.transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));

            GameObject loot = Instantiate(chest, chestSpawn.transform.position, lookRotation);
        }
    }

    IEnumerator spawnMon(GameObject cs, float spawnDelay)
    {
        yield return new WaitForSeconds(spawnDelay);

        Vector3 direction = (player.transform.position - cs.transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        ParticleSystem particle = Instantiate(spawnParticle, cs.transform.position, lookRotation);

        yield return new WaitForSeconds(0.2f);

        int mon = Random.Range(0, monsterTempates.monsterPrefabs.Length);
        GameObject go = Instantiate(monsterTempates.monsterPrefabs[mon], cs.transform.position, lookRotation);
        go.transform.SetParent(gameObject.transform);
        go.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
        Monsters.Add(go);

        Destroy(particle, 2f);
    }
}
