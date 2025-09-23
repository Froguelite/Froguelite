using UnityEngine;

public class BossDamageTester : MonoBehaviour
{
    [SerializeField] private BossEntity bossEntity;
    [SerializeField] private int damageAmount = 10;

    public void DealDamage()
    {
        if (bossEntity != null)
        {
            bossEntity.TakeDamage(damageAmount);
        }
    }
}
