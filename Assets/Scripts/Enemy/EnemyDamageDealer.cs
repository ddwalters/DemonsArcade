using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyDamageDealer : MonoBehaviour
{
    bool canDealDamage;
    bool hasDealtDamage;

    [SerializeField] float weaponDamage;
    [SerializeField] float weaponLength;

    public LayerMask layerMask;

    private void Start()
    {
        canDealDamage = false;
        hasDealtDamage = false;
    }

    public void SetDamage(float damage) => weaponDamage = damage;

    public void SetLength(float length) => weaponLength = length;

    public void SetLayerMask(LayerMask layer) => layerMask = layer;

    private void Update()
    {
        if (canDealDamage && !hasDealtDamage)
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, -transform.up, out hit, weaponLength, layerMask))
            {
                if (hit.transform.TryGetComponent(out PlayerStatsManager playerStats))
                {
                    playerStats.DamagePlayer(weaponDamage);
                    hasDealtDamage = true;
                }
            }
        }
    }

    public void StartDealDamage()
    {
        canDealDamage = true;
        hasDealtDamage = false;
    }

    public void EndDealDamage()
    {
        canDealDamage = false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position - transform.up * weaponLength);
    }
}
