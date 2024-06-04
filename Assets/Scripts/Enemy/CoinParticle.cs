using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    public void dropCoins(Vector2Int coinDropRange)
    {
        ParticleSystem.Burst burst = new ParticleSystem.Burst(0.0f, (short)coinDropRange.x, (short)coinDropRange.y, 0.01f);
        ps.emission.SetBursts(new ParticleSystem.Burst[] { burst });
        ps.Play();
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
