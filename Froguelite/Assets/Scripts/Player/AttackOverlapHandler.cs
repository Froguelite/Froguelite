using UnityEngine;

public class AttackOverlapHandler : MonoBehaviour
{

    // AttackOverlapHandler handles when the player's attack collider overlaps with other colliders


    #region VARIABLES


    


    #endregion


    #region OVERLAP


    private void OnTriggerEnter2D(Collider2D collision)
    {
        // If the player is not currently attacking, ignore (tongue is retracted)
        if (!PlayerAttack.Instance.IsAttacking())
            return;

        // Check if the collider belongs to an enemy
        if (collision.CompareTag("Enemy"))
        {
            IEnemy enemy = collision.GetComponent<IEnemy>();
            if (enemy == null)
                enemy = collision.GetComponentInParent<IEnemy>();

            if (enemy != null && !enemy.isDead)
            {
                enemy.DamageEnemy(StatsManager.Instance.playerDamage.GetValue(), StatsManager.Instance.playerKnockback.GetValue());
                
                // Apply poison if Sick Fly is active
                if (PlayerAttack.Instance.HasTongueTag("sickFly"))
                {
                    EnemyBase enemyBase = enemy as EnemyBase;
                    if (enemyBase != null)
                    {
                        float poisonDamage = StatsManager.Instance.playerDamage.GetValue() * 0.05f; // 5% of player damage per second
                        enemyBase.ApplyPoison(5f, poisonDamage); // 5 second duration (25% total damage)
                    }
                }
                
                PlayerAttack.Instance.StopTongueExtension(false);
            }
        }

        // Check if the collider belongs to a boss
        if (collision.CompareTag("Boss"))
        {
            BossEntity boss = collision.GetComponent<BossEntity>();
            if (boss == null)
                boss = collision.GetComponentInParent<BossEntity>();

            if (boss != null)
            {
                boss.TakeDamage(Mathf.FloorToInt(StatsManager.Instance.playerDamage.GetValue()));
                PlayerAttack.Instance.StopTongueExtension(false);
            }
        }

        // Doors
        else if (collision.CompareTag("Door"))
        {
            Door door = collision.GetComponent<Door>();
            if (door != null)
            {
                door.OnInteract();
            }
            else
            {
                SubZoneFinalDoor finalDoor = collision.GetComponent<SubZoneFinalDoor>();
                if (finalDoor != null)
                {
                    finalDoor.OnInteract();
                }
            }
        }
        // Collectables
        else if (collision.CompareTag("Collectable"))
        {
            ICollectable collectable = collision.GetComponent<ICollectable>();
            if (collectable != null)
            {
                collectable.OnCollect();
            }
        }
        // Totem
        else if (collision.CompareTag("Totem"))
        {
            Totem totem = collision.GetComponent<Totem>();
            if (totem == null)
                totem = collision.GetComponentInParent<Totem>();

            if (totem != null)
            {
                totem.OnInteract();
                PlayerAttack.Instance.StopTongueExtension(false);
            }
        }
        // Foliage
        else if (collision.CompareTag("Foliage"))
        {
            Foliage foliage = collision.GetComponent<Foliage>();

            if (foliage == null)
                foliage = collision.GetComponentInParent<Foliage>();
            if (foliage == null)
                return;
                
            // If it's impassable, stop the tongue extension
            if (foliage.IsImpassable())
            {
                PlayerAttack.Instance.StopTongueExtension();
                foliage.OnImpassableHit();
            }
            // If it's destructable, destroy it
            if (foliage.IsDestructable())
            {
                foliage.OnDestructableHit();
            }
        }
    }


    #endregion


}
