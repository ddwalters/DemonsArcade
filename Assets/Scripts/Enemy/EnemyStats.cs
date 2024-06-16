using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EnemyStats : MonoBehaviour
{
    Encounter encounter;

    [SerializeField] Animator anim;

    [SerializeField] float health;
    public bool isAlive;
    private float currentHealth;
    [SerializeField] Image healthBarSprite;
    [Space]
    [Header("Coin Particles")]
    [SerializeField] Vector2Int coinDropRange;
    [SerializeField] float interval;
    [SerializeField] int multiplier;
    [SerializeField] GameObject coinParticle;
    [SerializeField] Transform coinTransform;
    [Space]
    [Header("Death Particles")]
    [SerializeField] GameObject deathParticle;
    [SerializeField] Transform deathTransform;


    public bool inRange;

    private float lerpDuration;

    private void Start()
    {
        isAlive = true;
        encounter = GetComponentInParent<Encounter>();
        currentHealth = health;
        lerpDuration = .5f;
    }

    public void damageAI()
    {
        SkeletonAI skelly = gameObject.GetComponent<SkeletonAI>();
        if (skelly != null)
        {
            skelly.takeDamage();
        }
    }

    public void death()
    {
        if (!isAlive) return; // Prevent multiple calls

        isAlive = false;

        GameObject coinGO = Instantiate(coinParticle, coinTransform.position, coinTransform.rotation);
        CoinParticle coinPS = coinGO.GetComponent<CoinParticle>();
        coinPS.startRoutine(coinDropRange, interval, multiplier);

        Instantiate(deathParticle, deathTransform.position, deathTransform.rotation);
        if (encounter != null)
        {
            encounter.currentMonsters--;
        }
        Destroy(gameObject);
    }

    public IEnumerator DamageMonster(float damage)
    {
        float time = 0;
        float endValue = currentHealth - damage;

        if (endValue <= 0) endValue = 0;

        while (time < lerpDuration)
        {
            currentHealth = Mathf.Lerp(currentHealth, endValue, time / lerpDuration);
            healthBarSprite.fillAmount = currentHealth / health;
            time += Time.deltaTime;
            yield return null;
        }

        damageAI();

        currentHealth = endValue;

        if (currentHealth <= 0 && isAlive)
        {
            Debug.Log("Monster dead");
            death();
        }
    }
}
