using UnityEngine;

public interface IEnemy
{
    // IEnemy defines the contract for enemy behaviors

    void BeginPlayerChase();
    void DamageEnemy(float damageAmount, float knockbackForce);
    void ApplyKnockback(float knockbackForce);
    void Die();

}
