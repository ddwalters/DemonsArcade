using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EnemyStats : MonoBehaviour
{
    Encounter encounter;

    [SerializeField] float health;
    [SerializeField] Vector2Int coinDropRange;
    private float currentHealth;
    [SerializeField] Image healthBarSprite;
    [SerializeField] GameObject deathParticle;
    [SerializeField] Transform deathTransform;
    [SerializeField] GameObject coinParticle;
    [SerializeField] Transform coinTransform;

    public bool inRange;

    private float lerpDuration;

    private void Start()
    {
        encounter = GetComponentInParent<Encounter>();
        currentHealth = health;
        lerpDuration = .5f;
    }

    public void death()
    {
        Instantiate(coinParticle, coinTransform.position, coinTransform.rotation);
        Instantiate(deathParticle, deathTransform.position, deathTransform.rotation);
        encounter.currentMonsters--;
        Destroy(gameObject);
    }

    public IEnumerator DamageMonster(float damage)
    {
        float time = 0;
        float endValue = currentHealth - damage;

        if (endValue <= 0) endValue = 0;

        while (time < .5f)
        {
            currentHealth = Mathf.Lerp(currentHealth, endValue, time / lerpDuration);
            healthBarSprite.fillAmount = currentHealth / health;
            time += Time.deltaTime;
            yield return null;
        }

        currentHealth = endValue;

        if (currentHealth <= 0)
        {
            Debug.Log("Monster dead");
            death();
        }
    }
}
