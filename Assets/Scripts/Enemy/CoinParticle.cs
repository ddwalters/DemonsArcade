using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.ParticleSystem;

public class CoinParticle : MonoBehaviour
{
    public ParticleSystemForceField playerField;
    public GameObject Player;
    public PlayerController controller;
    PlayerStatsManager playerStats;
    public ParticleSystem ps;
    List<ParticleSystem.Particle> particles = new List<ParticleSystem.Particle>();

    private void Awake()
    {
        playerField = FindAnyObjectByType<ParticleSystemForceField>();
        Player = FindAnyObjectByType<PlayerController>().gameObject;
        controller = Player.GetComponent<PlayerController>();
        //playerStats = Player.GetComponent<PlayerStatsManager>();
        ps = gameObject.GetComponent<ParticleSystem>();
        ps.trigger.SetCollider(0, Player.GetComponent<Collider>());
    }

    public void startRoutine(Vector2Int coinDropRange, float interval, int count)
    {
        StartCoroutine(dropCoins(coinDropRange, interval, count));
    }

    public IEnumerator dropCoins(Vector2Int coinDropRange, float interval, int count)
    {
        int random = Random.Range(coinDropRange.x, coinDropRange.y);

        Burst burst = new(0.0f, count);
        ps.emission.SetBursts(new Burst[] { burst });

        for (int i = 0; i < random; i++)
        {
            ps.Play();
            yield return new WaitForSeconds(interval);
        }
    }

    private void OnParticleTrigger()
    {
        int triggeredParticles = ps.GetTriggerParticles(ParticleSystemTriggerEventType.Enter, particles);

        for (int i = 0; i < triggeredParticles; i++)
        {
            ParticleSystem.Particle p = particles[i];
            p.remainingLifetime = 0f;
            controller.goldAmount++;
            
            particles[i] = p;
        }

        ps.SetTriggerParticles(ParticleSystemTriggerEventType.Enter, particles);
    }
}
