using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EnemyStats : MonoBehaviour
{
    Encounter encounter;

    [SerializeField] float health;
    private float currentHealth;
    [SerializeField] Image healthBarSprite;

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
