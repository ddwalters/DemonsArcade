using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoinParticle : MonoBehaviour
{
    public ParticleSystemForceField playerField;
    public GameObject Player;
    PlayerStatsManager playerStats;
    public ParticleSystem ps;
    List<ParticleSystem.Particle> particles = new List<ParticleSystem.Particle>();

    private void Awake()
    {
        playerField = FindAnyObjectByType<ParticleSystemForceField>();
        Player = FindAnyObjectByType<PlayerController>().gameObject;
        //playerStats = Player.GetComponent<PlayerStatsManager>();
        ps = gameObject.GetComponent<ParticleSystem>();
        ps.trigger.SetCollider(0, Player.GetComponent<Collider>());
    }

    private void OnParticleTrigger()
    {
        int triggeredParticles = ps.GetTriggerParticles(ParticleSystemTriggerEventType.Enter, particles);

        for (int i = 0; i < triggeredParticles; i++)
        {
            ParticleSystem.Particle p = particles[i];
            p.remainingLifetime = 0f;
            
            particles[i] = p;
        }

        ps.SetTriggerParticles(ParticleSystemTriggerEventType.Enter, particles);
    }
}
